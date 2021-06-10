using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Discord;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using UNObot.Core.UNOCore;
using UNObot.Plugins;
using UNObot.Plugins.TerminalCore;
using static UNObot.Plugins.Helpers.DatabaseExtensions;

namespace UNObot.Core.Services
{
    public class DatabaseService
    {
        private readonly ILogger _logger;

        public string ConnString { get; }

        public DatabaseService(IConfig config, ILogger logger)
        {
            _logger = logger;
            ConnString = config.GetConnectionString();
        }

        public async Task<bool> IsServerInGame(ulong server)
        {
            var inGame = false;

            const string commandText = "SELECT inGame FROM UNObot.Games WHERE server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                inGame = await db.ExecuteScalarAsync<bool>(commandText, new {Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return inGame;
        }

        public async Task AddGame(ulong server)
        {
            const string commandText = "INSERT IGNORE INTO Games (server) VALUES(@Server)";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task UpdateDescription(ulong server, string text)
        {
            const string commandText = "UPDATE Games SET description = @Description WHERE server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Server = server, Description = text});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task<string> GetDescription(ulong server)
        {
            const string commandText = "SELECT description FROM UNObot.Games WHERE server = @Server";
            var description = "";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                description = await db.ExecuteScalarAsync<string>(commandText, new {server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return description;
        }

        public async Task ResetGame(ulong server)
        {
            const string commandText =
                "UPDATE Games SET inGame = 0, currentCard = '[]', `order` = 1, oneCardLeft = 0, queue = '[]', description = null WHERE server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task AddUser(ulong id, string username, ulong server)
        {
            const string commandText =
                "INSERT INTO Players (userid, username, inGame, cards, server) VALUES(@UserID, @Username, 1, '[]', @Server) ON DUPLICATE KEY UPDATE username = @Username, inGame = 1, cards = '[]', server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {UserID = id, Username = username, Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task AddUser(ulong id, string username)
        {
            const string commandText =
                "INSERT INTO Players (userid, username) VALUES(@UserID, @Username) ON DUPLICATE KEY UPDATE username = @Username";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {UserID = id, Username = username});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task RemoveUser(ulong id)
        {
            const string commandText =
                "INSERT INTO Players (userid, inGame, cards, server) VALUES(@UserID, 0, '[]', null) ON DUPLICATE KEY UPDATE inGame = 0, cards = '[]', server = null";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {UserID = id});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task AddGuild(ulong guild, ushort inGame)
        {
            await AddGuild(guild, inGame, 1);
        }

        public async Task AddGuild(ulong guild, ushort inGame, ushort gameMode)
        {
            const string commandText =
                "INSERT INTO Games (server, inGame, gameMode) VALUES(@Server, @InGame, @GameMode) ON DUPLICATE KEY UPDATE inGame = @InGame, gameMode = @GameMode";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Server = guild, InGame = inGame, GameMode = gameMode});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task<GameMode> GetGameMode(ulong server)
        {
            var gamemode = GameMode.Normal;

            const string commandText = "SELECT gameMode FROM UNObot.Games WHERE server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                gamemode = await db.ExecuteScalarAsync<GameMode>(commandText, new {Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return gamemode;
        }

        //NOTE THAT THIS GETS DIRECTLY FROM SERVER; YOU MUST AddPlayersToServer
        public async Task<Queue<ulong>> GetPlayers(ulong server)
        {
            const string commandText = "SELECT queue FROM Games WHERE inGame = 1 AND server = @Server";

            var players = new Queue<ulong>();
            await using var db = new MySqlConnection(ConnString);
            try
            {
                var result = await db.ExecuteScalarAsync<string>(commandText, new {Server = server});
                players = JsonConvert.DeserializeObject<Queue<ulong>>(result);
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return players;
        }

        public async Task<ulong> GetUserServer(ulong player)
        {
            ulong server = 0;

            const string commandText = "SELECT server FROM Players WHERE inGame = 1 AND userid = @UserID";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                server = await db.ExecuteScalarAsync<ulong>(commandText, new {UserID = player});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return server;
        }

        public async Task SetPlayers(ulong server, Queue<ulong> players)
        {
            const string commandText = "UPDATE Games SET queue = @Queue WHERE inGame = 1 AND server = @Server";
            var json = JsonConvert.SerializeObject(players);

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Queue = json, Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task<Queue<ulong>> GetUsersWithServer(ulong server)
        {
            const string commandText = "SELECT userid FROM Players WHERE inGame = 1 AND server = @Server";
            var players = new Queue<ulong>();

            await using var db = new MySqlConnection(ConnString);
            try
            {
                players = new Queue<ulong>(await db.QueryAsync<ulong>(commandText, new {Server = server}));
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return players;
        }

        public async Task<ulong> GetUNOPlayer(ulong server)
        {
            ulong player = 0;

            const string commandText = "SELECT oneCardLeft FROM Games WHERE inGame = 1 AND server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                player = await db.ExecuteScalarAsync<ulong>(commandText, new {Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return player;
        }

        public async Task SetUNOPlayer(ulong server, ulong player)
        {
            const string commandText = "UPDATE Games SET oneCardLeft = @UserID WHERE inGame = 1 AND server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {UserID = player, Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task<Card> GetCurrentCard(ulong server)
        {
            const string commandText = "SELECT currentCard FROM Games WHERE inGame = 1 AND server = @Server";

            // If parsing fails, we can throw an NRE.
            Card card = null;
            await using var db = new MySqlConnection(ConnString);
            try
            {
                var result = await db.ExecuteScalarAsync<string>(commandText, new {Server = server});
                if (result.HasDBValue())
                    card = JsonConvert.DeserializeObject<Card>(result);
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return card;
        }

        public async Task SetCurrentCard(ulong server, Card card)
        {
            const string commandText = "UPDATE Games SET currentCard = @Card WHERE inGame = 1 AND server = @Server";
            var cardJson = JsonConvert.SerializeObject(card);

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Card = cardJson, Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task<bool> IsPlayerInGame(ulong player)
        {
            var result = false;

            const string commandText = "SELECT inGame FROM UNObot.Players WHERE userid = @UserID";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                result = await db.ExecuteScalarAsync<bool>(commandText, new {UserID = player});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return result;
        }

        public async Task<bool> IsPlayerInServerGame(ulong player, ulong server)
        {
            const string commandText = "SELECT queue FROM UNObot.Games WHERE server = @Server";

            var players = new Queue<ulong>();
            await using var db = new MySqlConnection(ConnString);
            try
            {
                var result = await db.ExecuteScalarAsync<string>(commandText, new {Server = server});
                if (result.HasDBValue())
                {
                    var temp = JsonConvert.DeserializeObject<Queue<ulong>>(result);
                    if (temp != null)
                        players = temp;
                }
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return players.Contains(player);
        }

        public async Task<List<Card>> GetCards(ulong player)
        {
            const string commandText = "SELECT cards FROM UNObot.Players WHERE userid = @UserID";
            var cards = new List<Card>();

            await using var db = new MySqlConnection(ConnString);
            try
            {
                var result = await db.ExecuteScalarAsync<string>(commandText, new {UserID = player});
                if (result.HasDBValue())
                    cards = JsonConvert.DeserializeObject<List<Card>>(result);
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return cards;
        }

        public async Task<bool> UserExists(ulong player)
        {
            const string commandText = "SELECT EXISTS(SELECT 1 FROM UNObot.Players WHERE userid = @UserID)";
            var exists = false;

            await using var db = new MySqlConnection(ConnString);
            try
            {
                exists = await db.ExecuteScalarAsync<bool>(commandText, new {UserID = player});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
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

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Queue = json, Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
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
            
            await using var db = new MySqlConnection(ConnString);
            try
            {
                var results = await db.QueryFirstOrDefaultAsync(commandText, new {UserID = player});
                if (!ReferenceEquals(null, results))
                    return (results.gamesJoined, results.gamesPlayed, results.gamesWon);
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return (0, 0, 0);
        }

        public async Task<string> GetNote(ulong player)
        {
            const string commandText = "SELECT note FROM UNObot.Players WHERE userid = @UserID";

            string message = null;
            await using var db = new MySqlConnection(ConnString);
            try
            {
                message = await db.ExecuteScalarAsync<string>(commandText, new {UserID = player});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return message;
        }

        public async Task SetNote(ulong player, string note)
        {
            const string commandText = "UPDATE UNObot.Players SET note = @Note WHERE userid = @UserID";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Note = note, UserID = player});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task RemoveNote(ulong player)
        {
            const string commandText = "UPDATE UNObot.Players SET note = NULL WHERE userid = @UserID";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {UserID = player});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
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

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {UserID = player});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
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

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Cards = json, UserID = player});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
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

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Cards = json, UserID = player});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return true;
        }

        public async Task SetServerCards(ulong server, List<Card> cards)
        {
            const string commandText = "UPDATE Games SET cards = @Cards WHERE server = @Server";

            var json = JsonConvert.SerializeObject(cards);

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Cards = json, Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task<List<Card>> GetServerCards(ulong server)
        {
            var cards = new List<Card>();
            const string commandText = "SELECT cards FROM UNObot.Games WHERE server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                var result = await db.ExecuteScalarAsync<string>(commandText, new {Server = server});
                if (result.HasDBValue())
                    cards = JsonConvert.DeserializeObject<List<Card>>(result);
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return cards;
        }

        public async Task<int> GetCardsDrawn(ulong server)
        {
            const string commandText = "SELECT cardsDrawn FROM UNObot.Games WHERE server = @Server";
            var cardsDrawn = 0;

            await using var db = new MySqlConnection(ConnString);
            try
            {
                cardsDrawn = await db.ExecuteScalarAsync<int>(commandText, new {Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return cardsDrawn;
        }

        public async Task SetCardsDrawn(ulong server, int count)
        {
            const string commandText = "UPDATE Games SET cardsDrawn = @CardsDrawn WHERE server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {CardsDrawn = count, Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        /*
        public class Server
        {
            public bool InGame;

            public Server(ulong ID)
            {
                this.ID = ID;
                const string CommandText = "SELECT * FROM UNObot.Games WHERE server = ?";
                
                var p1 = new MySqlParameter
                {
                    Value = ID
                };
                Parameters.Add(p1);

                using var dr = db.ExecuteReader(CommandText, new {  });
                await using var db = new MySqlConnection(ConnString); try
                {
                    if (dr.Read())
                    {
                        InGame = dr.GetBoolean("inGame");
                        Gamemode = (UNOCoreServices.GameMode) dr.GetUInt16("gameMode");
                        CurrentCardJSON = dr.GetString("currentCard");
                        if (!dr.IsDBNull(dr.GetOrdinal("oneCardLeft")))
                            UNOUser = dr.GetUInt64("oneCardLeft");
                        CardsDrawn = dr.GetInt32("cardsDrawn");
                        QueueJSON = dr.GetString("queue");
                        if (!dr.IsDBNull(dr.GetOrdinal("description")))
                            Description = dr.GetString("description");
                        if (!dr.IsDBNull(dr.GetOrdinal("playChannel")))
                            PlayChannel = dr.GetUInt64("playChannel");
                        HasDefaultChannel = dr.GetBoolean("hasDefaultChannel");
                        EnforceChannel = dr.GetBoolean("enforceChannel");
                        AllowedChannelsJSON = dr.GetString("allowedChannels");
                        CommandPrefix = dr.GetChar("commandPrefix");
                        CardsJSON = dr.GetString("cards");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Could not find record for server ${ID}!");
                    }
                }
                catch (MySqlException ex)
                {
                    _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
                }
            }

            public ulong ID { get; }
            public UNOCoreServices.GameMode Gamemode { get; }
            private string CurrentCardJSON { get; }
            public Card CurrentCard { get; private set; }
            public ulong UNOUser { get; }
            public int CardsDrawn { get; }
            private string QueueJSON { get; }
            public Queue<ulong> Queue { get; private set; }
            public string Description { get; }
            public ulong PlayChannel { get; }
            public bool HasDefaultChannel { get; }
            public bool EnforceChannel { get; }
            private string AllowedChannelsJSON { get; }
            public List<ulong> AllowedChannels { get; private set; }
            public char CommandPrefix { get; }
            private string CardsJSON { get; }
            public List<Card> Cards { get; private set; }

            public void ParseJSON()
            {
                CurrentCard = JsonConvert.DeserializeObject<Card>(CurrentCardJSON);
                Queue = JsonConvert.DeserializeObject<Queue<ulong>>(QueueJSON);
                AllowedChannels = JsonConvert.DeserializeObject<List<ulong>>(AllowedChannelsJSON);
                Cards = JsonConvert.DeserializeObject<List<Card>>(CardsJSON);
            }
        }
        */
    }
}