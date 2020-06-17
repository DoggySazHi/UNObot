using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Discord;
using UNObot.Interop;
using static UNObot.Services.IRCON;

namespace UNObot.Services
{
    //Adopted from Valve description: https://developer.valvesoftware.com/wiki/Server_queries#A2S_INFO
    //Thanks to https://www.techpowerup.com/forums/threads/snippet-c-net-steam-a2s_info-query.229199/ for A2S_INFO, self-reimplemented for A2S_PLAYER and A2S_RULES
    //Dealing with network-level stuff is hard

    //Credit to https://github.com/maxime-paquatte/csharp-minecraft-query/blob/master/src/Status.cs
    //Mukyu... but I implemented the Minecraft RCON (Valve RCON) protocol by hand, as well as the query.
    public static class QueryHandlerService
    {
        public static IReadOnlyList<string> OutsideServers;
        public static IReadOnlyDictionary<ushort, RCONServer> SpecialServers;

        static QueryHandlerService()
        {
            var external = new List<string>();
            external.Add("williamle.com");
            OutsideServers = external;
            var servers = new Dictionary<ushort, RCONServer>();
            // Stored in plain-text anyways, plus is server-side. You could easily read this from a file on the same server.
            servers.Add(27285, new RCONServer {Server = "192.168.2.6", RCONPort = 27286, Password = "mukyumukyu"});
            servers.Add(29292, new RCONServer {Server = "192.168.2.11", RCONPort = 29293, Password = "mukyumukyu"});
            SpecialServers = servers;
        }

        public static string HumanReadable(float time)
        {
            var formatted = TimeSpan.FromSeconds(time);
            string output;
            if (formatted.Hours != 0)
                output = $"{(int) formatted.TotalHours}:{formatted.Minutes:00}:{formatted.Seconds:00}";
            else if (formatted.Minutes != 0)
                output = $"{formatted.Minutes}:{formatted.Seconds:00}";
            else
                output = $"{formatted.Seconds} second{(formatted.Seconds == 1 ? "" : "s")}";
            return output;
        }

        public static bool GetInfo(string ip, ushort port, out A2SInfo output)
        {
            var success = TryParseServer(ip, port, out var iPEndPoint);
            if (!success)
            {
                output = null;
                return false;
            }

            output = new A2SInfo(iPEndPoint);
            return output.ServerUp;
        }

        public static bool GetPlayers(string ip, ushort port, out A2SPlayer output)
        {
            var success = TryParseServer(ip, port, out var iPEndPoint);
            if (!success)
            {
                output = null;
                return false;
            }

            output = new A2SPlayer(iPEndPoint);
            return output.ServerUp;
        }

        public static bool GetRules(string ip, ushort port, out A2SRules output)
        {
            var success = TryParseServer(ip, port, out var iPEndPoint);
            if (!success)
            {
                output = null;
                return false;
            }

            output = new A2SRules(iPEndPoint);
            return output.ServerUp;
        }

        public static bool GetInfoMCNew(string ip, ushort port, out MinecraftStatus output)
        {
            var success = TryParseServer(ip, port, out var iPEndPoint);
            if (!success)
            {
                output = null;
                return false;
            }

            output = new MinecraftStatus(iPEndPoint);
            return output.ServerUp;
        }

        public static bool SendRCON(string ip, ushort port, string command, string password, out IRCON output)
        {
            var success = TryParseServer(ip, port, out var iPEndPoint);
            if (!success)
            {
                output = null;
                return false;
            }

            output = RCONManager.GetSingleton().CreateRCON(iPEndPoint, password, false, command);
            return output.Status == RCONStatus.Success;
        }

        public static bool CreateRCON(string ip, ushort port, string password, ulong user, out IRCON output)
        {
            var possibleRCON = RCONManager.GetSingleton().GetRCON(user);
            if (possibleRCON != null)
            {
                output = possibleRCON;
                return false;
            }

            var success = TryParseServer(ip, port, out var iPEndPoint);
            if (!success)
            {
                output = null;
                return false;
            }

            output = RCONManager.GetSingleton().CreateRCON(iPEndPoint, password, true);
            output.Owner = user;
            return output.Status == RCONStatus.Success;
        }

        public static bool ExecuteRCON(ulong user, string command, out IRCON output)
        {
            var possibleRCON = RCONManager.GetSingleton().GetRCON(user);
            output = possibleRCON;
            if (possibleRCON == null)
                return false;

            possibleRCON.Execute(command, true);
            return output.Status == RCONStatus.Success;
        }

        public static bool CloseRCON(ulong user)
        {
            var possibleRCON = RCONManager.GetSingleton().GetRCON(user);
            if (possibleRCON == null)
                return false;

            possibleRCON.Dispose();
            return true;
        }

