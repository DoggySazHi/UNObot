﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UNObot.Services
{
    public static class UNODatabaseService
    {
        private static string ConnString = "";

        private static void GetConnectionString()
        {
            using (var r = new StreamReader("config.json"))
            {
                var json = r.ReadToEnd();
                var jObject = JObject.Parse(json);
                if (jObject["connStr"] == null)
                {
                    LoggerService.Log(LogSeverity.Critical, "ERROR: Database string has not been written in config.json!\nIt must contain a connStr.");
                    return;
                }
                ConnString = (string)jObject["connStr"];
            }
            //ha, damn the limited encodings.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding.GetEncoding("windows-1254");
        }
        
        public static async Task CleanAll()
        {
            using (StreamReader r = new StreamReader("config.json"))
            {
                string json = r.ReadToEnd();
                JObject jObject = JObject.Parse(json);
                if (jObject["version"] == null)
                {
                    LoggerService.Log(LogSeverity.Error, "ERROR: Version has not been written in config.json!\nIt must contain a version.");
                    return;
                }
                Program.version = (string)jObject["version"];
                LoggerService.Log(LogSeverity.Info, $"Running {Program.version}!");
            }
            GetConnectionString();
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SET SQL_SAFE_UPDATES = 0; UPDATE UNObot.Players SET cards = ?, inGame = 0, server = null, gameName = null; UPDATE Games SET inGame = 0, currentCard = ?, `order` = 1, oneCardLeft = 0, queue = ?, description = null; SET SQL_SAFE_UPDATES = 1;";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = "[]"
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = "[]"
            };
            Parameters.Add(p2);
            MySqlParameter p3 = new MySqlParameter
            {
                Value = "[]"
            };
            Parameters.Add(p3);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        public static async Task AddGame(ulong server)
        {
            const string CommandText = "INSERT IGNORE INTO Games (server) VALUES(?)";
            List<MySqlParameter> Parameters = new List<MySqlParameter>();
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);

            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public struct Server
        {
            public ulong ID { get; }
            public bool InGame { get; }
            public Card CurrentCard { get; }
            public ulong UNOUser { get; }
            public int CardsDrawn { get; }
            public Queue<ulong> Queue { get; }
            public string Description { get; }
            public ulong PlayChannel { get; }
            public bool HasDefaultChannel { get; }
            public bool EnforceChannels { get; }
            public List<ulong> AllowedChannels { get; }
            public char CommandPrefix { get; }
            public List<Card> Cards { get; }

            /*
            public Server(ulong ID)
            {
                this.ID = ID;
                const string CommandText = "SELECT * FROM UNObot.Games WHERE server = ?";
                var Parameters = new List<MySqlParameter>();
                var p1 = new MySqlParameter
                {
                    Value = ID
                };
                Parameters.Add(p1);
                
                using MySqlDataReader dr = MySqlHelper.ExecuteReader(ConnString, CommandText, Parameters.ToArray());
                try
                {
                    while (dr.Read())
                    {
                        if (!await dr.IsDBNullAsync(0))
                            description = dr.GetString(0);
                    }
                }
                catch (MySqlException ex)
                {
                    LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
                }
                return description;
            }
            */
        }
        
        public static async Task UpdateDescription(ulong server, string text)
        {
            const string CommandText = "UPDATE Games SET description = ? WHERE server = ?";
            List<MySqlParameter> Parameters = new List<MySqlParameter>();
            MySqlParameter p1 = new MySqlParameter
            {
                Value = text
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p2);

            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task<string> GetDescription(ulong server)
        {
            const string CommandText = "SELECT description FROM UNObot.Games WHERE server = ?";
            string description = "";
            List<MySqlParameter> Parameters = new List<MySqlParameter>();
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    if (!await dr.IsDBNullAsync(0))
                        description = dr.GetString(0);
                }
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return description;
        }
        public static async Task ResetGame(ulong server)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "UPDATE Games SET inGame = 0, currentCard = ?, `order` = 1, oneCardLeft = 0, queue = ?, description = null WHERE server = ?";
            AFKtimer.DeleteTimer(server);
            MySqlParameter p1 = new MySqlParameter
            {
                Value = "[]"
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = "[]"
            };
            Parameters.Add(p2);
            MySqlParameter p3 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p3);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task<bool> IsServerInGame(ulong server)
        {
            bool yesorno = false;
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT inGame FROM UNObot.Games WHERE server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    yesorno |= dr.GetByte(0) == 1;
                }
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return yesorno;
        }
        public static async Task AddUser(ulong id, string usrname, ulong server)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "INSERT INTO Players (userid, username, inGame, cards, server) VALUES(?, ?, 1, ?, ?) ON DUPLICATE KEY UPDATE username = ?, inGame = 1, cards = ?, server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = id
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = usrname
            };
            Parameters.Add(p2);
            MySqlParameter p3 = new MySqlParameter
            {
                Value = "[]"
            };
            Parameters.Add(p3);
            MySqlParameter p4 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p4);
            MySqlParameter p5 = new MySqlParameter
            {
                Value = usrname
            };
            Parameters.Add(p5);
            MySqlParameter p6 = new MySqlParameter
            {
                Value = "[]"
            };
            Parameters.Add(p6);
            MySqlParameter p7 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p7);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task AddUser(ulong id, string usrname)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "INSERT INTO Players (userid, username) VALUES(?, ?) ON DUPLICATE KEY UPDATE username = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = id
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = usrname
            };
            Parameters.Add(p2);
            MySqlParameter p3 = new MySqlParameter
            {
                Value = usrname
            };
            Parameters.Add(p3);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task RemoveUser(ulong id)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "INSERT INTO Players (userid, inGame, cards, server) VALUES(?, 0, ?, null) ON DUPLICATE KEY UPDATE inGame = 0, cards = ?, server = null";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = id
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = "[]"
            };
            Parameters.Add(p2);
            MySqlParameter p3 = new MySqlParameter
            {
                Value = "[]"
            };
            Parameters.Add(p3);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        public static async Task AddGuild(ulong Guild, ushort ingame)
        => await AddGuild(Guild, ingame, 1);

        public static async Task AddGuild(ulong Guild, ushort ingame, ushort gamemode)
        {
            /* 
             * 1 - In a regular game.
             * 2 - In a game that prevents seeing other players' cards.
             * 3 (maybe?) - Allows skipping of a turn after drawing 2 cards.
            */
            List<MySqlParameter> Parameters = new List<MySqlParameter>();
            const string CommandText = "INSERT INTO Games (server, inGame, gameMode) VALUES(?, ?, ?) ON DUPLICATE KEY UPDATE inGame = ?, gameMode = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = Guild
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = ingame
            };
            Parameters.Add(p2);
            MySqlParameter p3 = new MySqlParameter
            {
                Value = gamemode
            };
            Parameters.Add(p3);
            MySqlParameter p4 = new MySqlParameter
            {
                Value = ingame
            };
            Parameters.Add(p4);
            MySqlParameter p5 = new MySqlParameter
            {
                Value = gamemode
            };
            Parameters.Add(p5);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task<UNOCoreServices.Gamemodes> GetGamemode(ulong server)
        {
            var Gamemode = UNOCoreServices.Gamemodes.Normal;
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT gameMode FROM UNObot.Games WHERE server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    Gamemode = (UNOCoreServices.Gamemodes) dr.GetUInt16(0);
                }
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return Gamemode;
        }
        //NOTE THAT THIS GETS DIRECTLY FROM SERVER; YOU MUST AddPlayersToServer
        public static async Task<Queue<ulong>> GetPlayers(ulong server)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT queue FROM Games WHERE inGame = 1 AND server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            Queue<ulong> players = new Queue<ulong>();
            await using (MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray()))
            {
                try
                {
                    while (dr.Read())
                    {
                        players = JsonConvert.DeserializeObject<Queue<ulong>>(dr.GetString(0));
                    }
                }
                catch (MySqlException ex)
                {
                    LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
                }
            }
            return players;
        }
        public static async Task<ulong> GetUserServer(ulong player)
        {
            ulong server = 0;
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT server FROM Players WHERE inGame = 1";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = player
            };
            Parameters.Add(p1);
            await using (MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray()))
            {
                try
                {
                    while (await dr.ReadAsync())
                    {
                        server = dr.GetUInt64(0);
                    }
                }
                catch (MySqlException ex)
                {
                    LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
                }
            }
            return server;
        }
        public static async Task SetPlayers(ulong server, Queue<ulong> players)
        {
            string json = JsonConvert.SerializeObject(players);
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "UPDATE Games SET queue = ? WHERE inGame = 1 AND server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = json
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task<Queue<ulong>> GetUsersWithServer(ulong server)
        {
            Queue<ulong> players = new Queue<ulong>();
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT userid FROM Players WHERE inGame = 1 AND server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    players.Enqueue(dr.GetUInt64(0));
                }
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return players;
        }
        public static async Task<ulong> GetUNOPlayer(ulong server)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT oneCardLeft FROM Games WHERE inGame = 1 AND server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            ulong player = 0;
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    if (!await dr.IsDBNullAsync(0))
                        player = dr.GetUInt64(0);
                }
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return player;
        }
        public static async Task SetUNOPlayer(ulong server, ulong player)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "UPDATE Games SET oneCardLeft = ? WHERE inGame = 1 AND server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = player
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task SetDefaultChannel(ulong server, ulong channel)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "UPDATE Games SET playChannel = ? WHERE server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = channel
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task SetHasDefaultChannel(ulong server, bool hasDefault)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "UPDATE Games SET hasDefaultChannel = ? WHERE server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = hasDefault
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task<bool> HasDefaultChannel(ulong server)
        {
            bool yesorno = false;
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT hasDefaultChannel FROM UNObot.Games WHERE server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                    yesorno |= dr.GetByte(0) == 1;
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return yesorno;
        }
        public static async Task<bool> EnforceChannel(ulong server)
        {
            bool yesorno = false;
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT enforceChannel FROM UNObot.Games WHERE server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                    yesorno |= dr.GetByte(0) == 1;
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return yesorno;
        }
        public static async Task SetEnforceChannel(ulong server, bool enforce)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "UPDATE Games SET enforceChannel = ? WHERE server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = enforce
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task<ulong> GetDefaultChannel(ulong server)
        {
            ulong channel = 0;
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT playChannel FROM UNObot.Games WHERE server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    channel = dr.GetUInt64(0);
                }
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return channel;
        }
        public static async Task<List<ulong>> GetAllowedChannels(ulong server)
        {
            List<ulong> allowedChannels = new List<ulong>();
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT allowedChannels FROM UNObot.Games WHERE server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                    allowedChannels = JsonConvert.DeserializeObject<List<ulong>>(dr.GetString(0));
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return allowedChannels;
        }
        public static async Task SetAllowedChannels(ulong server, List<ulong> allowedChannels)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "UPDATE Games SET allowedChannels = ? WHERE server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = JsonConvert.SerializeObject(allowedChannels)
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task<Card> GetCurrentCard(ulong server)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT currentCard FROM Games WHERE inGame = 1 AND server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            Card card = new Card();
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    card = JsonConvert.DeserializeObject<Card>(dr.GetString(0));
                }
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return card;
        }
        public static async Task SetCurrentCard(ulong server, Card card)
        {
            string cardJSON = JsonConvert.SerializeObject(card);
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "UPDATE Games SET currentCard = ? WHERE inGame = 1 AND server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = cardJSON
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task<bool> IsPlayerInGame(ulong player)
        {
            bool yesorno = false;
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT inGame FROM UNObot.Players WHERE userid = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = player
            };
            Parameters.Add(p1);
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    yesorno |= dr.GetByte(0) == 1;
                }
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return yesorno;
        }
        public static async Task<bool> IsPlayerInServerGame(ulong player, ulong server)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT queue FROM UNObot.Games WHERE server = ?";

            Queue<ulong> players = new Queue<ulong>();
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    players = JsonConvert.DeserializeObject<Queue<ulong>>(dr.GetString(0));
                }
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return players.Contains(player);
        }
        //Done?
        public static async Task<List<Card>> GetCards(ulong player)
        {
            string jsonstring = "";
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT cards FROM UNObot.Players WHERE userid = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = player
            };
            Parameters.Add(p1);
            List<Card> cards;
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    jsonstring = dr.GetString(0);
                }
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            cards = JsonConvert.DeserializeObject<List<Card>>(jsonstring);
            return cards;
        }
        public static async Task<bool> UserExists(ulong player)
        {
            bool exists = false;
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT EXISTS(SELECT 1 FROM UNObot.Players WHERE userid = ?)";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = player
            };
            Parameters.Add(p1);
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    exists |= dr.GetInt64(0) == 1;
                }
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return exists;
        }
        public static async Task GetUsersAndAdd(ulong server)
        {
            Queue<ulong> players = await GetUsersWithServer(server);
            //slight randomization of order
            for (int i = 0; i < ThreadSafeRandom.ThisThreadsRandom.Next(0, players.Count - 1); i++)
                players.Enqueue(players.Dequeue());
            if (players.Count == 0)
                LoggerService.Log(LogSeverity.Warning, "Why is the list empty when I'm getting players?");
            string json = JsonConvert.SerializeObject(players);
            List<MySqlParameter> Parameters = new List<MySqlParameter>();
            const string CommandText = "UPDATE UNObot.Games SET queue = ? WHERE server = ? AND inGame = 1";

            MySqlParameter p1 = new MySqlParameter();
            MySqlParameter p2 = new MySqlParameter();
            p1.Value = json;
            p2.Value = server;
            Parameters.Add(p1);
            Parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task StarterCard(ulong server)
        {
            Queue<ulong> players = await GetPlayers(server);
            foreach (ulong player in players)
            {
                for (int i = 0; i < 7; i++)
                {
                    await AddCard(player, UNOCoreServices.RandomCard());
                }
            }
        }
        public static async Task<int[]> GetStats(ulong player)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT gamesJoined,gamesPlayed,gamesWon FROM UNObot.Players WHERE userid = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = player
            };
            Parameters.Add(p1);
            int[] stats = { 0, 0, 0 };
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
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
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return stats;
        }
        public static async Task<string> GetNote(ulong player)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT note FROM UNObot.Players WHERE userid = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = player
            };
            Parameters.Add(p1);
            String message = null;
            await using (MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray()))
            {
                try
                {
                    while (dr.Read())
                    {
                        if (!await dr.IsDBNullAsync(0))
                            message = dr.GetString(0);
                    }
                }
                catch (MySqlException ex)
                {
                    LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
                }
            }
            return message;
        }
        public static async Task SetNote(ulong player, string note)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "UPDATE UNObot.Players SET note = ? WHERE userid = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = note
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = player
            };
            Parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task RemoveNote(ulong player)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "UPDATE UNObot.Players SET note = NULL WHERE userid = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = player
            };
            Parameters.Add(p1);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task UpdateStats(ulong player, int mode)
        {
            //1 is gamesJoined
            //2 is gamesPlayed
            //3 is gamesWon
            List<MySqlParameter> Parameters = new List<MySqlParameter>();
            string CommandText = "";
            switch (mode)
            {
                case 1:
                    CommandText = "UPDATE UNObot.Players SET gamesJoined = gamesJoined + 1 WHERE userid = ?";
                    break;
                case 2:
                    CommandText = "UPDATE UNObot.Players SET gamesPlayed = gamesPlayed + 1 WHERE userid = ?";
                    break;
                case 3:
                    CommandText = "UPDATE UNObot.Players SET gamesWon = gamesWon + 1 WHERE userid = ?";
                    break;
            }
            MySqlParameter p1 = new MySqlParameter
            {
                Value = player
            };
            Parameters.Add(p1);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task AddCard(ulong player, Card card)
        {
            List<Card> cards = await GetCards(player);
            if (cards == null)
                cards = new List<Card>();
            cards.Add(card);
            string json = JsonConvert.SerializeObject(cards);
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "UPDATE UNObot.Players SET cards = ? WHERE userid = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = json
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = player
            };
            Parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task SetCards(ulong player, List<Card> cards)
        {
            if (cards == null)
                cards = new List<Card>();
            string json = JsonConvert.SerializeObject(cards);
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "UPDATE UNObot.Players SET cards = ? WHERE userid = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = json
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = player
            };
            Parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task<char> GetPrefix(ulong server)
        {
            char prefix = '!';
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT commandPrefix FROM Games WHERE server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            await using (MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray()))
            {
                try
                {
                    while (dr.Read())
                    {
                        prefix = dr.GetChar(0);
                    }
                }
                catch (MySqlException ex)
                {
                    LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
                }
            }
            return prefix;
        }
        public static async Task SetPrefix(ulong server, char prefix)
        {
            List<MySqlParameter> Parameters = new List<MySqlParameter>();
            const string CommandText = "UPDATE Games SET commandPrefix = ? WHERE server = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = prefix
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        public static async Task<bool> RemoveCard(ulong player, Card card)
        {
            bool foundCard = false;
            List<Card> cards = await GetCards(player);
            int currentPlace = 0;
            foreach (Card cardindeck in cards)
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
            string json = JsonConvert.SerializeObject(cards);
            List<MySqlParameter> Parameters = new List<MySqlParameter>();

            const string CommandText = "UPDATE UNObot.Players SET cards = ? WHERE userid = ?";

            MySqlParameter p1 = new MySqlParameter
            {
                Value = json
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = player
            };
            Parameters.Add(p2);
            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return true;
        }
        public static async Task SetServerCards(ulong server, string text)
        {
            const string CommandText = "UPDATE Games SET cards = ? WHERE server = ?";
            List<MySqlParameter> Parameters = new List<MySqlParameter>();
            MySqlParameter p1 = new MySqlParameter
            {
                Value = text
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            Parameters.Add(p2);

            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        public static async Task<string> GetServerCards(ulong server)
        {
            const string CommandText = "SELECT cards FROM UNObot.Games WHERE server = ?";
            string description = "";
            List<MySqlParameter> Parameters = new List<MySqlParameter>();
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Parameters.Add(p1);
            await using MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    if (!await dr.IsDBNullAsync(0))
                        description = dr.GetString(0);
                }
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return description;
        }

        public static async Task<string> GetMinecraftUser(ulong User)
        {
            string Username = null;
            var Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT minecraftUsername FROM UNObot.Players WHERE userid = ?";

            var p1 = new MySqlParameter
            {
                Value = User
            };
            Parameters.Add(p1);
            await using var dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                    if (!dr.IsDBNull(0))
                        Username = dr.GetString(0);
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return Username;
        }

        public static async Task<int> GetCardsDrawn(ulong Server)
        {
            int CardsDrawn = 0;
            var Parameters = new List<MySqlParameter>();

            const string CommandText = "SELECT cardsDrawn FROM UNObot.Games WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = Server
            };
            Parameters.Add(p1);
            await using var dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                    if (!dr.IsDBNull(0))
                        CardsDrawn = dr.GetInt32(0);
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return CardsDrawn;
        }
        
        public static async Task SetCardsDrawn(ulong Server, int Count)
        {
            const string CommandText = "UPDATE Games SET cardsDrawn = ? WHERE server = ?";
            List<MySqlParameter> Parameters = new List<MySqlParameter>();
            MySqlParameter p1 = new MySqlParameter
            {
                Value = Count
            };
            Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = Server
            };
            Parameters.Add(p2);

            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        public static async Task AddWebhook(string Key, ulong Guild, ulong Channel, string Type = "bitbucket")
        {
            const string CommandText = "INSERT INTO UNObot.Webhooks (webhookKey, channel, guild, type) VALUES (?, ?, ?, ?)";

            var p1 = new MySqlParameter
            {
                Value = Key.Substring(0, 50)
            };
            var p2 = new MySqlParameter
            {
                Value = Channel
            };
            var p3 = new MySqlParameter
            {
                Value = Guild
            };
            var p4 = new MySqlParameter
            {
                Value = Type
            };

            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, p1, p2, p3, p4);
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        public static async Task DeleteWebhook(string Key)
        {
            const string CommandText = "DELETE FROM UNObot.Webhooks WHERE webhookKey = ?";
            var Parameters = new List<MySqlParameter>();
            var p1 = new MySqlParameter
            {
                Value = Key.Substring(0, Math.Min(Key.Length, 50))
            };
            Parameters.Add(p1);

            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, CommandText, Parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        public static async Task<(ulong Guild, byte Type)> GetWebhook(ulong Channel, string Key)
        {
            const string CommandText = "SELECT guild, type FROM UNObot.Webhooks WHERE channel = ? AND webhookKey = ?";
            ulong Guild = 0;
            byte Type = 0;
            var Parameters = new List<MySqlParameter>();
            var p1 = new MySqlParameter
            {
                Value = Channel
            };
            var p2 = new MySqlParameter
            {
                Value = Key.Substring(0, Math.Min(Key.Length, 50))
            };
            Parameters.Add(p1);
            Parameters.Add(p2);
            await using var dr = await MySqlHelper.ExecuteReaderAsync(ConnString, CommandText, Parameters.ToArray());
            try
            {
                while (dr.Read())
                {
                    Guild = dr.GetUInt64(0);
                    Type = dr.GetByte(1);
                }
            }
            catch (MySqlException ex)
            {
                LoggerService.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            return (Guild, Type);
        }
    }
}