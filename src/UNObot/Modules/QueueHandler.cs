using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

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
            return players.Peek();
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
            if(!attemptPeek)
            {
                ColorConsole.WriteLine("Error: Couldn't read first player!", ConsoleColor.Red);
                await db.ResetGame(server);
                await PlayCard.ReplyAsync("In an attempt to save myself, I have ended the game. ERR_PLAYERLIST_TRYPEEKERR");
            }
            else if(player == result)
            {
                //call end game, like empty crap
            }
            else
            {
                foreach(ulong playerLoop in players)
                {
                    if(playerLoop == players.Peek())
                    {
                        players.Dequeue();
                        break;
                    }
                    ulong sendToBack = players.Dequeue();
                    players.Enqueue(sendToBack);
                }
            }
            await db.SetPlayers(server, players);
        }
    }
}