        private static bool TryParseServer(string ip, ushort port, out IPEndPoint iPEndPoint)
        {
            var parseCheck = IPAddress.TryParse(ip, out var server);
            var addresses = ResolveDns(ip);
            if (!parseCheck)
            {
                if (addresses == null || addresses.Length == 0)
                {
                    iPEndPoint = null;
                    return false;
                }

                server = addresses[0];
            }

            iPEndPoint = new IPEndPoint(server, port);
            return true;
        }

        private static IPAddress[] ResolveDns(string ip)
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
        {
            return new MCStatus(ip, port);
        }

        public struct RCONServer
        {
            public string Server { get; set; }
            public ushort RCONPort { get; set; }
            public string Password { get; set; }
        }
    }

    public class A2SInfo
    {
        // \xFF\xFF\xFF\xFFTSource Engine Query\x00 because UTF-8 doesn't like to encode 0xFF
        public static readonly byte[] Request =
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65,
            0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00
        };

        public A2SInfo(IPEndPoint ep)
        {
            try
            {
                var udp = new UdpClient();
                udp.Client.SendTimeout = 5000;
                udp.Client.ReceiveTimeout = 5000;
                udp.Send(Request, Request.Length, ep);
                var ms = new MemoryStream(udp.Receive(ref ep)); // Saves the received data in a memory buffer
                var br = new BinaryReader(ms, Encoding.UTF8); // A binary reader that treats charaters as Unicode 8-bit
                ms.Seek(4, SeekOrigin.Begin); // skip the 4 0xFFs
                Header = br.ReadByte();
                Protocol = br.ReadByte();
                Name = A2SShared.ReadNullTerminatedString(ref br);
                Map = A2SShared.ReadNullTerminatedString(ref br);
                Folder = A2SShared.ReadNullTerminatedString(ref br);
                Game = A2SShared.ReadNullTerminatedString(ref br);
                Id = br.ReadInt16();
                Players = br.ReadByte();
                MaxPlayers = br.ReadByte();
                Bots = br.ReadByte();
                ServerType = (ServerTypeFlags) br.ReadByte();
                Environment = (EnvironmentFlags) br.ReadByte();
                Visibility = (VisibilityFlags) br.ReadByte();
                Vac = (VacFlags) br.ReadByte();
                Version = A2SShared.ReadNullTerminatedString(ref br);
                ExtraDataFlag = (ExtraDataFlags) br.ReadByte();

                #region These EDF readers have to be in this order because that's the way they are reported

                if (ExtraDataFlag.HasFlag(ExtraDataFlags.Port))
                    Port = br.ReadInt16();
                if (ExtraDataFlag.HasFlag(ExtraDataFlags.SteamId))
                    SteamId = br.ReadUInt64();
                if (ExtraDataFlag.HasFlag(ExtraDataFlags.Spectator))
                {
                    SpectatorPort = br.ReadInt16();
                    Spectator = A2SShared.ReadNullTerminatedString(ref br);
                }

                if (ExtraDataFlag.HasFlag(ExtraDataFlags.Keywords))
                    Keywords = A2SShared.ReadNullTerminatedString(ref br);
                if (ExtraDataFlag.HasFlag(ExtraDataFlags.GameId))
                    GameId = br.ReadUInt64();

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
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Failed to query via A2S.", ex);
                ServerUp = false;
            }
        }

        public byte Header { get; } // I
        public byte Protocol { get; }
        public string Name { get; }
        public string Map { get; }
        public string Folder { get; }
        public string Game { get; }
        public short Id { get; }
        public byte Players { get; }
        public byte MaxPlayers { get; }
        public byte Bots { get; }
        public ServerTypeFlags ServerType { get; }
        public EnvironmentFlags Environment { get; }
        public VisibilityFlags Visibility { get; }
        public VacFlags Vac { get; }
        public string Version { get; }
        public ExtraDataFlags ExtraDataFlag { get; }

        #region Strong Typing Enumerators

        [Flags]
        public enum ExtraDataFlags : byte
        {
            GameId = 0x01,
            SteamId = 0x10,
            Keywords = 0x20,
            Spectator = 0x40,
            Port = 0x80
        }

