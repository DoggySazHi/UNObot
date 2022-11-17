using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UNObot.ServerQuery.Queries;

public class MCStatus : IQuery
{
    private const ushort DataSize = 512; // this will hopefully suffice since the MotD should be <=59 characters
    private const ushort NumFields = 6; // number of values expected from server

    private readonly IPEndPoint _ipEndPoint;

    public MCStatus(IPEndPoint ipEndPoint)
    {
        _ipEndPoint = ipEndPoint;
    }
        
    public string Motd { get; private set; }
    public string Version { get; private set; }
    public string CurrentPlayers { get; private set; }
    public string MaximumPlayers { get; private set; }
    public bool ServerUp { get; private set; }
    public long Delay { get; private set; }
    public async Task FetchData()
    {
        var rawServerData = new byte[DataSize];
            
        try
        {
            var stopWatch = new Stopwatch();
            using var tcpClient = new TcpClient
            {
                ReceiveTimeout = 5000,
                SendTimeout = 5000
            };
            stopWatch.Start();
            await tcpClient.ConnectAsync(_ipEndPoint);
            var stream = tcpClient.GetStream();
            var payload = new byte[] {0xFE, 0x01};
            stream.Write(payload, 0, payload.Length);
            _ = stream.Read(rawServerData, 0, DataSize);
            stopWatch.Stop();
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
}