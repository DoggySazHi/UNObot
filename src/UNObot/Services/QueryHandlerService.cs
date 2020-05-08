using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Discord;

namespace UNObot.Services
{
    //Adopted from Valve description: https://developer.valvesoftware.com/wiki/Server_queries#A2S_INFO
    //Thanks to https://www.techpowerup.com/forums/threads/snippet-c-net-steam-a2s_info-query.229199/ for A2S_INFO, self-reimplemented for A2S_PLAYER and A2S_RULES
    //Dealing with network-level stuff is hard

    //Credit to https://github.com/maxime-paquatte/csharp-minecraft-query/blob/master/src/Status.cs
    public static class QueryHandlerService
    {
        public static string HumanReadable(float Time)
        {
            TimeSpan TS = TimeSpan.FromSeconds(Time);
            string Output;
            if (TS.Hours != 0)
                Output = $"{(int)TS.TotalHours}:{TS.Minutes:00}:{TS.Seconds:00}";
            else if (TS.Minutes != 0)
                Output = $"{TS.Minutes}:{TS.Seconds:00}";
            else
                Output = $"{TS.Seconds} second{(TS.Seconds == 1 ? "" : "s")}";
            return Output;
        }

        public static bool GetInfo(string ip, ushort port, out A2S_INFO output)
        {
            var parseCheck = IPAddress.TryParse(ip, out var server);
            var addresses = ResolveDNS(ip);
            if (!parseCheck)
            {
                if (addresses == null || addresses.Length == 0)
                {
                    output = null;
                    return false;
                }
                server = addresses[0];
            }
            var iPEndPoint = new IPEndPoint(server, port);
            output = new A2S_INFO(iPEndPoint);
            return output.ServerUp;
        }

        public static bool GetPlayers(string ip, ushort port, out A2S_PLAYER output)
        {
            var parseCheck = IPAddress.TryParse(ip, out var server);
            var addresses = ResolveDNS(ip);
            if (!parseCheck)
            {
                if (addresses == null || addresses.Length == 0)
                {
                    output = null;
                    return false;
                }
                server = addresses[0];
            }
            var iPEndPoint = new IPEndPoint(server, port);
            output = new A2S_PLAYER(iPEndPoint);
            return output.ServerUp;
        }

        public static bool GetRules(string ip, ushort port, out A2S_RULES output)
        {
            var parseCheck = IPAddress.TryParse(ip, out var server);
            var addresses = ResolveDNS(ip);
            if (!parseCheck)
            {
                if (addresses == null || addresses.Length == 0)
                {
                    output = null;
                    return false;
                }
                server = addresses[0];
            }
            var iPEndPoint = new IPEndPoint(server, port);
            output = new A2S_RULES(iPEndPoint);
            return output.ServerUp;
        }

        public static bool GetInfoMCNew(string ip, ushort port, out MinecraftStatus output)
        {
            var parseCheck = IPAddress.TryParse(ip, out var server);
            var addresses = ResolveDNS(ip);
            if (!parseCheck)
            {
                if (addresses == null || addresses.Length == 0)
                {
                    output = null;
                    return false;
                }
                server = addresses[0];
            }
            var iPEndPoint = new IPEndPoint(server, port);
            output = new MinecraftStatus(iPEndPoint);
            return output.ServerUp;
        }

        public static bool SendRCON(string ip, ushort port, string command, string password, out MinecraftRCON output)
        {
            var parseCheck = IPAddress.TryParse(ip, out var server);
            var addresses = ResolveDNS(ip);
            if (!parseCheck)
            {
                if (addresses == null || addresses.Length == 0)
                {
                    output = null;
                    return false;
                }
                server = addresses[0];
            }
            var iPEndPoint = new IPEndPoint(server, port);
            output = new MinecraftRCON(iPEndPoint, password, false, command);
            return output.Status == MinecraftRCON.RCONStatus.SUCCESS;
        }

