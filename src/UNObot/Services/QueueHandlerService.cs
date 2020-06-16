using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace UNObot.Services
{
    public static class QueueHandlerService
    {
        public static async Task NextPlayer(ulong server)
        {
            var players = await UNODatabaseService.GetPlayers(server);
            var sendToBack = players.Dequeue();
            players.Enqueue(sendToBack);
            await UNODatabaseService.SetPlayers(server, players);
            await UNODatabaseService.SetCardsDrawn(server, 0);
        }

        public static async Task ReversePlayers(ulong server)
        {
            var players = await UNODatabaseService.GetPlayers(server);
            await UNODatabaseService.SetPlayers(server, new Queue<ulong>(players.Reverse()));
        }

        public static async Task<ulong> GetCurrentPlayer(ulong server)
        {
            var players = await UNODatabaseService.GetPlayers(server);
            if (players.TryPeek(out var player))
                return player;
            LoggerService.Log(LogSeverity.Error, "[ERR] No players!");
            return player;
        }

        public static async Task<int> PlayerCount(ulong server)
        {
            var players = await UNODatabaseService.GetPlayers(server);
            return players.Count;
        }

        public static async Task<ulong[]> PlayerArray(ulong server)
        {
            var players = await UNODatabaseService.GetPlayers(server);
            return players.ToArray();
        }

        public static async Task RemovePlayer(ulong player, ulong server)
        {
            var players = await UNODatabaseService.GetPlayers(server);
            var attemptPeek = players.TryPeek(out var result);
            if (!attemptPeek)
            {
                LoggerService.Log(LogSeverity.Error, "Error: Couldn't read first player!");
                await UNODatabaseService.ResetGame(server);
            }
            else if (player == result)
            {
                players.Dequeue();
            }
            else
            {
                var oldplayer = players.Peek();
                for (var i = 0; i < players.Count; i++)
                {
                    if (player == players.Peek())
                    {
                        LoggerService.Log(LogSeverity.Debug, "RemovedPlayer");
                        players.Dequeue();
                        break;
                    }

                    var sendToBack = players.Dequeue();
                    players.Enqueue(sendToBack);
                }

                while (true)
                {
                    var sendToBack = players.Dequeue();
                    players.Enqueue(sendToBack);
                    if (oldplayer == players.Peek())
                        break;
                }
            }

            await UNODatabaseService.SetPlayers(server, players);
        }

        public static async Task DropFrontPlayer(ulong server)
        {
            var players = await UNODatabaseService.GetPlayers(server);
            players.Dequeue();
            await UNODatabaseService.SetPlayers(server, players);
        }
    }
}