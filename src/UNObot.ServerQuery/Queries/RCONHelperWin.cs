using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UNObot.ServerQuery.Queries
{
    public class RCONHelperWin : IRCON
    {
        private const int BufferSize = 4110;
        private const int MaxPacketsRead = 40;
        
        private enum PacketType { SERVERDATA_RESPONSE_VALUE = 0,  SERVERDATA_EXECCOMMAND = 2, SERVERDATA_AUTH_RESPONSE = 2, SERVERDATA_AUTH = 3, TYPE_100 = 100}
        
        public ulong Owner { get; set; }
        public IRCON.RCONStatus Status { get; private set; }
        public bool Disposed { get; private set; }
        public string Data => _data.ToString();
        private StringBuilder _data;
        public IPEndPoint Server { get; }
        private string Password;
        private Socket _socket;
        private byte[] RXData = new byte[BufferSize];

        public RCONHelperWin(IPEndPoint server, string password, string command = null)
        {
            Password = password;
            Server = server;
            _data = new StringBuilder();
            
            CreateConnection(command);
        }

        private void CreateConnection(string command)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveTimeout = 5000,
                SendTimeout = 5000
            };
            
            try
            {
                if (!_socket.ConnectAsync(Server).Wait(5000))
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
            
            if (Authenticate() && !string.IsNullOrEmpty(command))
                ExecuteSingle(command);
        }
        
        public bool Connected()
        {
            if (Status != IRCON.RCONStatus.Success) return false;
            try
            {
                return !(_socket.Poll(1, SelectMode.SelectRead) && _socket.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }

        private bool Authenticate()
        {
            var payload = MakePacketData(Password, PacketType.SERVERDATA_AUTH, 0);
            _socket.Send(payload);
            var count = _socket.Receive(RXData);
            var id = LittleEndianReader(RXData, 4);
            var type = LittleEndianReader(RXData, 8);
            if (count < 12 || id == -1 || type != (int) PacketType.SERVERDATA_AUTH_RESPONSE)
            {
                Status = IRCON.RCONStatus.AuthFail;
                return false;
            }

            Status = IRCON.RCONStatus.Success;
            return true;
        }
        
        public void ExecuteSingle(string command, bool reuse = false)
        {
            if (Status == IRCON.RCONStatus.AuthFail)
                Authenticate();
            if (Status == IRCON.RCONStatus.AuthFail)
                return;
            
            WipeBuffer();
            var payload = MakePacketData(command, PacketType.SERVERDATA_EXECCOMMAND, 0);
            _socket.Send(payload);
            var count = _socket.Receive(RXData);
            var id = LittleEndianReader(RXData, 4);
            var type = LittleEndianReader(RXData, 8);
            if (id == -1 || type != (int) PacketType.SERVERDATA_RESPONSE_VALUE || count <= 12)
            {
                Status = IRCON.RCONStatus.IntFail;
                return;
            }

            _data.Clear();
            _data.EnsureCapacity(count - 12);
            var position = 12;
            var currentChar = (char) RXData[position++];
            while (currentChar != '\x00')
            {
                _data.Append(currentChar);
                currentChar = (char) RXData[position++];
            }

            Status = IRCON.RCONStatus.Success;
        }

        public void Execute(string command, bool reuse = false)
        {
            if (Status == IRCON.RCONStatus.AuthFail)
                Authenticate();
            if (Status == IRCON.RCONStatus.AuthFail)
                return;

            var packetCount = 0;
            var payload = MakePacketData(command, PacketType.SERVERDATA_EXECCOMMAND, 0);
            var endOfCommand = MakePacketData("", PacketType.TYPE_100, 0);
            _socket.Send(payload);
            _socket.Send(endOfCommand);
            
            _data.Clear();
            WipeBuffer();
            var count = _socket.Receive(RXData);

            var dataTrim = -1;
            var startOfPacket = 0;
            var lifetime = 0;
            var position = 0;

            while (count > 0)
            {
                ++packetCount;

                if (lifetime == 0)
                {
                    lifetime = LittleEndianReader(RXData, startOfPacket) - 10;
                    position = startOfPacket + 12;
                    _data.EnsureCapacity(_data.Length + lifetime);
                }

                if (packetCount == 1)
                {
                    var id = LittleEndianReader(RXData, 4);
                    var type = LittleEndianReader(RXData, 8);
                    if (id == -1 || type != (int) PacketType.SERVERDATA_RESPONSE_VALUE || count <= 12)
                    {
                        Status = IRCON.RCONStatus.ExecFail;
                        return;
                    }
                }
                
                var currentChar = (char) RXData[position++];
                while (position < count && lifetime > 0)
                {
                    --lifetime;
                    if (currentChar != '\x00')
                        _data.Append(currentChar);
                    currentChar = (char) RXData[position++];
                }

                if (lifetime == 0)
                {
                    if (position != count - 1)
                    {
                        startOfPacket = position;
                        continue;
                    }

                    startOfPacket = 0;
                }

                // Bad performance-wise, but there's nothing on StringBuilder...
                dataTrim = Data.IndexOf("Unknown request 64", StringComparison.Ordinal);
                if (dataTrim >= 0 || packetCount >= MaxPacketsRead)
                    break;
                
                WipeBuffer();
                count = _socket.Receive(RXData);
                position = 0;
            }

            if (count < 0)
            {
                Status = IRCON.RCONStatus.IntFail;
                if (packetCount >= 2)
                    // Somehow data is read but count < 0 ?
                    Status = IRCON.RCONStatus.Success;
            }
            else
                Status = IRCON.RCONStatus.Success;

            if (dataTrim >= 0)
                _data.Remove(dataTrim, _data.Length - dataTrim);
        }

        private static byte[] LittleEndianConverter(int data)
        {
            var output = BitConverter.GetBytes(data);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(output);
            }

            return output;
        }

        private static int LittleEndianReader(IReadOnlyList<byte> data, int startIndex)
        {
            var temp = new [] {data[startIndex], data[startIndex + 1], data[startIndex + 2], data[startIndex + 3]};
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(temp);
            }

            return BitConverter.ToInt32(temp);
        }

        private void WipeBuffer()
        {
            Array.Fill(RXData, (byte) 0b0);
        }

        private byte[] MakePacketData(string body, PacketType type, int id)
        {
            var lengthNum = body.Length;
            var length = LittleEndianConverter(lengthNum + 10);
            var idData = LittleEndianConverter(id);
            var packetType = LittleEndianConverter((int) type);
            var bodyData = new ASCIIEncoding().GetBytes(body);
            var packet = new byte[length.Length + idData.Length + packetType.Length + body.Length + 2];
            var counter = 0;
            foreach (var b in length)
                packet[counter++] = b;
            foreach (var b in idData)
                packet[counter++] = b;
            foreach (var b in packetType)
                packet[counter++] = b;
            foreach (var b in bodyData)
                packet[counter++] = b;
            for (var i = 0; i < 2; ++i)
                packet[counter++] = 0b0;

            return packet;
        }
        
        public void Dispose()
        {
            _socket.Dispose();
            WipeBuffer();
            _data = null;
            Password = null;
            Disposed = true;
        }
    }
}