        public static bool CreateRCON(string ip, ushort port, string password, ulong User, out MinecraftRCON output)
        {
            var PossibleRCON = MinecraftRCON.GetRCON(User);
            if (PossibleRCON != null)
            {
                output = PossibleRCON;
                return false;
            }

            var parseCheck = IPAddress.TryParse(ip, out var server);
            var addresses = ResolveDNS(ip);
            if (!parseCheck)
            {
                if (addresses == null || addresses.Length == 0)
                {
                    output = null;
                    return false;
                }
                server = addresses[0];
            }
            var iPEndPoint = new IPEndPoint(server, port);
            output = new MinecraftRCON(iPEndPoint, password, true);
            return output.Status == MinecraftRCON.RCONStatus.SUCCESS;
        }

        public static bool ExecuteRCON(ulong User, string Command, out MinecraftRCON output)
        {
            var PossibleRCON = MinecraftRCON.GetRCON(User);
            output = PossibleRCON;
            if (PossibleRCON == null)
                return false;

            PossibleRCON.Execute(Command, true);
            return output.Status == MinecraftRCON.RCONStatus.SUCCESS;
        }
        public static bool CloseRCON(ulong User)
        {
            var PossibleRCON = MinecraftRCON.GetRCON(User);
            if (PossibleRCON == null)
                return false;

            PossibleRCON.Dispose();
            return true;
        }

        private static IPAddress[] ResolveDNS(string ip)
        {
            IPAddress[] addresses = null;
            try
            {
                addresses = Dns.GetHostAddresses(ip);
            }
            catch (SocketException)
            {

            }
            catch (ArgumentException)
            {

            }
            return addresses;
        }

        public static MCStatus GetInfoMC(string ip, ushort port = 25565)
            => new MCStatus(ip, port);
    }

    public class A2S_INFO
    {
        // \xFF\xFF\xFF\xFFTSource Engine Query\x00 because UTF-8 doesn't like to encode 0xFF
        public static readonly byte[] REQUEST = { 0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00 };
        #region Strong Typing Enumerators
        [Flags]
        public enum ExtraDataFlags : byte
        {
            GameID = 0x01,
            SteamID = 0x10,
            Keywords = 0x20,
            Spectator = 0x40,
            Port = 0x80
        }
        public enum VACFlags : byte
        {
            Unsecured = 0,
            Secured = 1
        }
        public enum VisibilityFlags : byte
        {
            Public = 0,
            Private = 1
        }
        public enum EnvironmentFlags : byte
        {
            Linux = 0x6C,   //l
            Windows = 0x77, //w
            Mac = 0x6D,     //m
            MacOsX = 0x6F   //o
        }
        public enum ServerTypeFlags : byte
        {
            Dedicated = 0x64,     //d
            Nondedicated = 0x6C,   //l
            SourceTV = 0x70   //p
        }
        #endregion
        public byte Header { get; }        // I
        public byte Protocol { get; }
        public string Name { get; }
        public string Map { get;}
        public string Folder { get; }
        public string Game { get; }
        public short ID { get; }
        public byte Players { get; }
        public byte MaxPlayers { get; }
        public byte Bots { get; }
        public ServerTypeFlags ServerType { get; }
        public EnvironmentFlags Environment { get; }
        public VisibilityFlags Visibility { get; }
        public VACFlags VAC { get; }
        public string Version { get; }
        public ExtraDataFlags ExtraDataFlag { get; }
        #region Extra Data Flag Members
        public ulong GameID { get; }           //0x01
        public ulong SteamID { get; }          //0x10
        public string Keywords { get; }        //0x20
        public string Spectator { get; }       //0x40
        public short SpectatorPort { get; }   //0x40
        public short Port { get; }             //0x80
        public bool ServerUp { get; }

        #endregion

