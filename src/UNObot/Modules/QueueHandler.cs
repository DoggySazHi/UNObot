using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

#pragma warning disable CS1701 // Assuming assembly reference matches identity
#pragma warning disable CS1702 // Assuming assembly reference matches identity

namespace UNObot.Modules
{
    public class QueueHandler
    {
        readonly UNOdb db = new UNOdb();

        async public Task NextPlayer(ulong server)
        {
            Queue<ulong> players = await db.GetPlayers(server);
            ulong sendToBack = players.Dequeue();
            players.Enqueue(sendToBack);
            await db.SetPlayers(server, players);
        }
        async public Task ReversePlayers(ulong server)
        {
            Queue<ulong> players = await db.GetPlayers(server);
            await db.SetPlayers(server, new Queue<ulong>(players.Reverse()));
        }
        async public Task<ulong> GetCurrentPlayer(ulong server)
        {
            Queue<ulong> players = await db.GetPlayers(server);
            ulong player = 0;
            if (players.TryPeek(out player))
                return player;
            ColorConsole.WriteLine("[ERR] No players!", ConsoleColor.Red);
            return player;
        }
        async public Task<int> PlayerCount(ulong server)
        {
            Queue<ulong> players = await db.GetPlayers(server);
            return players.Count;
        }
        async public Task<ulong[]> PlayerArray(ulong server)
        {
            Queue<ulong> players = await db.GetPlayers(server);
            return players.ToArray();
        }
        async public Task RemovePlayer(ulong player, ulong server)
        {
            Queue<ulong> players = await db.GetPlayers(server);
            bool attemptPeek = players.TryPeek(out ulong result);
            if (!attemptPeek)
            {
                ColorConsole.WriteLine("Error: Couldn't read first player!", ConsoleColor.Red);
                await db.ResetGame(server);
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
            await db.SetPlayers(server, players);
        }
        public async Task DropFrontPlayer(ulong server)
        {
            Queue<ulong> players = await db.GetPlayers(server);
            players.Dequeue();
            await db.SetPlayers(server, players);
        }
    }
}