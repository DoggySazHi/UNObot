using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UNObot.ServerQuery.Queries
{
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
                Name = Utilities.ReadNullTerminatedString(ref br);
                Map = Utilities.ReadNullTerminatedString(ref br);
                Folder = Utilities.ReadNullTerminatedString(ref br);
                Game = Utilities.ReadNullTerminatedString(ref br);
                Id = br.ReadInt16();
                Players = br.ReadByte();
                MaxPlayers = br.ReadByte();
                Bots = br.ReadByte();
                ServerType = (ServerTypeFlags) br.ReadByte();
                Environment = (EnvironmentFlags) br.ReadByte();
                Visibility = (VisibilityFlags) br.ReadByte();
                Vac = (VacFlags) br.ReadByte();
                Version = Utilities.ReadNullTerminatedString(ref br);
                ExtraDataFlag = (ExtraDataFlags) br.ReadByte();

                #region These EDF readers have to be in this order because that's the way they are reported

                if (ExtraDataFlag.HasFlag(ExtraDataFlags.Port))
                    Port = br.ReadInt16();
                if (ExtraDataFlag.HasFlag(ExtraDataFlags.SteamId))
                    SteamId = br.ReadUInt64();
                if (ExtraDataFlag.HasFlag(ExtraDataFlags.Spectator))
                {
                    SpectatorPort = br.ReadInt16();
                    Spectator = Utilities.ReadNullTerminatedString(ref br);
                }

                if (ExtraDataFlag.HasFlag(ExtraDataFlags.Keywords))
                    Keywords = Utilities.ReadNullTerminatedString(ref br);
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
            catch (Exception)
            {
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
}