        public A2S_INFO(IPEndPoint ep)
        {
            try
            {
                UdpClient udp = new UdpClient();
                udp.Client.SendTimeout = 5000;
                udp.Client.ReceiveTimeout = 5000;
                udp.Send(REQUEST, REQUEST.Length, ep);
                MemoryStream ms = new MemoryStream(udp.Receive(ref ep));    // Saves the received data in a memory buffer
                BinaryReader br = new BinaryReader(ms, Encoding.UTF8);      // A binary reader that treats charaters as Unicode 8-bit
                ms.Seek(4, SeekOrigin.Begin);   // skip the 4 0xFFs
                Header = br.ReadByte();
                Protocol = br.ReadByte();
                Name = A2S_SHARED.ReadNullTerminatedString(ref br);
                Map = A2S_SHARED.ReadNullTerminatedString(ref br);
                Folder = A2S_SHARED.ReadNullTerminatedString(ref br);
                Game = A2S_SHARED.ReadNullTerminatedString(ref br);
                ID = br.ReadInt16();
                Players = br.ReadByte();
                MaxPlayers = br.ReadByte();
                Bots = br.ReadByte();
                ServerType = (ServerTypeFlags)br.ReadByte();
                Environment = (EnvironmentFlags)br.ReadByte();
                Visibility = (VisibilityFlags)br.ReadByte();
                VAC = (VACFlags)br.ReadByte();
                Version = A2S_SHARED.ReadNullTerminatedString(ref br);
                ExtraDataFlag = (ExtraDataFlags)br.ReadByte();
                #region These EDF readers have to be in this order because that's the way they are reported
                if (ExtraDataFlag.HasFlag(ExtraDataFlags.Port))
                    Port = br.ReadInt16();
                if (ExtraDataFlag.HasFlag(ExtraDataFlags.SteamID))
                    SteamID = br.ReadUInt64();
                if (ExtraDataFlag.HasFlag(ExtraDataFlags.Spectator))
                {
                    SpectatorPort = br.ReadInt16();
                    Spectator = A2S_SHARED.ReadNullTerminatedString(ref br);
                }
                if (ExtraDataFlag.HasFlag(ExtraDataFlags.Keywords))
                    Keywords = A2S_SHARED.ReadNullTerminatedString(ref br);
                if (ExtraDataFlag.HasFlag(ExtraDataFlags.GameID))
                    GameID = br.ReadUInt64();
                #endregion
                br.Close();
                ms.Close();
                udp.Close();
                ServerUp = true;
            }
            catch (EndOfStreamException)
            {
                ServerUp = true;
            }
            catch (SocketException)
            {
                ServerUp = false;
            }
            catch (Exception Ex)
            {
                LoggerService.Log(LogSeverity.Error, "Failed to query via A2S.", Ex);
                ServerUp = false;
            }
        }
    }

    public class A2S_PLAYER
    {
        private static readonly byte[] Handshake = { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF };

        public struct Player
        {
            public byte Index { get; set; }
            public string Name { get; set; }
            public long Score { get; set; }
            public float Duration { get; set; }
        }

        public byte Header { get; }
        public byte PlayerCount { get; }

        public bool ServerUp { get; }
        public List<Player> Players { get; }

        public A2S_PLAYER(IPEndPoint ep)
        {
            try
            {
                Players = new List<Player>();
                UdpClient udp = new UdpClient();
                udp.Client.SendTimeout = 5000;
                udp.Client.ReceiveTimeout = 5000;
                udp.Send(Handshake, Handshake.Length, ep);
                MemoryStream ms = new MemoryStream(udp.Receive(ref ep));
                BinaryReader br = new BinaryReader(ms, Encoding.UTF8);
                ms.Seek(5, SeekOrigin.Begin);

                //Get challenge number, and plan to resend it.
                byte[] Response = { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte() };

                br.Close();
                ms.Close();
                udp.Send(Response, Response.Length, ep);
                ms = new MemoryStream(udp.Receive(ref ep));
                br = new BinaryReader(ms, Encoding.UTF8);

                ms.Seek(4, SeekOrigin.Begin);
                Header = br.ReadByte();
                PlayerCount = br.ReadByte();
                for (int i = 0; i < Convert.ToInt32(PlayerCount); i++)
                {
                    var Index = br.ReadByte();
                    var Name = A2S_SHARED.ReadNullTerminatedString(ref br);
                    var Score = br.ReadInt32();
                    var Duration = br.ReadSingle();
                    Player p = new Player
                    {
                        Index = Index,
                        Name = Name,
                        Score = Score,
                        Duration = Duration
                    };
                    Players.Add(p);
                }

                br.Close();
                ms.Close();
                udp.Close();
                ServerUp = true;
            }
            catch (EndOfStreamException)
            {
                ServerUp = true;
            }
            catch (SocketException)
            {
                ServerUp = false;
            }
            catch (Exception Ex)
            {
                LoggerService.Log(LogSeverity.Error, "Failed to query via A2S.", Ex);
                ServerUp = false;
            }
        }
    }

