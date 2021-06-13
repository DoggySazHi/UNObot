using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Discord;
using Newtonsoft.Json;
using UNObot.Core.UNOCore;
using UNObot.Plugins;
using UNObot.Plugins.TerminalCore;
using static UNObot.Plugins.Helpers.DatabaseExtensions;

namespace UNObot.Core.Services
{
    public class DatabaseService
    {
        private readonly IUNObotConfig _config;
        private readonly ILogger _logger;

        public DatabaseService(IUNObotConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> IsServerInGame(ulong server)
        {
            var inGame = false;

            const string commandText = "SELECT inGame FROM UNObot.Games WHERE server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                inGame = await db.ExecuteScalarAsync<bool>(_config.ConvertSql(commandText), new {Server = Convert.ToDecimal(server)});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return inGame;
        }

        public async Task AddGame(ulong server)
        {
            const string commandText = "INSERT IGNORE INTO UNObot.Games (server) VALUES(@Server)";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new {Server = Convert.ToDecimal(server)});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task UpdateDescription(ulong server, string text)
        {
            const string commandText = "UPDATE UNObot.Games SET description = @Description WHERE server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new {Server = Convert.ToDecimal(server), Description = text});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task<string> GetDescription(ulong server)
        {
            const string commandText = "SELECT description FROM UNObot.Games WHERE server = @Server";
            var description = "";

            await using var db = _config.GetConnection();
            try
            {
                description = await db.ExecuteScalarAsync<string>(_config.ConvertSql(commandText), new { Server = Convert.ToDecimal(server) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return description;
        }

        public async Task ResetGame(ulong server)
        {
            const string commandText =
                "UPDATE UNObot.Games SET inGame = 0, currentCard = '[]', `order` = 1, oneCardLeft = 0, queue = '[]', description = null WHERE server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new {Server = Convert.ToDecimal(server)});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task AddUser(ulong id, string username, ulong server)
        {
            const string commandText =
                "INSERT INTO UNObot.Players (userid, username, inGame, cards, server) VALUES(@UserID, @Username, 1, '[]', @Server) ON DUPLICATE KEY UPDATE username = @Username, inGame = 1, cards = '[]', server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new {UserID = Convert.ToDecimal(id), Username = username, Server = Convert.ToDecimal(server)});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task AddUser(ulong id, string username)
        {
            const string commandText =
                "INSERT INTO UNObot.Players (userid, username) VALUES(@UserID, @Username) ON DUPLICATE KEY UPDATE username = @Username";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { UserID = Convert.ToDecimal(id), Username = username });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task RemoveUser(ulong id)
        {
            const string commandText =
                "INSERT INTO UNObot.Players (userid, inGame, cards, server) VALUES(@UserID, 0, '[]', null) ON DUPLICATE KEY UPDATE inGame = 0, cards = '[]', server = null";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { UserID = Convert.ToDecimal(id) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task AddGuild(ulong guild, bool inGame)
        {
            await AddGuild(guild, inGame, 1);
        }

        public async Task AddGuild(ulong guild, bool inGame, byte gameMode)
        {
            const string commandText =
                "INSERT INTO UNObot.Games (server, inGame, gameMode) VALUES(@Server, @InGame, @GameMode) ON DUPLICATE KEY UPDATE inGame = @InGame, gameMode = @GameMode";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { Server = Convert.ToDecimal(guild), InGame = inGame, GameMode = gameMode });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task<GameMode> GetGameMode(ulong server)
        {
            var gamemode = GameMode.Normal;

            const string commandText = "SELECT gameMode FROM UNObot.Games WHERE server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                gamemode = await db.ExecuteScalarAsync<GameMode>(_config.ConvertSql(commandText), new { Server = Convert.ToDecimal(server) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return gamemode;
        }

        //NOTE THAT THIS GETS DIRECTLY FROM SERVER; YOU MUST AddPlayersToServer
        public async Task<Queue<ulong>> GetPlayers(ulong server)
        {
            const string commandText = "SELECT queue FROM UNObot.Games WHERE server = @Server";

            var players = new Queue<ulong>();
            await using var db = _config.GetConnection();
            try
            {
                var result = await db.ExecuteScalarAsync<string>(_config.ConvertSql(commandText), new { Server = Convert.ToDecimal(server) });
                players = JsonConvert.DeserializeObject<Queue<ulong>>(result);
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return players;
        }

        public async Task<ulong> GetUserServer(ulong player)
        {
            ulong server = 0;

            const string commandText = "SELECT server FROM UNObot.Players WHERE inGame = 1 AND userid = @UserID";

            await using var db = _config.GetConnection();
            try
            {
                server = await db.ExecuteScalarAsync<ulong>(_config.ConvertSql(commandText), new { UserID = Convert.ToDecimal(player) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return server;
        }

        public async Task SetPlayers(ulong server, Queue<ulong> players)
        {
            const string commandText = "UPDATE UNObot.Games SET queue = @Queue WHERE inGame = 1 AND server = @Server";
            var json = JsonConvert.SerializeObject(players);

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new {Queue = json, Server = Convert.ToDecimal(server)});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task<Queue<ulong>> GetUsersWithServer(ulong server)
        {
            const string commandText = "SELECT userid FROM UNObot.Players WHERE inGame = 1 AND server = @Server";
            var players = new Queue<ulong>();

            await using var db = _config.GetConnection();
            try
            {
                players = new Queue<ulong>(await db.QueryAsync<ulong>(_config.ConvertSql(commandText), new { Server = Convert.ToDecimal(server) }));
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return players;
        }

        public async Task<ulong> GetUNOPlayer(ulong server)
        {
            ulong player = 0;

            const string commandText = "SELECT oneCardLeft FROM UNObot.Games WHERE inGame = 1 AND server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                player = await db.ExecuteScalarAsync<ulong>(_config.ConvertSql(commandText), new { Server = Convert.ToDecimal(server) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return player;
        }

        public async Task SetUNOPlayer(ulong server, ulong player)
        {
            const string commandText = "UPDATE UNObot.Games SET oneCardLeft = @UserID WHERE inGame = 1 AND server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new {UserID = Convert.ToDecimal(player), Server = Convert.ToDecimal(server)});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task<Card> GetCurrentCard(ulong server)
        {
            const string commandText = "SELECT currentCard FROM UNObot.Games WHERE inGame = 1 AND server = @Server";

            // If parsing fails, we can throw an NRE.
            Card card = null;
            await using var db = _config.GetConnection();
            try
            {
                var result = await db.ExecuteScalarAsync<string>(_config.ConvertSql(commandText), new { Server = Convert.ToDecimal(server) });
                if (result.HasDBValue())
                    card = JsonConvert.DeserializeObject<Card>(result);
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return card;
        }

        public async Task SetCurrentCard(ulong server, Card card)
        {
            const string commandText = "UPDATE UNObot.Games SET currentCard = @Card WHERE inGame = 1 AND server = @Server";
            var cardJson = JsonConvert.SerializeObject(card);

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { Card = cardJson, Server = Convert.ToDecimal(server) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task<bool> IsPlayerInGame(ulong player)
        {
            var result = false;

            const string commandText = "SELECT inGame FROM UNObot.Players WHERE userid = @UserID";

            await using var db = _config.GetConnection();
            try
            {
                result = await db.ExecuteScalarAsync<bool>(_config.ConvertSql(commandText), new { UserID = Convert.ToDecimal(player) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return result;
        }

        public async Task<bool> IsPlayerInServerGame(ulong player, ulong server)
        {
            const string commandText = "SELECT queue FROM UNObot.Games WHERE server = @Server";

            var players = new Queue<ulong>();
            await using var db = _config.GetConnection();
            try
            {
                var result = await db.ExecuteScalarAsync<string>(_config.ConvertSql(commandText), new { Server = Convert.ToDecimal(server) });
                if (result.HasDBValue())
                {
                    var temp = JsonConvert.DeserializeObject<Queue<ulong>>(result);
                    if (temp != null)
                        players = temp;
                }
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return players.Contains(player);
        }

        public async Task<List<Card>> GetCards(ulong player)
        {
            const string commandText = "SELECT cards FROM UNObot.Players WHERE userid = @UserID";
            var cards = new List<Card>();

            await using var db = _config.GetConnection();
            try
            {
                var result = await db.ExecuteScalarAsync<string>(_config.ConvertSql(commandText), new { UserID = Convert.ToDecimal(player) });
                if (result.HasDBValue())
                    cards = JsonConvert.DeserializeObject<List<Card>>(result);
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return cards;
        }

        public async Task<bool> UserExists(ulong player)
        {
            const string commandText = "SELECT COUNT(1) FROM UNObot.Players WHERE userid = @UserID";
            var exists = false;

            await using var db = _config.GetConnection();
            try
            {
                exists = await db.ExecuteScalarAsync<bool>(_config.ConvertSql(commandText), new { UserID = Convert.ToDecimal(player) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return exists;
        }

        public async Task GetUsersAndAdd(ulong server)
        {
            var players = await GetUsersWithServer(server);
            //slight randomization of order
            for (var i = 0; i < ThreadSafeRandom.ThisThreadsRandom.Next(0, players.Count - 1); i++)
                players.Enqueue(players.Dequeue());
            if (players.Count == 0)
                _logger.Log(LogSeverity.Warning, "Why is the list empty when I'm getting players?");
            var json = JsonConvert.SerializeObject(players);

            const string commandText = "UPDATE UNObot.Games SET queue = @Queue WHERE server = @Server AND inGame = 1";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new {Queue = json, Server = Convert.ToDecimal(server)});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task StarterCard(ulong server)
        {
            var players = await GetPlayers(server);
            foreach (var player in players)
                await AddCard(player, Card.RandomCard(7));
        }

        public async Task<(int GamesJoined, int GamesPlayed, int GamesWon)> GetStats(ulong player)
        {
            const string commandText =
                "SELECT gamesJoined, gamesPlayed, gamesWon FROM UNObot.Players WHERE userid = @UserID";
            
            await using var db = _config.GetConnection();
            try
            {
                var results = await db.QueryFirstOrDefaultAsync(_config.ConvertSql(commandText), new { UserID = Convert.ToDecimal(player) });
                if (!ReferenceEquals(null, results))
                    return (results.gamesJoined, results.gamesPlayed, results.gamesWon);
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return (0, 0, 0);
        }

        public async Task<string> GetNote(ulong player)
        {
            const string commandText = "SELECT note FROM UNObot.Players WHERE userid = @UserID";

            string message = null;
            await using var db = _config.GetConnection();
            try
            {
                message = await db.ExecuteScalarAsync<string>(_config.ConvertSql(commandText), new { UserID = Convert.ToDecimal(player) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return message;
        }

        public async Task SetNote(ulong player, string note)
        {
            const string commandText = "UPDATE UNObot.Players SET note = @Note WHERE userid = @UserID";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { Note = note, UserID = Convert.ToDecimal(player) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task RemoveNote(ulong player)
        {
            const string commandText = "UPDATE UNObot.Players SET note = NULL WHERE userid = @UserID";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { UserID = Convert.ToDecimal(player) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task UpdateStats(ulong player, int mode)
        {
            //1 is gamesJoined
            //2 is gamesPlayed
            //3 is gamesWon

            var commandText = mode switch
            {
                1 => "UPDATE UNObot.Players SET gamesJoined = gamesJoined + 1 WHERE userid = @UserID",
                2 => "UPDATE UNObot.Players SET gamesPlayed = gamesPlayed + 1 WHERE userid = @UserID",
                3 => "UPDATE UNObot.Players SET gamesWon = gamesWon + 1 WHERE userid = @UserID",
                _ => ""
            };

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { UserID = Convert.ToDecimal(player) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task AddCard(ulong player, params Card[] cardsAdd)
        {
            var cards = await GetCards(player) ?? new List<Card>();
            cards.AddRange(cardsAdd);
            await SetCards(player, cards);
        }

        private async Task SetCards(ulong player, List<Card> cards)
        {
            const string commandText = "UPDATE UNObot.Players SET cards = @Cards WHERE userid = @UserID";

            cards ??= new List<Card>();
            var json = JsonConvert.SerializeObject(cards);

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { Cards = json, UserID = Convert.ToDecimal(player) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task<bool> RemoveCard(ulong player, Card card)
        {
            var foundCard = false;
            var cards = await GetCards(player);
            var currentPlace = 0;
            foreach (var cardindeck in cards)
            {
                if (card.Equals(cardindeck))
                {
                    cards.RemoveAt(currentPlace);
                    foundCard = true;
                    break;
                }

                currentPlace++;
            }

            if (!foundCard)
                return false;
            var json = JsonConvert.SerializeObject(cards);

            const string commandText = "UPDATE UNObot.Players SET cards = @Cards WHERE userid = @UserID";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { Cards = json, UserID = Convert.ToDecimal(player) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return true;
        }

        public async Task SetServerCards(ulong server, List<Card> cards)
        {
            const string commandText = "UPDATE UNObot.Games SET cards = @Cards WHERE server = @Server";

            var json = JsonConvert.SerializeObject(cards);

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { Cards = json, Server = Convert.ToDecimal(server) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }

        public async Task<List<Card>> GetServerCards(ulong server)
        {
            var cards = new List<Card>();
            const string commandText = "SELECT cards FROM UNObot.Games WHERE server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                var result = await db.ExecuteScalarAsync<string>(_config.ConvertSql(commandText), new { Server = Convert.ToDecimal(server) });
                if (result.HasDBValue())
                    cards = JsonConvert.DeserializeObject<List<Card>>(result);
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return cards;
        }

        public async Task<int> GetCardsDrawn(ulong server)
        {
            const string commandText = "SELECT cardsDrawn FROM UNObot.Games WHERE server = @Server";
            var cardsDrawn = 0;

            await using var db = _config.GetConnection();
            try
            {
                cardsDrawn = await db.ExecuteScalarAsync<int>(_config.ConvertSql(commandText), new { Server = Convert.ToDecimal(server) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }

            return cardsDrawn;
        }

        public async Task SetCardsDrawn(ulong server, int count)
        {
            const string commandText = "UPDATE UNObot.Games SET cardsDrawn = @CardsDrawn WHERE server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { CardsDrawn = count, Server = Convert.ToDecimal(server) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "An SQL error has occurred.", ex);
            }
        }
    }
}