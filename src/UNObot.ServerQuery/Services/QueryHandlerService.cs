using System;
using System.Net;
using System.Net.Sockets;
using UNObot.ServerQuery.Queries;

namespace UNObot.ServerQuery.Services
{
    //Adopted from Valve description: https://developer.valvesoftware.com/wiki/Server_queries#A2S_INFO
    //Thanks to https://www.techpowerup.com/forums/threads/snippet-c-net-steam-a2s_info-query.229199/ for A2S_INFO, self-reimplemented for A2S_PLAYER and A2S_RULES
    //Dealing with network-level stuff is hard

    //Credit to https://github.com/maxime-paquatte/csharp-minecraft-query/blob/master/src/Status.cs
    //Mukyu... but I implemented the Minecraft RCON (Valve RCON) protocol by hand, as well as the query.
    public class QueryHandlerService
    {
        private readonly RCONManager _manager;

        public QueryHandlerService(RCONManager manager)
        {
            _manager = manager;
        }

        public bool GetInfo(string ip, ushort port, out A2SInfo output)
        {
            var success = TryParseServer(ip, port, out var iPEndPoint);
            if (!success)
            {
                output = null;
                return false;
            }

            output = new A2SInfo(iPEndPoint);
            return output.ServerUp;
        }

        public bool GetPlayers(string ip, ushort port, out A2SPlayer output)
        {
            var success = TryParseServer(ip, port, out var iPEndPoint);
            if (!success)
            {
                output = null;
                return false;
            }

            output = new A2SPlayer(iPEndPoint);
            return output.ServerUp;
        }

        public bool GetRules(string ip, ushort port, out A2SRules output)
        {
            var success = TryParseServer(ip, port, out var iPEndPoint);
            if (!success)
            {
                output = null;
                return false;
            }

            output = new A2SRules(iPEndPoint);
            return output.ServerUp;
        }

        public bool GetInfoMCNew(string ip, ushort port, out MinecraftStatus output)
        {
            var success = TryParseServer(ip, port, out var iPEndPoint);
            if (!success)
            {
                output = null;
                return false;
            }
            output = new MinecraftStatus(iPEndPoint);
            return output.ServerUp;
        }

        public bool SendRCON(string ip, ushort port, string command, string password, out IRCON output)
        {
            var success = TryParseServer(ip, port, out var iPEndPoint);
            if (!success)
            {
                output = null;
                return false;
            }

            output = _manager.CreateRCON(iPEndPoint, password, false, command);
            return output.Status == IRCON.RCONStatus.Success;
        }

        public bool CreateRCON(string ip, ushort port, string password, ulong user, out IRCON output)
        {
            var possibleRCON = _manager.GetRCON(user);
            if (possibleRCON != null)
            {
                output = possibleRCON;
                return false;
            }

            var success = TryParseServer(ip, port, out var iPEndPoint);
            if (!success)
            {
                output = null;
                return false;
            }

            output = _manager.CreateRCON(iPEndPoint, password, true);
            output.Owner = user;
            return output.Status == IRCON.RCONStatus.Success;
        }

        public bool ExecuteRCON(ulong user, string command, out IRCON output)
        {
            var possibleRCON = _manager.GetRCON(user);
            output = possibleRCON;
            if (possibleRCON == null)
                return false;

            possibleRCON.Execute(command, true);
            return output.Status == IRCON.RCONStatus.Success;
        }

        public bool CloseRCON(ulong user)
        {
            var possibleRCON = _manager.GetRCON(user);
            if (possibleRCON == null)
                return false;

            possibleRCON.Dispose();
            return true;
        }

        private static bool TryParseServer(string ip, ushort port, out IPEndPoint iPEndPoint)
        {
            var parseCheck = IPAddress.TryParse(ip, out var server);
            var addresses = ResolveDns(ip);
            if (!parseCheck)
            {
                if (addresses == null || addresses.Length == 0)
                {
                    iPEndPoint = null;
                    return false;
                }

                server = addresses[0];
            }

            iPEndPoint = new IPEndPoint(server, port);
            return true;
        }

        private static IPAddress[] ResolveDns(string ip)
        {
            IPAddress[] addresses = null;
            try
            {
                addresses = Dns.GetHostAddresses(ip);
            }
            catch (SocketException)
            {
            }
            catch (ArgumentException)
            {
            }

            return addresses;
        }

        public MCStatus GetInfoMC(string ip, ushort port = 25565)
        {
            return new MCStatus(ip, port);
        }
    }
}