    public class A2S_RULES
    {
        private static readonly byte[] Handshake = { 0xFF, 0xFF, 0xFF, 0xFF, 0x56, 0xFF, 0xFF, 0xFF, 0xFF };

        public struct Rule
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public byte Header { get; }
        public short RuleCount { get; }
        public List<Rule> Rules { get; }
        public bool ServerUp { get; }

        public A2S_RULES(IPEndPoint ep)
        {
            try
            {
                Rules = new List<Rule>();
                UdpClient udp = new UdpClient();
                udp.Client.SendTimeout = 5000;
                udp.Client.ReceiveTimeout = 5000;
                udp.Send(Handshake, Handshake.Length, ep);
                MemoryStream ms = new MemoryStream(udp.Receive(ref ep));
                BinaryReader br = new BinaryReader(ms, Encoding.UTF8);
                ms.Seek(4, SeekOrigin.Begin);

                Header = br.ReadByte();
                if (Header != 0x56)
                {
                    //Get challenge number, and plan to resend it.
                    byte[] Response = { 0xFF, 0xFF, 0xFF, 0xFF, 0x56, br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte() };

                    br.Close();
                    ms.Close();
                    udp.Send(Response, Response.Length, ep);
                    ms = new MemoryStream(udp.Receive(ref ep));
                    br = new BinaryReader(ms, Encoding.UTF8);
                    ms.Seek(4, SeekOrigin.Begin);
                    Header = br.ReadByte();
                }

                RuleCount = br.ReadInt16();
                for (int i = 0; i < RuleCount; i++)
                {
                    var Name = A2S_SHARED.ReadNullTerminatedString(ref br);
                    var Value = A2S_SHARED.ReadNullTerminatedString(ref br);
                    Rule r = new Rule
                    {
                        Name = Name,
                        Value = Value
                    };
                    Rules.Add(r);
                }

                br.Close();
                ms.Close();
                udp.Close();
                ServerUp = true;
            }
            catch (EndOfStreamException)
            {
                ServerUp = true;
            }
            catch (SocketException)
            {
                ServerUp = false;
            }
            catch (Exception Ex)
            {
                LoggerService.Log(LogSeverity.Error, "Failed to query via A2S.", Ex);
                ServerUp = false;
            }
        }
    }

    public static class A2S_SHARED
    {
        internal static string ReadNullTerminatedString(ref BinaryReader input)
        {
            StringBuilder sb = new StringBuilder();
            char read = input.ReadChar();
            while (read != '\x00')
            {
                sb.Append(read);
                read = input.ReadChar();
            }
            return sb.ToString();
        }
    }

    public class MCStatus
    {
        const ushort dataSize = 512; // this will hopefully suffice since the MotD should be <=59 characters
        const ushort numFields = 6;  // number of values expected from server

        public string Address { get; set; }
        public ushort Port { get; set; }
        public string Motd { get; set; }
        public string Version { get; set; }
        public string CurrentPlayers { get; set; }
        public string MaximumPlayers { get; set; }
        public bool ServerUp { get; set; }
        public long Delay { get; set; }

        public MCStatus(string address, ushort port)
        {
            var rawServerData = new byte[dataSize];

            Address = address;
            Port = port;

            try
            {
                var stopWatch = new Stopwatch();
                var tcpclient = new TcpClient
                {
                    ReceiveTimeout = 5000,
                    SendTimeout = 5000
                };
                stopWatch.Start();
                tcpclient.Connect(address, port);
                stopWatch.Stop();
                var stream = tcpclient.GetStream();
                var payload = new byte[] { 0xFE, 0x01 };
                stream.Write(payload, 0, payload.Length);
                stream.Read(rawServerData, 0, dataSize);
                tcpclient.Close();
                tcpclient.Dispose();
                Delay = stopWatch.ElapsedMilliseconds;
            }
            catch (Exception)
            {
                ServerUp = false;
                return;
            }

            if (rawServerData.Length == 0)
            {
                ServerUp = false;
            }
            else
            {
                var serverData = Encoding.Unicode.GetString(rawServerData).Split("\u0000\u0000\u0000".ToCharArray());
                if (serverData.Length >= numFields)
                {
                    ServerUp = true;
                    Version = serverData[2];
                    Motd = serverData[3];
                    CurrentPlayers = serverData[4];
                    MaximumPlayers = serverData[5];
                }
                else
                {
                    ServerUp = false;
                }
            }
        }
    }

