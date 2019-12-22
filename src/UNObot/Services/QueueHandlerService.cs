using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
using UNObot.TerminalCore;

#pragma warning disable CS1701 // Assuming assembly reference matches identity
#pragma warning disable CS1702 // Assuming assembly reference matches identity

namespace UNObot.Modules
{
    public static class QueueHandlerService
    {
        public static async Task NextPlayer(ulong server)
        {
            Queue<ulong> players = await UNODatabaseService.GetPlayers(server);
            ulong sendToBack = players.Dequeue();
            players.Enqueue(sendToBack);
            await UNODatabaseService.SetPlayers(server, players);
        }
        public static async Task ReversePlayers(ulong server)
        {
            Queue<ulong> players = await UNODatabaseService.GetPlayers(server);
            await UNODatabaseService.SetPlayers(server, new Queue<ulong>(players.Reverse()));
        }
        public static async Task<ulong> GetCurrentPlayer(ulong server)
        {
            Queue<ulong> players = await UNODatabaseService.GetPlayers(server);
            if (players.TryPeek(out ulong player))
                return player;
            ColorConsole.WriteLine("[ERR] No players!", ConsoleColor.Red);
            return player;
        }
        public static async Task<int> PlayerCount(ulong server)
        {
            Queue<ulong> players = await UNODatabaseService.GetPlayers(server);
            return players.Count;
        }
        public static async Task<ulong[]> PlayerArray(ulong server)
        {
            Queue<ulong> players = await UNODatabaseService.GetPlayers(server);
            return players.ToArray();
        }
        public static async Task RemovePlayer(ulong player, ulong server)
        {
            Queue<ulong> players = await UNODatabaseService.GetPlayers(server);
            bool attemptPeek = players.TryPeek(out ulong result);
            if (!attemptPeek)
            {
                ColorConsole.WriteLine("Error: Couldn't read first player!", ConsoleColor.Red);
                await UNODatabaseService.ResetGame(server);
            }
            else if (player == result)
            {
                players.Dequeue();
            }
            else
            {
                ulong oldplayer = players.Peek();
                for (int i = 0; i < players.Count; i++)
                {
                    if (player == players.Peek())
                    {
                        Console.WriteLine("RemovedPlayer");
                        players.Dequeue();
                        break;
                    }
                    ulong sendToBack = players.Dequeue();
                    players.Enqueue(sendToBack);
                }
                while (true)
                {
                    ulong sendToBack = players.Dequeue();
                    players.Enqueue(sendToBack);
                    if (oldplayer == players.Peek())
                        break;
                }
            }
            await UNODatabaseService.SetPlayers(server, players);
        }
        public static async Task DropFrontPlayer(ulong server)
        {
            Queue<ulong> players = await UNODatabaseService.GetPlayers(server);
            players.Dequeue();
            await UNODatabaseService.SetPlayers(server, players);
        }
    }
}