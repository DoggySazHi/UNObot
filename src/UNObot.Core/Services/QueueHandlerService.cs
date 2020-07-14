using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace UNObot.Core.Services
{
    internal class QueueHandlerService
    {
        private readonly LoggerService _logger;
        private readonly UNODatabaseService _db;

        public QueueHandlerService(LoggerService logger, UNODatabaseService db)
        {
            _logger = logger;
            _db = db;
        }
        
        internal async Task NextPlayer(ulong server)
        {
            var players = await _db.GetPlayers(server);
            var sendToBack = players.Dequeue();
            players.Enqueue(sendToBack);
            await _db.SetPlayers(server, players);
            await _db.SetCardsDrawn(server, 0);
        }

        internal async Task ReversePlayers(ulong server)
        {
            var players = await _db.GetPlayers(server);
            await _db.SetPlayers(server, new Queue<ulong>(players.Reverse()));
        }

        internal async Task<ulong> GetCurrentPlayer(ulong server)
        {
            var players = await _db.GetPlayers(server);
            if (players.TryPeek(out var player))
                return player;
            _logger.Log(LogSeverity.Error, "[ERR] No players!");
            return player;
        }

        internal async Task<int> PlayerCount(ulong server)
        {
            var players = await _db.GetPlayers(server);
            return players.Count;
        }

        internal async Task<ulong[]> PlayerArray(ulong server)
        {
            var players = await _db.GetPlayers(server);
            return players.ToArray();
        }

        internal async Task RemovePlayer(ulong player, ulong server)
        {
            var players = await _db.GetPlayers(server);
            var attemptPeek = players.TryPeek(out var result);
            if (!attemptPeek)
            {
                _logger.Log(LogSeverity.Error, "Error: Couldn't read first player!");
                await _db.ResetGame(server);
            }
            else if (player == result)
            {
                players.Dequeue();
            }
            else
            {
                var oldPlayer = players.Peek();
                for (var i = 0; i < players.Count; i++)
                {
                    if (player == players.Peek())
                    {
                        _logger.Log(LogSeverity.Debug, "RemovedPlayer");
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
                    if (oldPlayer == players.Peek())
                        break;
                }
            }

            await _db.SetPlayers(server, players);
        }

        internal async Task DropFrontPlayer(ulong server)
        {
            var players = await _db.GetPlayers(server);
            players.Dequeue();
            await _db.SetPlayers(server, players);
        }
    }
}