    /// <summary>
    /// William's proud of himself for writing this class.
    /// </summary>
    public class MinecraftStatus
    {
        private readonly byte[] SESSION_HANDSHAKE = { 0xFE, 0xFD, 0x09, 0x00, 0x00, 0x00, 0x01 };
        public bool ServerUp { get; }
        public int MaxPlayers { get; }
        public string IP { get; }
        public ushort Port { get; }
        public string GameID { get; }
        public string GameType { get; }
        public string Plugins { get; }
        public string Version { get; }
        public string Map { get; }
        public string MOTD { get; }
        public string[] Players { get; }

        public MinecraftStatus(IPEndPoint Server)
        {
            UdpClient Udp = null;
            MemoryStream MemoryStream = null;
            BinaryReader BinaryReader = null;
            try
            {
                Udp = new UdpClient
                {
                    Client =
                    {
                        SendTimeout = 5000,
                        ReceiveTimeout = 5000
                    }
                };

                Udp.Send(SESSION_HANDSHAKE, SESSION_HANDSHAKE.Length, Server);
                MemoryStream = new MemoryStream(Udp.Receive(ref Server));
                BinaryReader = new BinaryReader(MemoryStream, Encoding.UTF8);
                MemoryStream.Seek(5, SeekOrigin.Begin);
                var challengeString = A2S_SHARED.ReadNullTerminatedString(ref BinaryReader);
                var challengeNumber = int.Parse(challengeString);
                var bytes = BitConverter.GetBytes(challengeNumber);

                // Save challenge token.
                byte[] Response =
                {
                    0xFE, 0xFD, 0x00, 0x00, 0x00, 0x00, 0x01, bytes[3], bytes[2], bytes[1], bytes[0], 0x00, 0x00, 0x00,
                    0x00
                };

                BinaryReader.Close();
                MemoryStream.Close();
                Udp.Send(Response, Response.Length, Server);
                MemoryStream = new MemoryStream(Udp.Receive(ref Server));
                BinaryReader = new BinaryReader(MemoryStream, Encoding.UTF8);
                MemoryStream.Seek(1 + 4 + 11, SeekOrigin.Begin);

                string Input;
                do
                {
                    Input = A2S_SHARED.ReadNullTerminatedString(ref BinaryReader);
                    var Value = A2S_SHARED.ReadNullTerminatedString(ref BinaryReader).Trim();
                    switch (Input.Trim().ToLower())
                    {
                        case "numplayers":
                            Players = new string[int.Parse(Value)];
                            break;
                        case "maxplayers":
                            MaxPlayers = int.Parse(Value);
                            break;
                        case "game_id":
                            GameID = Value;
                            break;
                        case "gametype":
                            GameType = Value;
                            break;
                        case "hostip":
                            IP = Value;
                            break;
                        case "hostport":
                            Port = ushort.Parse(Value);
                            break;
                        case "hostname":
                            MOTD = Value;
                            break;
                        case "plugins":
                            Plugins = Value;
                            break;
                        case "map":
                            Map = Value;
                            break;
                        case "version":
                            Version = Value;
                            break;
                    }
                } while (Input.Length != 0);

                var CurrentIndex = 0;
                do
                {
                    Input = A2S_SHARED.ReadNullTerminatedString(ref BinaryReader);
                    if(Input.Length >= 2 && Players != null && CurrentIndex < Players.Length)
                        Players[CurrentIndex++] = Input;
                } while (true);
            }
            catch (EndOfStreamException)
            {
                ServerUp = true;
            }
            catch (SocketException)
            {
                ServerUp = false;
            }
            catch (Exception Ex)
            {
                LoggerService.Log(LogSeverity.Error, "Failed to query via MCStatusFull.", Ex);
                ServerUp = false;
            }
            finally
            {
                BinaryReader?.Close();
                MemoryStream?.Close();
                Udp?.Close();
            }
        }
    }

