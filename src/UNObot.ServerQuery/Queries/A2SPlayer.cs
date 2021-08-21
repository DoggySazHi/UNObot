using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UNObot.ServerQuery.Queries
{
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
}