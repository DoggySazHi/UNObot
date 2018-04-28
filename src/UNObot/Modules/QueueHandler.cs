using System.Collections.Generic;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace UNObot.Modules
{
    public class QueueHandler
    {
        UNOdb db = new UNOdb();
        async public Task NextPlayer(ulong server)
        {
            Queue<ulong> players = await db.GetPlayers(server);
            ulong sendToBack = players.Dequeue();
            players.Enqueue(sendToBack);
        }
        async public Task<ulong> CurrentPlayer(ulong server)
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
                //TODO write something here to end the game?
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
                    } else
                    {
                        await NextPlayer(server);
                    }
                }
            }
        }
    }
}