    public class MinecraftRCON : IDisposable
    {
        private static List<MinecraftRCON> ReusableRCONSockets = new List<MinecraftRCON>();
        private static readonly byte[] MultiPacketRequestEnd = MakePacketData("", PacketType.SERVERDATA_RESPONSE_VALUE, 0);
        private static readonly byte[] MultiPacketResponseEnd = {0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 };
        public ulong Owner { get; set; }
        public bool Disposed { get; private set; }
        private Socket Client;
        private const ushort RX_SIZE = 4096;
        private enum PacketType {SERVERDATA_RESPONSE_VALUE = 0, SERVERDATA_EXECCOMMAND = 2, SERVERDATA_AUTH = 3}
        public enum RCONStatus {CONN_FAIL, AUTH_FAIL, EXEC_FAIL, INT_FAIL, SUCCESS}
        public RCONStatus Status { get; private set; }
        public string Data { get; private set; }
        private string Password { get; }
        public IPEndPoint Server { get; }

        public MinecraftRCON(IPEndPoint Server, string Password, bool Reuse = false, string Command = null)
        {
            this.Password = Password;
            Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveTimeout = 5000,
                SendTimeout = 5000
            };
            this.Server = Server;
            try
            {
                if (!Client.ConnectAsync(Server).Wait(5000))
                {
                    LoggerService.Log(LogSeverity.Verbose, $"Failed to connect to {Server.Address} at {Server.Port}.");
                    Status = RCONStatus.CONN_FAIL;
                    return;
                }
            }
            catch (Exception)
            {
                Status = RCONStatus.CONN_FAIL;
                return;
            }

            if(Authenticate() && Command != null)
                Execute(Command);
            if (Status == RCONStatus.SUCCESS && Reuse)
            {
                ReusableRCONSockets.Add(this);
            }
        }

        public bool Authenticate()
        {
            var PreAlloc = new byte[RX_SIZE];
            return Authenticate(ref PreAlloc);
        }

        public void Execute(string Command, bool Reuse = false)
        {
            var PreAlloc = new byte[RX_SIZE];
            Execute(Command, ref PreAlloc, Reuse);
        }

        public bool Authenticate(ref byte[] RXData)
        {
            try
            {
                var Payload = MakePacketData(Password, PacketType.SERVERDATA_AUTH, 0);
                Client.Send(Payload);
                Client.Receive(RXData);
                var ID = LittleEndianReader(ref RXData, 4);
                var Type = LittleEndianReader(ref RXData, 8);
                if (ID == -1 || Type != 2)
                {
                    Status = RCONStatus.AUTH_FAIL;
                    return false;
                }

                Status = RCONStatus.SUCCESS;
                return true;
            }
            catch (Exception)
            {
                Status = RCONStatus.INT_FAIL;
                return false;
            }
        }

