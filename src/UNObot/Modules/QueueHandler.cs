using System.Collections.Generic;
using System;
using System.Collections;
using System.Threading.Tasks;

public class QueueHandler
{
    UNObot.Modules.UNOdb db = new UNObot.Modules.UNOdb();
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
}