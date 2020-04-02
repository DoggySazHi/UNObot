using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            var addresses = Dns.GetHostAddresses(ip);
            if (!parseCheck)
            {
                if (addresses.Length == 0)
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
            var addresses = Dns.GetHostAddresses(ip);
            if (!parseCheck)
            {
                if (addresses.Length == 0)
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
            var addresses = Dns.GetHostAddresses(ip);
            if (!parseCheck)
            {
                if (addresses.Length == 0)
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
        public byte Header { get; set; }        // I
        public byte Protocol { get; set; }
        public string Name { get; set; }
        public string Map { get; set; }
        public string Folder { get; set; }
        public string Game { get; set; }
        public short ID { get; set; }
        public byte Players { get; set; }
        public byte MaxPlayers { get; set; }
        public byte Bots { get; set; }
        public ServerTypeFlags ServerType { get; set; }
        public EnvironmentFlags Environment { get; set; }
        public VisibilityFlags Visibility { get; set; }
        public VACFlags VAC { get; set; }
        public string Version { get; set; }
        public ExtraDataFlags ExtraDataFlag { get; set; }
        #region Extra Data Flag Members
        public ulong GameID { get; set; }           //0x01
        public ulong SteamID { get; set; }          //0x10
        public string Keywords { get; set; }        //0x20
        public string Spectator { get; set; }       //0x40
        public short SpectatorPort { get; set; }   //0x40
        public short Port { get; set; }             //0x80
        public bool ServerUp { get; set; }

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
                var tcpclient = new TcpClient();
                stopWatch.Start();
                tcpclient.Connect(address, port);
                stopWatch.Stop();
                var stream = tcpclient.GetStream();
                var payload = new byte[] { 0xFE, 0x01 };
                stream.Write(payload, 0, payload.Length);
                stream.Read(rawServerData, 0, dataSize);
                tcpclient.Close();
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
}