using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UNObot.Plugins.TerminalCore;

namespace UNObot.ServerQuery.Queries;

/// <summary>
///     William's proud of himself for writing this class.
/// </summary>
public class MCPEStatus : IQuery
{
    private readonly byte[] _magicBytes = { 0x00, 0xFF, 0xFF, 0x00, 0xFE, 0xFE, 0xFE, 0xFE, 0xFD, 0xFD, 0xFD, 0xFD, 0x12, 0x34, 0x56, 0x78 };

    private IPEndPoint _endPoint;
        
    public long Time { get; private set; }
    public long ServerGUID { get; private set; }
    public string Edition { get; private set; }
    public string Motd { get; private set; }
    public string Motd2 { get; private set; }
    public string Version { get; private set; }
    public int ProtocolVersion { get; private set; }
    public int Players { get; private set; }
    public int MaxPlayers { get; private set; }
    public string ServerID { get; private set; }
    public int GameModeID { get; private set; }
    public string GameMode { get; private set; }
    public int PortIPv4 { get; private set; }
    public int PortIPv6 { get; private set; }
    public long Ping { get; private set; }
        
    public MCPEStatus(IPEndPoint endPoint)
    {
        _endPoint = endPoint;
    }

    public async Task FetchData()
    {
        var stopwatch = new Stopwatch();
            
        using var udp = new UdpClient();
        udp.Client.SendTimeout = 5000;
        udp.Client.ReceiveTimeout = 5000;

        var guid = RandomGUID();
            
        var data = new byte[] { 0x01 };
        data = Append(data, Utilities.LittleEndianConverter(DateTimeOffset.Now.ToUnixTimeMilliseconds()));
        data = Append(data, _magicBytes);
        data = Append(data, guid);

        stopwatch.Restart();
           
        await udp.SendAsync(data, data.Length, _endPoint);
        await using var stream = new MemoryStream(udp.Receive(ref _endPoint));
        using var reader = new BinaryReader(stream, Encoding.UTF8);
            
        stopwatch.Stop();
        Ping = stopwatch.ElapsedMilliseconds;

        if (reader.ReadByte() != 0x1C)
        {
            throw new InvalidDataException("Unexpected packet type received from the server!");
        }

        // First byte already checked for packet.
        Time = reader.ReadInt64();
        ServerGUID = reader.ReadInt64();
        // Skip the next 16 bytes for the magic bytes.
        reader.ReadInt64();
        reader.ReadInt64();
        // Skip one bytes for length. I have no idea why.
        reader.ReadByte();
        // The actual data string.
        var text = reader.ReadString().Split(';');
        Edition = text[0];
        Motd = text[1];
        ProtocolVersion = int.Parse(text[2]);
        Version = text[3];
        Players = int.Parse(text[4]);
        MaxPlayers = int.Parse(text[5]);
        ServerID = text[6];
        Motd2 = text[7]; // Seems to just show the world; why is it an Motd second line?
        GameMode = text[8];
        GameModeID = int.Parse(text[9]);
        PortIPv4 = int.Parse(text[10]);
        PortIPv6 = int.Parse(text[11]);
    }

    private static byte[] Append(IReadOnlyList<byte> a, IReadOnlyList<byte> b)
    {
        var temp = new byte[a.Count + b.Count];
            
        for (var i = 0; i < a.Count; ++i)
        {
            temp[i] = a[i];
        }

        for (var i = 0; i < b.Count; ++i)
        {
            temp[i + a.Count] = b[i];
        }

        return temp;
    }

    private static byte[] RandomGUID()
    {
        // Not truly random, but does it really matter?
        var bytes = new byte[4];
        ThreadSafeRandom.ThisThreadsRandom.NextBytes(bytes);
        return bytes;
    }
}