        public void Execute(string Command, ref byte[] RXData, bool Reuse = false)
        {
            var StringConcat = new StringBuilder();
            var PacketCount = 0;
            try
            {
                var Payload = MakePacketData(Command, PacketType.SERVERDATA_EXECCOMMAND, 0);
                Client.Send(Payload);
                //Client.Send(MultiPacketRequestEnd);
                var End = false;
                do
                {
                    Wipe(ref RXData);
                    // Subtract 1 to ignore null.
                    var Size = Client.Receive(RXData) - 1;
#if DEBUG
                    using (var fs = new FileStream($"packet{PacketCount}", FileMode.Create, FileAccess.Write))
                        fs.Write(RXData, 0, RXData.Length);
                    StringConcat.Append($"\nPacket {PacketCount}\n\n");
#endif
                    var ID = LittleEndianReader(ref RXData, 4);
                    var Type = LittleEndianReader(ref RXData, 8);
                    if ((ID == -1 || Type != 0) && PacketCount == 0)
                    {
                        LoggerService.Log(LogSeverity.Verbose,
                            $"Failed to execute \"{Command}\", type of {Type} after reading {PacketCount} packets!!");
                        Status = RCONStatus.AUTH_FAIL;
                        return;
                    }

                    var Position = PacketCount == 0 ? 12 : 4;
                    var CurrentChar = (char) RXData[Position++];
                    var PeekChar = Position < RXData.Length ? (char) RXData[Position] : '\x00';
                    while (Position < Size)
                    {
                        if (CurrentChar != '\x00' && RXData[Position] != 0)
                            StringConcat.Append(CurrentChar);
                        CurrentChar = (char) RXData[Position++];
                        
                    }
                    if (RXData[Size - 1] == '\x00')
                        End = true;

                    PacketCount++;
                    LoggerService.Log(LogSeverity.Verbose,
                        $"Finished reading {PacketCount} packets. Last position: {Position}, Size: {Size}");
                    if (PacketCount == 20)
                    {
                        LoggerService.Log(LogSeverity.Verbose,
                            "Something internally failed; attempted to read infinite packets!");
                        End = true;
                    }
                } while (!End);

                LoggerService.Log(LogSeverity.Verbose, $"Read {PacketCount} packets!");
                Data = StringConcat.ToString();
                Status = RCONStatus.SUCCESS;
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10060)
                {
                    Status = RCONStatus.SUCCESS;
                    Data = StringConcat.ToString();
                }
                else
                {
                    Status = RCONStatus.INT_FAIL;
                    LoggerService.Log(LogSeverity.Verbose, "Something went wrong while querying!", ex);
                }
            }
            catch (Exception ex)
            {
                Status = RCONStatus.INT_FAIL;
                LoggerService.Log(LogSeverity.Verbose, "Something went wrong while querying!", ex);
            }
            if(Status != RCONStatus.SUCCESS || !Reuse)
                Dispose();
        }

        private static bool IsEndByte(ref byte[] data)
        {
            return false;
        }

        private static byte[] LittleEndianConverter(int data)
        {
            var b = new byte[4];
            b[0] = (byte)data;
            b[1] = (byte)(((uint)data >> 8) & 0xFF);
            b[2] = (byte)(((uint)data >> 16) & 0xFF);
            b[3] = (byte)(((uint)data >> 24) & 0xFF);
            return b;
        }

        private static int LittleEndianReader(ref byte[] data, int startIndex)
        {
            return (data[startIndex + 3] << 24)
                   | (data[startIndex + 2] << 16)
                   | (data[startIndex + 1] << 8)
                   | data[startIndex];
        }

        private static void Wipe(ref byte[] data)
        {
            for (var i = 0; i < data.Length; i++)
                data[i] = (byte) '\x00';
        }

        private static byte[] MakePacketData(string Body, PacketType Type, int ID)
        {
            var Length = LittleEndianConverter(Body.Length + 9);
            var IDData = LittleEndianConverter(ID);
            var PacketType = LittleEndianConverter((int) Type);
            var BodyData = Encoding.UTF8.GetBytes(Body);
            // Plus 1 for the null byte.
            var Packet = new byte[Length.Length + IDData.Length + PacketType.Length + BodyData.Length + 1];
            var Counter = 0;
            foreach (var Byte in Length)
                Packet[Counter++] = Byte;
            foreach (var Byte in IDData)
                Packet[Counter++] = Byte;
            foreach (var Byte in PacketType)
                Packet[Counter++] = Byte;
            foreach (var Byte in BodyData)
                Packet[Counter++] = Byte;
            return Packet;
        }
        public bool Connected()
        {
            if (Status != RCONStatus.SUCCESS) return false;
            try
            {
                return !(Client.Poll(1, SelectMode.SelectRead) && Client.Available == 0);
            }
            catch (SocketException) { return false; }
        }

        public void Dispose()
        {
            Disposed = true;
            Client?.Dispose();
            ReusableRCONSockets.Remove(this);
        }

        public static MinecraftRCON GetRCON(ulong User)
        {
            var Saved = ReusableRCONSockets.Where(o => o.Owner == User).ToList();
            if (Saved.Count != 0)
                if (Saved[0].Connected())
                    return Saved[0];
                else
                    Saved[0].Dispose();
            return null;
        }

        public static void DisposeAll()
        {
            while(ReusableRCONSockets.Count != 0)
                ReusableRCONSockets[0].Dispose();
        }
    }
}