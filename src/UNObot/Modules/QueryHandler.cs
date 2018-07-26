﻿using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UNObot.Modules
{
    //Adopted from Valve description: https://developer.valvesoftware.com/wiki/Server_queries#A2S_INFO
    //Thanks to https://www.techpowerup.com/forums/threads/snippet-c-net-steam-a2s_info-query.229199/ for the snippet
    //Dealing with network-level stuff is hard
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
        #endregion
        public A2S_INFO(IPEndPoint ep)
        {
            UdpClient udp = new UdpClient();
            udp.Send(REQUEST, REQUEST.Length, ep);
            MemoryStream ms = new MemoryStream(udp.Receive(ref ep));    // Saves the received data in a memory buffer
            BinaryReader br = new BinaryReader(ms, Encoding.UTF8);      // A binary reader that treats charaters as Unicode 8-bit
            ms.Seek(4, SeekOrigin.Begin);   // skip the 4 0xFFs
            Header = br.ReadByte();
            Protocol = br.ReadByte();
            Name = ReadNullTerminatedString(ref br);
            Map = ReadNullTerminatedString(ref br);
            Folder = ReadNullTerminatedString(ref br);
            Game = ReadNullTerminatedString(ref br);
            ID = br.ReadInt16();
            Players = br.ReadByte();
            MaxPlayers = br.ReadByte();
            Bots = br.ReadByte();
            ServerType = (ServerTypeFlags)br.ReadByte();
            Environment = (EnvironmentFlags)br.ReadByte();
            Visibility = (VisibilityFlags)br.ReadByte();
            VAC = (VACFlags)br.ReadByte();
            Version = ReadNullTerminatedString(ref br);
            ExtraDataFlag = (ExtraDataFlags)br.ReadByte();
            #region These EDF readers have to be in this order because that's the way they are reported
            if (ExtraDataFlag.HasFlag(ExtraDataFlags.Port))
                Port = br.ReadInt16();
            if (ExtraDataFlag.HasFlag(ExtraDataFlags.SteamID))
                SteamID = br.ReadUInt64();
            if (ExtraDataFlag.HasFlag(ExtraDataFlags.Spectator))
            {
                SpectatorPort = br.ReadInt16();
                Spectator = ReadNullTerminatedString(ref br);
            }
            if (ExtraDataFlag.HasFlag(ExtraDataFlags.Keywords))
                Keywords = ReadNullTerminatedString(ref br);
            if (ExtraDataFlag.HasFlag(ExtraDataFlags.GameID))
                GameID = br.ReadUInt64();
            #endregion
            br.Close();
            ms.Close();
            udp.Close();
        }
        /// <summary>Reads a null-terminated string into a .NET Framework compatible string.</summary>
        /// <param name="input">Binary reader to pull the null-terminated string from.  Make sure it is correctly positioned in the stream before calling.</param>
        /// <returns>String of the same encoding as the input BinaryReader.</returns>
        public static string ReadNullTerminatedString(ref BinaryReader input)
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
        /* This is one way to use it.
        static void Main(string[] args)
        {
            bool success = GetInfo("108.61.100.48", 25445, out A2S_INFO response);
            Console.WriteLine("Got info!");
            if (!success)
            {
                Console.WriteLine("Failed to get info!");
                return;
            }
            Console.WriteLine($"Map: {response.Map}\n" +
                              $"Players: {Convert.ToInt32(response.Players)}");
            return;
        }
        */
    }

    //Credit to https://github.com/maxime-paquatte/csharp-minecraft-query/blob/master/src/Status.cs
    public class MCStatus
    {
        const Byte Statistic = 0x00;
        const Byte Handshake = 0x09;

        private readonly Dictionary<string, string> _keyValues;
        private List<string> _players;

        public string MessageOfTheDay
        {
            get { return _keyValues["hostname"]; }
        }

        public string Gametype
        {
            get { return _keyValues["gametype"]; }
        }

        public string GameId
        {
            get { return _keyValues["game_id"]; }
        }

        public string Version
        {
            get { return _keyValues["version"]; }
        }

        public string Plugins
        {
            get { return _keyValues["plugins"]; }
        }

        public string Map
        {
            get { return _keyValues["map"]; }
        }
        public string NumPlayers
        {
            get { return _keyValues["numplayers"]; }
        }
        public string MaxPlayers
        {
            get { return _keyValues["maxplayers"]; }
        }
        public string HostPort
        {
            get { return _keyValues["hostport"]; }
        }
        public string HostIp
        {
            get { return _keyValues["hostip"]; }
        }

        public IEnumerable<string> Players
        {
            get { return _players; }
        }

        internal MCStatus(byte[] message)
        {
            _keyValues = new Dictionary<string, string>();
            _players = new List<string>();

            var buffer = new byte[256];
            Stream stream = new MemoryStream(message);

            stream.Read(buffer, 0, 5);// Read Type + SessionID
            stream.Read(buffer, 0, 11); // Padding: 11 bytes constant
            var constant1 = new byte[] { 0x73, 0x70, 0x6C, 0x69, 0x74, 0x6E, 0x75, 0x6D, 0x00, 0x80, 0x00 };
            for (int i = 0; i < constant1.Length; i++) Debug.Assert(constant1[i] == buffer[i], "Byte mismatch at " + i + " Val :" + buffer[i]);

            var sb = new StringBuilder();
            string lastKey = string.Empty;
            int currentByte;
            while ((currentByte = stream.ReadByte()) != -1)
            {
                if (currentByte == 0x00)
                {
                    if (!string.IsNullOrEmpty(lastKey))
                    {
                        _keyValues.Add(lastKey, sb.ToString());
                        lastKey = string.Empty;
                    }
                    else
                    {
                        lastKey = sb.ToString();
                        if (string.IsNullOrEmpty(lastKey)) break;
                    }
                    sb.Clear();
                }
                else sb.Append((char)currentByte);
            }

            stream.Read(buffer, 0, 10); // Padding: 10 bytes constant
            var constant2 = new byte[] { 0x01, 0x70, 0x6C, 0x61, 0x79, 0x65, 0x72, 0x5F, 0x00, 0x00 };
            for (int i = 0; i < constant2.Length; i++) Debug.Assert(constant2[i] == buffer[i], "Byte mismatch at " + i + " Val :" + buffer[i]);

            while ((currentByte = stream.ReadByte()) != -1)
            {
                if (currentByte == 0x00)
                {
                    var player = sb.ToString();
                    if (string.IsNullOrEmpty(player)) break;
                    _players.Add(player);
                    sb.Clear();
                }
                else sb.Append((char)currentByte);
            }
        }


        /// <summary>
        /// Get the status of the given host and optional port
        /// </summary>
        /// <param name="host">The host name or address (monserver.com or 123.123.123.123)</param>
        /// <param name="port">The query port, by default is 25565</param>
        public static MCStatus GetStatus(string host, int port = 25565)
        {
            var e = new IPEndPoint(IPAddress.Any, port);
            using (var u = new UdpClient(e))
            {
                try
                {
                    var s = new UdpState { EndPoint = e, Client = u };
                    u.Connect(host, port);
                    var status = GetStatus(s);
                    return new MCStatus(status);
                }
                finally
                {
                    u.Close();
                }
            }
        }


        static byte[] GetStatus(UdpState s)
        {
            var challengeToken = GetChallengeToken(s);

            //append 4 bytes to obtains the Full status
            WriteData(s, Statistic, challengeToken, new byte[] { 0x00, 0x00, 0x00, 0x00 });
            return ReceiveMessages(s);
        }

        static byte[] GetChallengeToken(UdpState s)
        {
            WriteData(s, Handshake);

            var message = ReceiveMessages(s);

            var challangeBytes = new byte[16];
            Array.Copy(message, 5, challangeBytes, 0, message.Length - 5);
            var challengeInt = int.Parse(Encoding.ASCII.GetString(challangeBytes));
            return BitConverter.GetBytes(challengeInt).Reverse().ToArray();

        }


        static void WriteData(UdpState s, byte cmd, byte[] append = null, byte[] append2 = null)
        {
            var cmdData = new byte[] { 0xFE, 0xFD, cmd, 0x01, 0x02, 0x03, 0x04 };
            var dataLength = cmdData.Length + (append != null ? append.Length : 0) + (append2 != null ? append2.Length : 0);
            var data = new byte[dataLength];
            cmdData.CopyTo(data, 0);
            if (append != null) append.CopyTo(data, cmdData.Length);
            if (append2 != null) append2.CopyTo(data, cmdData.Length + (append != null ? append.Length : 0));
            s.Client.Send(data, data.Length);
        }

        static byte[] ReceiveMessages(UdpState s)
        {
            return s.Client.Receive(ref s.EndPoint);
        }

        class UdpState
        {
            public UdpClient Client;
            public IPEndPoint EndPoint;
        }
    }

    public static class QueryHandler
    {
        public static bool GetInfo(string ip, int port, out A2S_INFO output)
        {
            bool parseCheck = IPAddress.TryParse(ip, out IPAddress iP);
            if (!parseCheck)
            {
                output = null;
                return false;
            }
            IPEndPoint iPEndPoint = new IPEndPoint(iP, port);
            output = new A2S_INFO(iPEndPoint);
            return true;
        }

        public static MCStatus GetInfoMC(string ip, int port = 25565)
            => MCStatus.GetStatus(ip, port);
    }
}