using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UNObot.ServerQuery.Queries
{
    public class A2SInfo
    {
        // \xFF\xFF\xFF\xFFTSource Engine Query\x00 because UTF-8 doesn't like to encode 0xFF
        private static readonly byte[] Request =
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65,
            0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00
        };

        private IPEndPoint _endPoint;

        public A2SInfo(IPEndPoint ep)
        {
            _endPoint = ep;
        }

        public async Task FetchData()
        {
            try
            {
                var udp = new UdpClient();
                udp.Client.SendTimeout = 5000;
                udp.Client.ReceiveTimeout = 5000;
                await udp.SendAsync(Request, Request.Length, _endPoint);
                var ms = new MemoryStream(udp.Receive(ref _endPoint)); // Saves the received data in a memory buffer
                var br = new BinaryReader(ms, Encoding.UTF8); // A binary reader that treats characters as Unicode 8-bit
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

        public byte Header { get; private set; } // I
        public byte Protocol { get; private set; }
        public string Name { get; private set; }
        public string Map { get; private set; }
        public string Folder { get; private set; }
        public string Game { get; private set; }
        public short Id { get; private set; }
        public byte Players { get; private set; }
        public byte MaxPlayers { get; private set; }
        public byte Bots { get; private set; }
        public ServerTypeFlags ServerType { get; private set; }
        public EnvironmentFlags Environment { get; private set; }
        public VisibilityFlags Visibility { get; private set; }
        public VacFlags Vac { get; private set; }
        public string Version { get; private set; }
        public ExtraDataFlags ExtraDataFlag { get; private set; }

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

        public ulong GameId { get; private set; } //0x01
        public ulong SteamId { get; private set; } //0x10
        public string Keywords { get; private set; } //0x20
        public string Spectator { get; private set; } //0x40
        public short SpectatorPort { get; private set; } //0x40
        public short Port { get; private set; } //0x80
        public bool ServerUp { get; private set; }

        #endregion
    }
}