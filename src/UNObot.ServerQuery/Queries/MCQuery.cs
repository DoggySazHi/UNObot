using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UNObot.ServerQuery.Queries;

/// <summary>
///     William's proud of himself for writing this class.
/// </summary>
public class MCQuery : IQuery
{
    private readonly byte[] _sessionHandshake = {0xFE, 0xFD, 0x09, 0x00, 0x00, 0x00, 0x01};

    private IPEndPoint _ipEndPoint;

    public MCQuery(IPEndPoint ipEndPoint)
    {
        _ipEndPoint = ipEndPoint;
    }

    public bool ServerUp { get; private set; }
    public int MaxPlayers { get; private set; }
    public string Ip { get; private set; }
    public ushort Port { get; private set; }
    public string GameId { get; private set; }
    public string GameType { get; private set; }
    public string Plugins { get; private set; }
    public string Version { get; private set; }
    public string Map { get; private set; }
    public string Motd { get; private set; }
    public string[] Players { get; private set; }
        
    public async Task FetchData()
    {
        UdpClient udp = null;
        MemoryStream memoryStream = null;
        BinaryReader binaryReader = null;
        try
        {
            udp = new UdpClient
            {
                Client =
                {
                    SendTimeout = 5000,
                    ReceiveTimeout = 5000
                }
            };

            await udp.SendAsync(_sessionHandshake, _sessionHandshake.Length, _ipEndPoint);
            memoryStream = new MemoryStream(udp.Receive(ref _ipEndPoint));
            binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);
            memoryStream.Seek(5, SeekOrigin.Begin);
            var challengeString = Utilities.ReadNullTerminatedString(ref binaryReader);
            var challengeNumber = int.Parse(challengeString);
            var bytes = BitConverter.GetBytes(challengeNumber);

            // Save challenge token.
            byte[] response =
            {
                0xFE, 0xFD, 0x00, 0x00, 0x00, 0x00, 0x01, bytes[3], bytes[2], bytes[1], bytes[0], 0x00, 0x00, 0x00,
                0x00
            };

            binaryReader.Close();
            memoryStream.Close();
            await udp.SendAsync(response, response.Length, _ipEndPoint);
            memoryStream = new MemoryStream(udp.Receive(ref _ipEndPoint));
            binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);
            memoryStream.Seek(1 + 4 + 11, SeekOrigin.Begin);

            string input;
            do
            {
                input = Utilities.ReadNullTerminatedString(ref binaryReader);
                var value = Utilities.ReadNullTerminatedString(ref binaryReader).Trim();
                switch (input.Trim().ToLower())
                {
                    case "numplayers":
                        Players = new string[int.Parse(value)];
                        break;
                    case "maxplayers":
                        MaxPlayers = int.Parse(value);
                        break;
                    case "game_id":
                        GameId = value;
                        break;
                    case "gametype":
                        GameType = value;
                        break;
                    case "hostip":
                        Ip = value;
                        break;
                    case "hostport":
                        Port = ushort.Parse(value);
                        break;
                    case "hostname":
                        Motd = value;
                        break;
                    case "plugins":
                        Plugins = value;
                        break;
                    case "map":
                        Map = value;
                        break;
                    case "version":
                        Version = value;
                        break;
                }
            } while (input.Length != 0);

            var currentIndex = 0;
            do
            {
                input = Utilities.ReadNullTerminatedString(ref binaryReader);
                if (input.Length >= 2 && Players != null && currentIndex < Players.Length)
                    Players[currentIndex++] = input;
            } while (true);
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
        finally
        {
            binaryReader?.Close();
            memoryStream?.Close();
            udp?.Close();
        }
    }
}