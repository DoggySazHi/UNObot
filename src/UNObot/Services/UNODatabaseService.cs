using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using UNObot.Plugins.TerminalCore;
using UNObot.UNOCore;

namespace UNObot.Services
{
    internal static class DatabaseExtensions
    {
        internal static bool HasDBValue(this object item) => !DBNull.Value.Equals(item) && item != null;
    }
    
    internal class UNODatabaseService
    {
        private readonly IConfiguration _config;
        private readonly LoggerService _logger;

        private string _connString = "";

        private void GetConnectionString()
        {
            _connString = _config["connStr"];
            //ha, damn the limited encodings.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding.GetEncoding("windows-1254");
        }

        public UNODatabaseService(IConfiguration config, LoggerService logger)
        {
            _config = config;
            _logger = logger;
            GetConnectionString();
            var parameters = new List<MySqlParameter>();

            const string commandText =
                "SET SQL_SAFE_UPDATES = 0; UPDATE UNObot.Players SET cards = ?, inGame = 0, server = null, gameName = null; UPDATE Games SET inGame = 0, currentCard = ?, `order` = 1, oneCardLeft = 0, queue = ?, description = null; SET SQL_SAFE_UPDATES = 1;";
            var p1 = new MySqlParameter
            {
                Value = "[]"
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = "[]"
            };
            parameters.Add(p2);
            var p3 = new MySqlParameter
            {
                Value = "[]"
            };
            parameters.Add(p3);
            try
            {
                MySqlHelper.ExecuteNonQuery(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<bool> IsServerInGame(ulong server)
        {
            var inGame = false;
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT inGame FROM UNObot.Games WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                inGame = result.HasDBValue() && (byte) result == 1;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return inGame;
        }

        internal async Task AddGame(ulong server)
        {
            const string commandText = "INSERT IGNORE INTO Games (server) VALUES(?)";
            var parameters = new List<MySqlParameter>();
            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);

            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task UpdateDescription(ulong server, string text)
        {
            const string commandText = "UPDATE Games SET description = ? WHERE server = ?";
            var parameters = new List<MySqlParameter>();
            var p1 = new MySqlParameter
            {
                Value = text
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p2);

            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<string> GetDescription(ulong server)
        {
            const string commandText = "SELECT description FROM UNObot.Games WHERE server = ?";
            var description = "";
            var parameters = new List<MySqlParameter>();
            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if(result.HasDBValue()) 
                    description = (string) result;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return description;
        }

        internal async Task ResetGame(ulong server)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText =
                "UPDATE Games SET inGame = 0, currentCard = ?, `order` = 1, oneCardLeft = 0, queue = ?, description = null WHERE server = ?";
            var p1 = new MySqlParameter
            {
                Value = "[]"
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = "[]"
            };
            parameters.Add(p2);
            var p3 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p3);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task AddUser(ulong id, string username, ulong server)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText =
                "INSERT INTO Players (userid, username, inGame, cards, server) VALUES(?, ?, 1, ?, ?) ON DUPLICATE KEY UPDATE username = ?, inGame = 1, cards = ?, server = ?";
            var p1 = new MySqlParameter
            {
                Value = id
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = username
            };
            parameters.Add(p2);
            var p3 = new MySqlParameter
            {
                Value = "[]"
            };
            parameters.Add(p3);
            var p4 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p4);
            var p5 = new MySqlParameter
            {
                Value = username
            };
            parameters.Add(p5);
            var p6 = new MySqlParameter
            {
                Value = "[]"
            };
            parameters.Add(p6);
            var p7 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p7);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task AddUser(ulong id, string username)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText =
                "INSERT INTO Players (userid, username) VALUES(?, ?) ON DUPLICATE KEY UPDATE username = ?";
            var p1 = new MySqlParameter
            {
                Value = id
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = username
            };
            parameters.Add(p2);
            var p3 = new MySqlParameter
            {
                Value = username
            };
            parameters.Add(p3);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task RemoveUser(ulong id)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText =
                "INSERT INTO Players (userid, inGame, cards, server) VALUES(@id, 0, ?, null) ON DUPLICATE KEY UPDATE inGame = 0, cards = ?, server = null";
            var p1 = new MySqlParameter
            {
                Value = id,
                ParameterName = "id"
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = "[]"
            };
            parameters.Add(p2);
            var p3 = new MySqlParameter
            {
                Value = "[]"
            };
            parameters.Add(p3);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task AddGuild(ulong guild, ushort inGame)
        {
            await AddGuild(guild, inGame, 1);
        }

        internal async Task AddGuild(ulong guild, ushort inGame, ushort gameMode)
        {
            /* 
             * 1 - In a regular game.
             * 2 - In a game that prevents seeing other players' cards.
             * 3 (maybe?) - Allows skipping of a turn after drawing 2 cards.
            */
            var parameters = new List<MySqlParameter>();
            const string commandText =
                "INSERT INTO Games (server, inGame, gameMode) VALUES(?, ?, ?) ON DUPLICATE KEY UPDATE inGame = ?, gameMode = ?";

            var p1 = new MySqlParameter
            {
                Value = guild
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = inGame
            };
            parameters.Add(p2);
            var p3 = new MySqlParameter
            {
                Value = gameMode
            };
            parameters.Add(p3);
            var p4 = new MySqlParameter
            {
                Value = inGame
            };
            parameters.Add(p4);
            var p5 = new MySqlParameter
            {
                Value = gameMode
            };
            parameters.Add(p5);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<GameMode> GetGameMode(ulong server)
        {
            var gamemode = GameMode.Normal;
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT gameMode FROM UNObot.Games WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    gamemode = (GameMode) result;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return gamemode;
        }

        //NOTE THAT THIS GETS DIRECTLY FROM SERVER; YOU MUST AddPlayersToServer
        internal async Task<Queue<ulong>> GetPlayers(ulong server)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT queue FROM Games WHERE inGame = 1 AND server = ?";

            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            var players = new Queue<ulong>();
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if(result.HasDBValue())
                    players = JsonConvert.DeserializeObject<Queue<ulong>>((string) result);
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return players;
        }

        internal async Task<ulong> GetUserServer(ulong player)
        {
            ulong server = 0;
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT server FROM Players WHERE inGame = 1";

            var p1 = new MySqlParameter
            {
                Value = player
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    server = (ulong) result;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return server;
        }

        internal async Task SetPlayers(ulong server, Queue<ulong> players)
        {
            var json = JsonConvert.SerializeObject(players);
            var parameters = new List<MySqlParameter>();

            const string commandText = "UPDATE Games SET queue = ? WHERE inGame = 1 AND server = ?";

            var p1 = new MySqlParameter
            {
                Value = json
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<Queue<ulong>> GetUsersWithServer(ulong server)
        {
            var players = new Queue<ulong>();
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT userid FROM Players WHERE inGame = 1 AND server = ?";

            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            await using var dr = await MySqlHelper.ExecuteReaderAsync(_connString, commandText, parameters.ToArray());
            try
            {
                while (dr.Read()) players.Enqueue(dr.GetUInt64(0));
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return players;
        }

        internal async Task<ulong> GetUNOPlayer(ulong server)
        {
            ulong player = 0;
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT oneCardLeft FROM Games WHERE inGame = 1 AND server = ?";

            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    player = (ulong) result;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return player;
        }

        internal async Task SetUNOPlayer(ulong server, ulong player)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText = "UPDATE Games SET oneCardLeft = ? WHERE inGame = 1 AND server = ?";

            var p1 = new MySqlParameter
            {
                Value = player
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = server
            };
            
            parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task SetDefaultChannel(ulong server, ulong channel)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText = "UPDATE Games SET playChannel = ? WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = channel
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task SetHasDefaultChannel(ulong server, bool hasDefault)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText = "UPDATE Games SET hasDefaultChannel = ? WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = hasDefault
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<bool> HasDefaultChannel(ulong server)
        {
            var yesOrNo = false;
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT hasDefaultChannel FROM UNObot.Games WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    yesOrNo = (byte) result == 1;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return yesOrNo;
        }

        internal async Task<bool> ChannelEnforced(ulong server)
        {
            var yesOrNo = false;
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT enforceChannel FROM UNObot.Games WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    yesOrNo = (byte) result == 1;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return yesOrNo;
        }

        internal async Task SetEnforceChannel(ulong server, bool enforce)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText = "UPDATE Games SET enforceChannel = ? WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = enforce
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<ulong> GetDefaultChannel(ulong server)
        {
            ulong channel = 0;
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT playChannel FROM UNObot.Games WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    channel = (ulong) result;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return channel;
        }

        internal async Task<List<ulong>> GetAllowedChannels(ulong server)
        {
            var allowedChannels = new List<ulong>();
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT allowedChannels FROM UNObot.Games WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if(result.HasDBValue())
                    allowedChannels = JsonConvert.DeserializeObject<List<ulong>>((string) result);
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return allowedChannels;
        }

        internal async Task SetAllowedChannels(ulong server, List<ulong> allowedChannels)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText = "UPDATE Games SET allowedChannels = ? WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = JsonConvert.SerializeObject(allowedChannels)
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<Card> GetCurrentCard(ulong server)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT currentCard FROM Games WHERE inGame = 1 AND server = ?";

            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            // If parsing fails, we can throw an NRE.
            Card card = null;
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if(result.HasDBValue())
                    card = JsonConvert.DeserializeObject<Card>((string) result);
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return card;
        }

        internal async Task SetCurrentCard(ulong server, Card card)
        {
            var cardJson = JsonConvert.SerializeObject(card);
            var parameters = new List<MySqlParameter>();

            const string commandText = "UPDATE Games SET currentCard = ? WHERE inGame = 1 AND server = ?";

            var p1 = new MySqlParameter
            {
                Value = cardJson
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<bool> IsPlayerInGame(ulong player)
        {
            var yesOrNo = false;
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT inGame FROM UNObot.Players WHERE userid = ?";

            var p1 = new MySqlParameter
            {
                Value = player
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    yesOrNo = (byte) result == 1;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return yesOrNo;
        }

        internal async Task<bool> IsPlayerInServerGame(ulong player, ulong server)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT queue FROM UNObot.Games WHERE server = ?";

            var players = new Queue<ulong>();
            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if(result.HasDBValue())
                    players = JsonConvert.DeserializeObject<Queue<ulong>>((string) result);
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return players.Contains(player);
        }

        internal async Task<List<Card>> GetCards(ulong player)
        {
            var cards = new List<Card>();
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT cards FROM UNObot.Players WHERE userid = ?";

            var p1 = new MySqlParameter
            {
                Value = player
            };
            parameters.Add(p1);
            await using var dr = await MySqlHelper.ExecuteReaderAsync(_connString, commandText, parameters.ToArray());
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    cards = JsonConvert.DeserializeObject<List<Card>>((string) result);
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return cards;
        }

        internal async Task<bool> UserExists(ulong player)
        {
            var exists = false;
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT EXISTS(SELECT 1 FROM UNObot.Players WHERE userid = ?)";

            var p1 = new MySqlParameter
            {
                Value = player
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    exists = (int) result == 1;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return exists;
        }

        internal async Task GetUsersAndAdd(ulong server)
        {
            var players = await GetUsersWithServer(server);
            //slight randomization of order
            for (var i = 0; i < ThreadSafeRandom.ThisThreadsRandom.Next(0, players.Count - 1); i++)
                players.Enqueue(players.Dequeue());
            if (players.Count == 0)
                _logger.Log(LogSeverity.Warning, "Why is the list empty when I'm getting players?");
            var json = JsonConvert.SerializeObject(players);
            var parameters = new List<MySqlParameter>();
            const string commandText = "UPDATE UNObot.Games SET queue = ? WHERE server = ? AND inGame = 1";

            var p1 = new MySqlParameter();
            var p2 = new MySqlParameter();
            p1.Value = json;
            p2.Value = server;
            parameters.Add(p1);
            parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task StarterCard(ulong server)
        {
            var players = await GetPlayers(server);
            foreach (var player in players)
            {
                await AddCard(player, Card.RandomCard(7));
            }
        }

        internal async Task<int[]> GetStats(ulong player)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT gamesJoined,gamesPlayed,gamesWon FROM UNObot.Players WHERE userid = ?";

            var p1 = new MySqlParameter
            {
                Value = player
            };
            parameters.Add(p1);
            int[] stats = {0, 0, 0};
            await using var dr = await MySqlHelper.ExecuteReaderAsync(_connString, commandText, parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    stats[0] = dr.GetInt32(0);
                    stats[1] = dr.GetInt32(1);
                    stats[2] = dr.GetInt32(2);
                }
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return stats;
        }

        internal async Task<string> GetNote(ulong player)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT note FROM UNObot.Players WHERE userid = ?";

            var p1 = new MySqlParameter
            {
                Value = player
            };
            parameters.Add(p1);
            string message = null;
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    message = (string) result;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return message;
        }

        internal async Task SetNote(ulong player, string note)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText = "UPDATE UNObot.Players SET note = ? WHERE userid = ?";

            var p1 = new MySqlParameter
            {
                Value = note
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = player
            };
            parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task RemoveNote(ulong player)
        {
            var parameters = new List<MySqlParameter>();

            const string commandText = "UPDATE UNObot.Players SET note = NULL WHERE userid = ?";

            var p1 = new MySqlParameter
            {
                Value = player
            };
            parameters.Add(p1);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task UpdateStats(ulong player, int mode)
        {
            //1 is gamesJoined
            //2 is gamesPlayed
            //3 is gamesWon
            var parameters = new List<MySqlParameter>();
            var commandText = mode switch
            {
                1 => "UPDATE UNObot.Players SET gamesJoined = gamesJoined + 1 WHERE userid = ?",
                2 => "UPDATE UNObot.Players SET gamesPlayed = gamesPlayed + 1 WHERE userid = ?",
                3 => "UPDATE UNObot.Players SET gamesWon = gamesWon + 1 WHERE userid = ?",
                _ => ""
            };
            var p1 = new MySqlParameter
            {
                Value = player
            };
            parameters.Add(p1);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task AddCard(ulong player, params Card[] cardsAdd)
        {
            var cards = await GetCards(player) ?? new List<Card>();
            cards.AddRange(cardsAdd);
            await SetCards(player, cards);
        }

        private async Task SetCards(ulong player, List<Card> cards)
        {
            cards ??= new List<Card>();
            var json = JsonConvert.SerializeObject(cards);
            var parameters = new List<MySqlParameter>();

            const string commandText = "UPDATE UNObot.Players SET cards = ? WHERE userid = ?";

            var p1 = new MySqlParameter
            {
                Value = json
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = player
            };
            parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<char> GetPrefix(ulong server)
        {
            var prefix = '!';
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT commandPrefix FROM Games WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    prefix = (char) result;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return prefix;
        }

        internal async Task SetPrefix(ulong server, char prefix)
        {
            var parameters = new List<MySqlParameter>();
            const string commandText = "UPDATE Games SET commandPrefix = ? WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = prefix
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<bool> RemoveCard(ulong player, Card card)
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
            var parameters = new List<MySqlParameter>();

            const string commandText = "UPDATE UNObot.Players SET cards = ? WHERE userid = ?";

            var p1 = new MySqlParameter
            {
                Value = json
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = player
            };
            parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return true;
        }

        internal async Task SetServerCards(ulong server, string text)
        {
            const string commandText = "UPDATE Games SET cards = ? WHERE server = ?";
            var parameters = new List<MySqlParameter>();
            var p1 = new MySqlParameter
            {
                Value = text
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            parameters.Add(p2);

            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<List<Card>> GetServerCards(ulong server)
        {
            var cards = new List<Card>();
            const string commandText = "SELECT cards FROM UNObot.Games WHERE server = ?";
            var parameters = new List<MySqlParameter>();
            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    cards = JsonConvert.DeserializeObject<List<Card>>((string) result);
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return cards;
        }

        internal async Task<string> GetMinecraftUser(ulong user)
        {
            string username = null;
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT minecraftUsername FROM UNObot.Players WHERE userid = ?";

            var p1 = new MySqlParameter
            {
                Value = user
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    username = (string) result;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return username;
        }

        internal async Task<int> GetCardsDrawn(ulong server)
        {
            var cardsDrawn = 0;
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT cardsDrawn FROM UNObot.Games WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(_connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    cardsDrawn = (int) result;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return cardsDrawn;
        }

        internal async Task SetCardsDrawn(ulong server, int count)
        {
            const string commandText = "UPDATE Games SET cardsDrawn = ? WHERE server = ?";
            var parameters = new List<MySqlParameter>();
            var p1 = new MySqlParameter
            {
                Value = count
            };
            parameters.Add(p1);
            var p2 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p2);

            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task AddWebhook(string key, ulong guild, ulong channel, string type = "bitbucket")
        {
            const string commandText =
                "INSERT INTO UNObot.Webhooks (webhookKey, channel, guild, type) VALUES (?, ?, ?, ?)";

            var p1 = new MySqlParameter
            {
                Value = key.Substring(0, 50)
            };
            var p2 = new MySqlParameter
            {
                Value = channel
            };
            var p3 = new MySqlParameter
            {
                Value = guild
            };
            var p4 = new MySqlParameter
            {
                Value = type
            };

            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, p1, p2, p3, p4);
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task DeleteWebhook(string key)
        {
            const string commandText = "DELETE FROM UNObot.Webhooks WHERE webhookKey = ?";
            var parameters = new List<MySqlParameter>();
            var p1 = new MySqlParameter
            {
                Value = key.Substring(0, Math.Min(key.Length, 50))
            };
            parameters.Add(p1);

            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(_connString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<(ulong Guild, byte Type)> GetWebhook(ulong channel, string key)
        {
            const string commandText = "SELECT guild, type FROM UNObot.Webhooks WHERE channel = ? AND webhookKey = ?";
            ulong guild = 0;
            byte type = 0;
            var parameters = new List<MySqlParameter>();
            var p1 = new MySqlParameter
            {
                Value = channel
            };
            var p2 = new MySqlParameter
            {
                Value = key.Substring(0, Math.Min(key.Length, 50))
            };
            parameters.Add(p1);
            parameters.Add(p2);
            await using var dr = await MySqlHelper.ExecuteReaderAsync(_connString, commandText, parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    guild = dr.GetUInt64(0);
                    type = dr.GetByte(1);
                }
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return (guild, type);
        }

        /*
        internal class Server
        {
            internal bool InGame;

            internal Server(ulong ID)
            {
                this.ID = ID;
                const string CommandText = "SELECT * FROM UNObot.Games WHERE server = ?";
                var Parameters = new List<MySqlParameter>();
                var p1 = new MySqlParameter
                {
                    Value = ID
                };
                Parameters.Add(p1);

                using var dr = MySqlHelper.ExecuteReader(ConnString, CommandText, Parameters.ToArray());
                try
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

            internal ulong ID { get; }
            internal UNOCoreServices.GameMode Gamemode { get; }
            private string CurrentCardJSON { get; }
            internal Card CurrentCard { get; private set; }
            internal ulong UNOUser { get; }
            internal int CardsDrawn { get; }
            private string QueueJSON { get; }
            internal Queue<ulong> Queue { get; private set; }
            internal string Description { get; }
            internal ulong PlayChannel { get; }
            internal bool HasDefaultChannel { get; }
            internal bool EnforceChannel { get; }
            private string AllowedChannelsJSON { get; }
            internal List<ulong> AllowedChannels { get; private set; }
            internal char CommandPrefix { get; }
            private string CardsJSON { get; }
            internal List<Card> Cards { get; private set; }

            internal void ParseJSON()
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