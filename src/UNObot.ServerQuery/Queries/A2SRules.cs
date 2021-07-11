using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UNObot.ServerQuery.Queries
{
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
                    var name = Utilities.ReadNullTerminatedString(ref br);
                    var value = Utilities.ReadNullTerminatedString(ref br);
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
            catch (Exception)
            {
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
}