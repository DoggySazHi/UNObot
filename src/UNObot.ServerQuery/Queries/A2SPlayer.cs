using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UNObot.ServerQuery.Queries
{
    public class A2SPlayer
    {
        private static readonly byte[] Handshake = {0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF};

        private IPEndPoint _endPoint;
        
        public A2SPlayer(IPEndPoint ep)
        {
            _endPoint = ep;
        }

        public async Task FetchData()
        {
            try
            {
                Players = new List<Player>();
                var udp = new UdpClient();
                udp.Client.SendTimeout = 5000;
                udp.Client.ReceiveTimeout = 5000;
                await udp.SendAsync(Handshake, Handshake.Length, _endPoint);
                var ms = new MemoryStream(udp.Receive(ref _endPoint));
                var br = new BinaryReader(ms, Encoding.UTF8);
                ms.Seek(5, SeekOrigin.Begin);

                //Get challenge number, and plan to resend it.
                byte[] response =
                    {0xFF, 0xFF, 0xFF, 0xFF, 0x55, br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte()};

                br.Close();
                ms.Close();
                await udp.SendAsync(response, response.Length, _endPoint);
                ms = new MemoryStream(udp.Receive(ref _endPoint));
                br = new BinaryReader(ms, Encoding.UTF8);

                ms.Seek(4, SeekOrigin.Begin);
                Header = br.ReadByte();
                PlayerCount = br.ReadByte();
                for (var i = 0; i < Convert.ToInt32(PlayerCount); i++)
                {
                    var index = br.ReadByte();
                    var name = Utilities.ReadNullTerminatedString(ref br);
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
            catch (Exception)
            {
                ServerUp = false;
            }
        }

        public byte Header { get; private set; }
        public byte PlayerCount { get; private set; }

        public bool ServerUp { get; private set; }
        public List<Player> Players { get; private set; }

        public readonly struct Player
        {
            public byte Index { get; init; }
            public string Name { get; init; }
            public long Score { get; init; }
            public float Duration { get; init; }
        }
    }
}