using System.Collections.Generic;
using System;
using System.Collections;
using System.Threading.Tasks;

public class QueueHandler
{
    UNObot.Modules.UNOdb db = new UNObot.Modules.UNOdb();
    DiscordBot.Modules.ColorConsole cc = new DiscordBot.Modules.ColorConsole();
    async Task NextPlayer(ulong server)
    {
        Queue<ulong> players = await db.GetPlayers(server);
        ulong sendToBack = players.Dequeue();
        players.Enqueue(sendToBack);
    }
    async Task<ulong> CurrentPlayer(ulong server)
    {
        Queue<ulong> players = await db.GetPlayers(server);
        return players.Peek();
    }
    async Task<int> PlayerCount(ulong server)
    {
        Queue<ulong> players = await db.GetPlayers(server);
        return players.Count;
    }
    async Task<ulong[]> PlayerArray(ulong server)
    {
        Queue<ulong> players = await db.GetPlayers(server);
        return players.ToArray();
    }
    async Task RemovePlayer(ulong player, ulong server)
    {
        Queue<ulong> players = await db.GetPlayers(server);
        bool attemptPeek = players.TryPeek(out ulong result);
        if(!attemptPeek)
        {
            DiscordBot.Modules.ColorConsole.WriteLine("Error: Couldn't read first player!", ConsoleColor.Red);
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