        public enum VacFlags : byte
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
            Linux = 0x6C, //l
            Windows = 0x77, //w
            Mac = 0x6D, //m
            MacOsX = 0x6F //o
        }

        public enum ServerTypeFlags : byte
        {
            Dedicated = 0x64, //d
            Nondedicated = 0x6C, //l
            SourceTv = 0x70 //p
        }

        #endregion

        #region Extra Data Flag Members

        public ulong GameId { get; } //0x01
        public ulong SteamId { get; } //0x10
        public string Keywords { get; } //0x20
        public string Spectator { get; } //0x40
        public short SpectatorPort { get; } //0x40
        public short Port { get; } //0x80
        public bool ServerUp { get; }

        #endregion
    }

    public class A2SPlayer
    {
        private static readonly byte[] Handshake = {0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF};

        public A2SPlayer(IPEndPoint ep)
        {
            try
            {
                Players = new List<Player>();
                var udp = new UdpClient();
                udp.Client.SendTimeout = 5000;
                udp.Client.ReceiveTimeout = 5000;
                udp.Send(Handshake, Handshake.Length, ep);
                var ms = new MemoryStream(udp.Receive(ref ep));
                var br = new BinaryReader(ms, Encoding.UTF8);
                ms.Seek(5, SeekOrigin.Begin);

                //Get challenge number, and plan to resend it.
                byte[] response =
                    {0xFF, 0xFF, 0xFF, 0xFF, 0x55, br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte()};

                br.Close();
                ms.Close();
                udp.Send(response, response.Length, ep);
                ms = new MemoryStream(udp.Receive(ref ep));
                br = new BinaryReader(ms, Encoding.UTF8);

                ms.Seek(4, SeekOrigin.Begin);
                Header = br.ReadByte();
                PlayerCount = br.ReadByte();
                for (var i = 0; i < Convert.ToInt32(PlayerCount); i++)
                {
                    var index = br.ReadByte();
                    var name = A2SShared.ReadNullTerminatedString(ref br);
                    var score = br.ReadInt32();
                    var duration = br.ReadSingle();
                    var p = new Player
                    {
                        Index = index,
                        Name = name,
                        Score = score,
                        Duration = duration
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
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Failed to query via A2S.", ex);
                ServerUp = false;
            }
        }

        public byte Header { get; }
        public byte PlayerCount { get; }

        public bool ServerUp { get; }
        public List<Player> Players { get; }

        public struct Player
        {
            public byte Index { get; set; }
            public string Name { get; set; }
            public long Score { get; set; }
            public float Duration { get; set; }
        }
    }

    public class A2SRules
    {
        private static readonly byte[] Handshake = {0xFF, 0xFF, 0xFF, 0xFF, 0x56, 0xFF, 0xFF, 0xFF, 0xFF};

        public A2SRules(IPEndPoint ep)
        {
            try
            {
                Rules = new List<Rule>();
                var udp = new UdpClient();
                udp.Client.SendTimeout = 5000;
                udp.Client.ReceiveTimeout = 5000;
                udp.Send(Handshake, Handshake.Length, ep);
                var ms = new MemoryStream(udp.Receive(ref ep));
                var br = new BinaryReader(ms, Encoding.UTF8);
                ms.Seek(4, SeekOrigin.Begin);

                Header = br.ReadByte();
                if (Header != 0x56)
                {
                    //Get challenge number, and plan to resend it.
                    byte[] response =
                        {0xFF, 0xFF, 0xFF, 0xFF, 0x56, br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte()};

                    br.Close();
                    ms.Close();
                    udp.Send(response, response.Length, ep);
                    ms = new MemoryStream(udp.Receive(ref ep));
                    br = new BinaryReader(ms, Encoding.UTF8);
                    ms.Seek(4, SeekOrigin.Begin);
                    Header = br.ReadByte();
                }

                RuleCount = br.ReadInt16();
                for (var i = 0; i < RuleCount; i++)
                {
                    var name = A2SShared.ReadNullTerminatedString(ref br);
                    var value = A2SShared.ReadNullTerminatedString(ref br);
                    var r = new Rule
                    {
                        Name = name,
                        Value = value
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
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Failed to query via A2S.", ex);
                ServerUp = false;
            }
        }

        public byte Header { get; }
        public short RuleCount { get; }
        public List<Rule> Rules { get; }
        public bool ServerUp { get; }

        public struct Rule
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }

    public static class A2SShared
    {
        internal static string ReadNullTerminatedString(ref BinaryReader input)
        {
            var sb = new StringBuilder();
            var read = input.ReadChar();
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
        private const ushort DataSize = 512; // this will hopefully suffice since the MotD should be <=59 characters
        private const ushort NumFields = 6; // number of values expected from server

        public MCStatus(string address, ushort port)
        {
            var rawServerData = new byte[DataSize];

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
                var payload = new byte[] {0xFE, 0x01};
                stream.Write(payload, 0, payload.Length);
                stream.Read(rawServerData, 0, DataSize);
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
                if (serverData.Length >= NumFields)
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

        public string Address { get; set; }
        public ushort Port { get; set; }
        public string Motd { get; set; }
        public string Version { get; set; }
        public string CurrentPlayers { get; set; }
        public string MaximumPlayers { get; set; }
        public bool ServerUp { get; set; }
        public long Delay { get; set; }
    }

    /// <summary>
    ///     William's proud of himself for writing this class.
    /// </summary>
    public class MinecraftStatus
    {
        private readonly byte[] _sessionHandshake = {0xFE, 0xFD, 0x09, 0x00, 0x00, 0x00, 0x01};

        public MinecraftStatus(IPEndPoint server)
        {
            UdpClient udp = null;
            MemoryStream memoryStream = null;
            BinaryReader binaryReader = null;
            try
            {
                udp = new UdpClient
                {
                    Client =
                    {
                        SendTimeout = 5000,
                        ReceiveTimeout = 5000
                    }
                };

                udp.Send(_sessionHandshake, _sessionHandshake.Length, server);
                memoryStream = new MemoryStream(udp.Receive(ref server));
                binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);
                memoryStream.Seek(5, SeekOrigin.Begin);
                var challengeString = A2SShared.ReadNullTerminatedString(ref binaryReader);
                var challengeNumber = int.Parse(challengeString);
                var bytes = BitConverter.GetBytes(challengeNumber);

                // Save challenge token.
                byte[] response =
                {
                    0xFE, 0xFD, 0x00, 0x00, 0x00, 0x00, 0x01, bytes[3], bytes[2], bytes[1], bytes[0], 0x00, 0x00, 0x00,
                    0x00
                };

                binaryReader.Close();
                memoryStream.Close();
                udp.Send(response, response.Length, server);
                memoryStream = new MemoryStream(udp.Receive(ref server));
                binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);
                memoryStream.Seek(1 + 4 + 11, SeekOrigin.Begin);

                string input;
                do
                {
                    input = A2SShared.ReadNullTerminatedString(ref binaryReader);
                    var value = A2SShared.ReadNullTerminatedString(ref binaryReader).Trim();
                    switch (input.Trim().ToLower())
                    {
                        case "numplayers":
                            Players = new string[int.Parse(value)];
                            break;
                        case "maxplayers":
                            MaxPlayers = int.Parse(value);
                            break;
                        case "game_id":
                            GameId = value;
                            break;
                        case "gametype":
                            GameType = value;
                            break;
                        case "hostip":
                            Ip = value;
                            break;
                        case "hostport":
                            Port = ushort.Parse(value);
                            break;
                        case "hostname":
                            Motd = value;
                            break;
                        case "plugins":
                            Plugins = value;
                            break;
                        case "map":
                            Map = value;
                            break;
                        case "version":
                            Version = value;
                            break;
                    }
                } while (input.Length != 0);

                var currentIndex = 0;
                do
                {
                    input = A2SShared.ReadNullTerminatedString(ref binaryReader);
                    if (input.Length >= 2 && Players != null && currentIndex < Players.Length)
                        Players[currentIndex++] = input;
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
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Failed to query via MCStatusFull.", ex);
                ServerUp = false;
            }
            finally
            {
                binaryReader?.Close();
                memoryStream?.Close();
                udp?.Close();
            }
        }

        public bool ServerUp { get; }
        public int MaxPlayers { get; }
        public string Ip { get; }
        public ushort Port { get; }
        public string GameId { get; }
        public string GameType { get; }
        public string Plugins { get; }
        public string Version { get; }
        public string Map { get; }
        public string Motd { get; }
        public string[] Players { get; }
    }

    public interface IRCON : IDisposable
    {
        public enum RCONStatus
        {
            ConnFail,
            AuthFail,
            ExecFail,
            IntFail,
            Success
        }

        public ulong Owner { get; set; }
        public IPEndPoint Server { get; }
        public RCONStatus Status { get; }
        public string Data { get; }
        public bool Disposed { get; }
        public void Execute(string command, bool reuse = false);
        public void ExecuteSingle(string command, bool reuse = false);
        public bool Connected();
    }

    public class RCONManager : IDisposable
    {
        private static RCONManager _instance;
        private List<IRCON> _reusableRCONSockets;

        private RCONManager()
        {
            _reusableRCONSockets = new List<IRCON>();
        }

        public void Dispose()
        {
            _reusableRCONSockets.ForEach(o => o.Dispose());
            _reusableRCONSockets.Clear();
            _reusableRCONSockets = null;
        }

        public static RCONManager GetSingleton()
        {
            return _instance ??= new RCONManager();
        }

        public IRCON GetRCON(ulong user)
        {
            var saved = _reusableRCONSockets.Where(o => o.Owner == user).ToList();
            if (saved.Count == 0) return null;
            if (!saved[0].Disposed && saved[0].Connected())
                return saved[0];
            saved[0].Dispose();
            return null;
        }

        public IRCON CreateRCON(IPEndPoint server, string password, bool reuse = false, string command = null)
        {
            IRCON rconInstance;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                rconInstance = new RCONHelper(server, password, command);
            else
                rconInstance = new MinecraftRCON(server, password, reuse, command);
            if (reuse)
                _reusableRCONSockets.Add(rconInstance);
            return rconInstance;
        }
    }

    internal class MinecraftRCON : IRCON
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
        public RCONStatus Status { get; private set; }
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
            if (Status != RCONStatus.Success) return false;
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
                    LoggerService.Log(LogSeverity.Verbose, $"Failed to connect to {Server.Address} at {Server.Port}.");
                    Status = RCONStatus.ConnFail;
                    return;
                }
            }
            catch (Exception)
            {
                Status = RCONStatus.ConnFail;
                return;
            }

            LoggerService.Log(LogSeverity.Verbose, "Successfully created RCON connection!");
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
                    Status = RCONStatus.AuthFail;
                    LoggerService.Log(LogSeverity.Verbose, "RCON failed to authenticate!");
                    return false;
                }

                LoggerService.Log(LogSeverity.Verbose, "RCON login successful!");
                Status = RCONStatus.Success;
                return true;
            }
            catch (Exception)
            {
                Status = RCONStatus.IntFail;
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
                        LoggerService.Log(LogSeverity.Verbose, "Sending payload...");
                        _client.Send(payload);
                    }
                    catch (ObjectDisposedException)
                    {
                        LoggerService.Log(LogSeverity.Warning, "Socket was disposed, attempting to re-auth...");
                        CreateConnection(reuse);
                        _client.Send(payload);
                    }

                    LoggerService.Log(LogSeverity.Verbose, "Sending bad type...");
                    _client.Send(EndOfCommandPacket);
                    LoggerService.Log(LogSeverity.Verbose, $"Now reading... Connection status: {Connected()}");
                    var end = false;
                    do
                    {
                        Wipe(ref rxData);
                        var size = _client.Receive(rxData);
                        if (size == 0)
                        {
                            // Connection failed.
                            LoggerService.Log(LogSeverity.Warning, "Failed to execute. Attempting to retry...");
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
                            LoggerService.Log(LogSeverity.Verbose,
                                $"Failed to execute \"{command}\", type of {type}!");
                            Status = RCONStatus.AuthFail;
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
                        LoggerService.Log(LogSeverity.Debug, $"Packet {packetCount}, Size {size}");

                        // Excess packets.
                        if (packetCount == 100)
                        {
                            LoggerService.Log(LogSeverity.Warning, $"Over-read {packetCount} packets!");
                            end = true;
                        }
                    } while (!end);

                    Data = Stringifier(ref _packetCollector);
                    _packetCollector.Clear();
                    LoggerService.Log(LogSeverity.Verbose, command + "\n\n" + Data);
                    Status = RCONStatus.Success;
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10060 && _packetCollector.Count > 0)
                    {
                        LoggerService.Log(LogSeverity.Warning,
                            "Timed out, but got data... did we try to read another packet?", ex);
                        Status = RCONStatus.Success;
                        Data = Stringifier(ref _packetCollector);
                        LoggerService.Log(LogSeverity.Verbose, Data);
                    }
                    else if ((ex.ErrorCode == 10053 || ex.ErrorCode == 32) && reuse)
                    {
                        LoggerService.Log(LogSeverity.Warning,
                            "We were closed, however attempting to reopen connection...", ex);
                        Status = RCONStatus.IntFail;
                        CreateConnection(true);
                    }
                    else
                    {
                        Status = RCONStatus.IntFail;
                        LoggerService.Log(LogSeverity.Verbose, "Something went wrong while querying!", ex);
                    }
                }
                catch (Exception ex)
                {
                    Status = RCONStatus.IntFail;
                    LoggerService.Log(LogSeverity.Verbose, "Something went wrong while querying!", ex);
                }

                if (Status != RCONStatus.Success || !reuse)
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
                    LoggerService.Log(LogSeverity.Verbose, $"Failed to execute \"{command}\"!");
                    Status = RCONStatus.AuthFail;
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
                Status = RCONStatus.Success;
            }
            catch (Exception)
            {
                Status = RCONStatus.IntFail;
            }

            if (Status != RCONStatus.Success || !reuse)
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