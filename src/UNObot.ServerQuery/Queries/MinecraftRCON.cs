using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace UNObot.ServerQuery.Queries
{
    public class MinecraftRCON : IRCON
    {
        private const ushort RxSize = 4096;
        private static readonly byte[] EndOfCommandPacket = MakePacketData("", PacketType.Type100, 0);
        private readonly object _lock = new object();
        private byte[] _buffer;
        private Socket _client;
        private List<byte> _packetCollector = new List<byte>(RxSize);


        public MinecraftRCON(IPEndPoint server, string password, bool reuse = false, string command = null)
        {
            Password = password;
            Server = server;
            _buffer = new byte[RxSize];
            CreateConnection(reuse, command);
        }

        private string Password { get; }

        public ulong Owner { get; set; }
        public bool Disposed { get; private set; }
        public IRCON.RCONStatus Status { get; private set; }
        public string Data { get; private set; }
        public IPEndPoint Server { get; }

        public void Execute(string command, bool reuse = false)
        {
            Wipe(ref _buffer);
            Execute(command, ref _buffer, reuse);
        }

        public void ExecuteSingle(string command, bool reuse = false)
        {
            Wipe(ref _buffer);
            ExecuteSingle(command, ref _buffer, reuse);
        }

        public bool Connected()
        {
            if (Status != IRCON.RCONStatus.Success) return false;
            try
            {
                return !(_client.Poll(1, SelectMode.SelectRead) && _client.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }

        public void Dispose()
        {
            Disposed = true;
            _client?.Dispose();
        }

        private void CreateConnection(bool reuse = false, string command = null)
        {
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveTimeout = 5000,
                SendTimeout = 5000
            };
            try
            {
                if (!_client.ConnectAsync(Server).Wait(5000))
                {
                    Status = IRCON.RCONStatus.ConnFail;
                    return;
                }
            }
            catch (Exception)
            {
                Status = IRCON.RCONStatus.ConnFail;
                return;
            }

            if (Authenticate() && command != null)
                Execute(command, reuse);
        }

        private bool Authenticate()
        {
            Wipe(ref _buffer);
            return Authenticate(ref _buffer);
        }

        private bool Authenticate(ref byte[] rxData)
        {
            try
            {
                var payload = MakePacketData(Password, PacketType.ServerdataAuth, 0);
                _client.Send(payload);
                _client.Receive(rxData);
                var id = LittleEndianReader(ref rxData, 4);
                var type = LittleEndianReader(ref rxData, 8);
                if (id == -1 || type != 2)
                {
                    Status = IRCON.RCONStatus.AuthFail;
                    return false;
                }

                Status = IRCON.RCONStatus.Success;
                return true;
            }
            catch (Exception)
            {
                Status = IRCON.RCONStatus.IntFail;
                return false;
            }
        }

        public void Execute(string command, ref byte[] rxData, bool reuse = false)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                throw new InvalidOperationException("Don't do this, it's going to die either way.");
            lock (_lock)
            {
                var packetCount = 0;
                try
                {
                    var payload = MakePacketData(command, PacketType.ServerdataExeccommand, 0);
                    try
                    {
                        _client.Send(payload);
                    }
                    catch (ObjectDisposedException)
                    {
                        CreateConnection(reuse);
                        _client.Send(payload);
                    }

                    _client.Send(EndOfCommandPacket);
                    var end = false;
                    do
                    {
                        Wipe(ref rxData);
                        var size = _client.Receive(rxData);
                        if (size == 0)
                        {
                            // Connection failed.
                            packetCount = 0;
                            _packetCollector.Clear();
                            CreateConnection(reuse);
                            _client.Send(payload);
                            _client.Send(EndOfCommandPacket);
                        }
#if DEBUG
                        using (var fs = new FileStream($"packet{packetCount}", FileMode.Create, FileAccess.Write))
                        {
                            fs.Write(rxData, 0, rxData.Length);
                        }
                        // StringConcat.Append($"\nPacket {PacketCount}\n\n");
#endif
                        var id = LittleEndianReader(ref rxData, 4);
                        var type = LittleEndianReader(ref rxData, 8);
                        if ((id == -1 || type != (int) PacketType.ServerdataResponseValue) && packetCount == 0)
                        {
                            Status = IRCON.RCONStatus.AuthFail;
                            return;
                        }

                        var position = packetCount == 0 ? 12 : 0;
                        var currentByte = rxData[position++];
                        while (position < size)
                        {
                            /*
                            if (CurrentChar != '\x00' && CurrentChar != 0 && CurrentChar != 0x1b)
                                StringConcat.Append(CurrentChar);
                            WackDataProcessor(ref RXData, ref Position);
                            */
                            _packetCollector.Add(currentByte);
                            currentByte = rxData[position++];
                        }

                        if (currentByte != '\x00' && currentByte != 0)
                            _packetCollector.Add(currentByte);

                        /*
                        if (RXData[Size - 1] == '\x00')
                            End = true;
                            */
                        if (Contains(_packetCollector, "Unknown", out var remove))
                        {
                            _packetCollector.RemoveRange(remove, _packetCollector.Count - remove);
                            end = true;
                        }

                        packetCount++;

                        // Excess packets.
                        if (packetCount == 100)
                        {
                            end = true;
                        }
                    } while (!end);

                    Data = Stringifier(ref _packetCollector);
                    _packetCollector.Clear();
                    Status = IRCON.RCONStatus.Success;
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10060 && _packetCollector.Count > 0)
                    {
                        Status = IRCON.RCONStatus.Success;
                        Data = Stringifier(ref _packetCollector);
                    }
                    else if ((ex.ErrorCode == 10053 || ex.ErrorCode == 32) && reuse)
                    {
                        Status = IRCON.RCONStatus.IntFail;
                        CreateConnection(true);
                    }
                    else
                    {
                        Status = IRCON.RCONStatus.IntFail;
                    }
                }
                catch (Exception)
                {
                    Status = IRCON.RCONStatus.IntFail;
                }

                if (Status != IRCON.RCONStatus.Success || !reuse)
                    Dispose();
            }
        }

        public void ExecuteSingle(string command, ref byte[] rxData, bool reuse = false)
        {
            try
            {
                var payload = MakePacketData(command, PacketType.ServerdataExeccommand, 0);
                _client.Send(payload);
                _client.Receive(rxData);
                var id = LittleEndianReader(ref rxData, 4);
                var type = LittleEndianReader(ref rxData, 8);
                if (id == -1 || type != 0)
                {
                    Status = IRCON.RCONStatus.AuthFail;
                    return;
                }

                var stringConcat = new StringBuilder();
                var position = 12;
                var currentChar = (char) rxData[position++];
                while (currentChar != '\x00')
                {
                    stringConcat.Append(currentChar);
                    currentChar = (char) rxData[position++];
                }

                Data = stringConcat.ToString();
                Status = IRCON.RCONStatus.Success;
            }
            catch (Exception)
            {
                Status = IRCON.RCONStatus.IntFail;
            }

            if (Status != IRCON.RCONStatus.Success || !reuse)
                Dispose();
        }

        private static byte[] LittleEndianConverter(int data)
        {
            var b = new byte[4];
            b[0] = (byte) data;
            b[1] = (byte) (((uint) data >> 8) & 0xFF);
            b[2] = (byte) (((uint) data >> 16) & 0xFF);
            b[3] = (byte) (((uint) data >> 24) & 0xFF);
            return b;
        }

        private static int LittleEndianReader(ref byte[] data, int startIndex)
        {
            return (data[startIndex + 3] << 24)
                   | (data[startIndex + 2] << 16)
                   | (data[startIndex + 1] << 8)
                   | data[startIndex];
        }

        // Fixes those random 14 byte sequences in RCON output.
        private static void WackDataProcessor(ref List<byte> data)
        {
            for (var i = 0; i < data.Count; i++)
                if (data[i] == 0)
                {
                    var start = i;
                    while (i < data.Count && data[i] == 0)
                        i++;
                    while (i < data.Count && data[i] != 0)
                        i++;
                    while (i < data.Count && data[i] == 0)
                        i++;
                    var end = i;
                    data.RemoveRange(start, end - start - 1);
                }
        }

        private static void Wipe(ref byte[] data)
        {
            for (var i = 0; i < data.Length; i++)
                data[i] = (byte) '\x00';
        }

        private static string Stringifier(ref List<byte> data)
        {
            var sb = new StringBuilder(data.Count);
            WackDataProcessor(ref data);
            foreach (var character in data)
                if (character != 0)
                    sb.Append((char) character);
            return sb.ToString();
        }

        private static byte[] MakePacketData(string body, PacketType type, int id)
        {
            var length = LittleEndianConverter(body.Length + 9);
            var idData = LittleEndianConverter(id);
            var packetType = LittleEndianConverter((int) type);
            var bodyData = Encoding.UTF8.GetBytes(body);
            // Plus 1 for the null byte.
            var packet = new byte[length.Length + idData.Length + packetType.Length + bodyData.Length + 1];
            var counter = 0;
            foreach (var @byte in length)
                packet[counter++] = @byte;
            foreach (var @byte in idData)
                packet[counter++] = @byte;
            foreach (var @byte in packetType)
                packet[counter++] = @byte;
            foreach (var @byte in bodyData)
                packet[counter++] = @byte;
            return packet;
        }

        private static bool Contains(IReadOnlyList<byte> data, string content, out int position)
        {
            position = -1;
            if (content.Length > data.Count) return false;
            for (position = data.Count - 1 - content.Length; position >= 0; position--)
            {
                var success = true;
                for (var j = position; j < position + content.Length; j++)
                    if (data[j] != content[j - position])
                        success = false;
                if (success)
                    return true;
            }

            position = -1;
            return false;
        }

        private enum PacketType
        {
            ServerdataResponseValue = 0,
            ServerdataExeccommand = 2,
            ServerdataAuth = 3,
            Type100 = 100
        }
    }
}