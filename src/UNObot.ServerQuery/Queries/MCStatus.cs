using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace UNObot.ServerQuery.Queries
{
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
}