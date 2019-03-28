using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#pragma warning disable CS1701 // Assuming assembly reference matches identity
#pragma warning disable CS1702 // Assuming assembly reference matches identity
namespace UNObot.Modules
{
    public static class UNOdb
    {
        public static string ConnString = "";
        public static void GetConnectionString()
        {
            using (StreamReader r = new StreamReader("config.json"))
            {
                string json = r.ReadToEnd();
                JObject jObject = JObject.Parse(json);
                if (jObject["connStr"] == null)
                {
                    Console.WriteLine("ERROR: Database string has not been written in config.json!\nIt must contain a connStr.");
                    return;
                }
                ConnString = (string)jObject["connStr"];
            }
            //ha, damn the limited encodings.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding.GetEncoding("windows-1254");
        }
        public static async Task AddGame(ulong server)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);

            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, "INSERT IGNORE INTO Games (server) VALUES(?)", parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task UpdateDescription(ulong server, string text)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            MySqlParameter p1 = new MySqlParameter
            {
                Value = text
            };
            parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p2);

            try
            {
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, "UPDATE Games SET description = ? WHERE server = ?", parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task<string> GetDescription(ulong server)
        {
            string description = "";
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            using (MySqlDataReader dr = await MySqlHelper.ExecuteReaderAsync(ConnString, "SELECT description FROM UNObot.Games WHERE server = ?", parameters.ToArray()))
            {
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
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                return description;
            }
        }
        public static async Task ResetGame(ulong server)
        {
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "UPDATE Games SET inGame = 0, currentCard = ?, `order` = 1, oneCardLeft = 0, queue = ?, description = null WHERE server = ?"
            };
            AFKtimer.DeleteTimer(server);
            MySqlParameter p1 = new MySqlParameter
            {
                Value = "[]"
            };
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = "[]"
            };
            Cmd.Parameters.Add(p2);
            MySqlParameter p3 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p3);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task<bool> IsServerInGame(ulong server)
        {
            bool yesorno = false;
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "SELECT inGame FROM UNObot.Games WHERE server = ?"
            };
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        yesorno |= dr.GetByte(0) == 1;
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                return yesorno;
            }
        }
        public static async Task AddUser(ulong id, string usrname, ulong server)
        {
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "INSERT INTO Players (userid, username, inGame, cards, server) VALUES(?, ?, 1, ?, ?) ON DUPLICATE KEY UPDATE username = ?, inGame = 1, cards = ?, server = ?"
            };
            MySqlParameter p1 = new MySqlParameter
            {
                Value = id
            };
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = usrname
            };
            Cmd.Parameters.Add(p2);
            MySqlParameter p3 = new MySqlParameter
            {
                Value = "[]"
            };
            Cmd.Parameters.Add(p3);
            MySqlParameter p4 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p4);
            MySqlParameter p5 = new MySqlParameter
            {
                Value = usrname
            };
            Cmd.Parameters.Add(p5);
            MySqlParameter p6 = new MySqlParameter
            {
                Value = "[]"
            };
            Cmd.Parameters.Add(p6);
            MySqlParameter p7 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p7);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task AddUser(ulong id, string usrname)
        {
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "INSERT INTO Players (userid, username) VALUES(?, ?) ON DUPLICATE KEY UPDATE username = ?"
            };
            MySqlParameter p1 = new MySqlParameter
            {
                Value = id
            };
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = usrname
            };
            Cmd.Parameters.Add(p2);
            MySqlParameter p3 = new MySqlParameter
            {
                Value = usrname
            };
            Cmd.Parameters.Add(p3);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task RemoveUser(ulong id)
        {
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "INSERT INTO Players (userid, inGame, cards, server) VALUES(?, 0, ?, null) ON DUPLICATE KEY UPDATE inGame = 0, cards = ?, server = null"
            };
            MySqlParameter p1 = new MySqlParameter
            {
                Value = id
            };
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = "[]"
            };
            Cmd.Parameters.Add(p2);
            MySqlParameter p3 = new MySqlParameter
            {
                Value = "[]"
            };
            Cmd.Parameters.Add(p3);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task CleanAll()
        {
            using (StreamReader r = new StreamReader("config.json"))
            {
                string json = r.ReadToEnd();
                JObject jObject = JObject.Parse(json);
                if (jObject["version"] == null)
                {
                    Console.WriteLine("ERROR: Version has not been written in config.json!\nIt must contain a version.");
                    return;
                }
                Program.version = (string)jObject["version"];
                Console.WriteLine($"Running {Program.version}!");
            }
            GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "SET SQL_SAFE_UPDATES = 0; UPDATE UNObot.Players SET cards = ?, inGame = 0, server = null, gameName = null; UPDATE Games SET inGame = 0, currentCard = ?, `order` = 1, oneCardLeft = 0, queue = ?, description = null; SET SQL_SAFE_UPDATES = 1;"
            };
            JArray empty = new JArray();
            MySqlParameter p1 = new MySqlParameter
            {
                Value = empty.ToString()
            };
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = empty.ToString()
            };
            Cmd.Parameters.Add(p2);
            MySqlParameter p3 = new MySqlParameter
            {
                Value = empty.ToString()
            };
            Cmd.Parameters.Add(p3);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
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
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "INSERT INTO Games (server, inGame, gameMode) VALUES(?, ?, ?) ON DUPLICATE KEY UPDATE inGame = ?, gameMode = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = Guild
            };
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = ingame
            };
            Cmd.Parameters.Add(p2);
            MySqlParameter p3 = new MySqlParameter
            {
                Value = gamemode
            };
            Cmd.Parameters.Add(p3);
            MySqlParameter p4 = new MySqlParameter
            {
                Value = ingame
            };
            Cmd.Parameters.Add(p4);
            MySqlParameter p5 = new MySqlParameter
            {
                Value = gamemode
            };
            Cmd.Parameters.Add(p5);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task<ushort> GetGamemode(ulong server)
        {
            ushort gamemode = 1;
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "SELECT gameMode FROM UNObot.Games WHERE server = ?"
            };
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        gamemode = dr.GetUInt16(0);
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                return gamemode;
            }
        }
        //NOTE THAT THIS GETS DIRECTLY FROM SERVER; YOU MUST AddPlayersToServer
        public static async Task<Queue<ulong>> GetPlayers(ulong server)
        {
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT queue FROM Games WHERE inGame = 1 AND server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            Queue<ulong> players = new Queue<ulong>();
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
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
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
            }
            return players;
        }
        public static async Task<ulong> GetUserServer(ulong player)
        {
            ulong server = 0;
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT server FROM Players WHERE inGame = 1";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = player
            };
            Cmd.Parameters.Add(p1);
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
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
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
            }
            return server;
        }
        public static async Task SetPlayers(ulong server, Queue<ulong> players)
        {
            string json = JsonConvert.SerializeObject(players);
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE Games SET queue = ? WHERE inGame = 1 AND server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = json
            };
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p2);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task<Queue<ulong>> GetUsersWithServer(ulong server)
        {
            Queue<ulong> players = new Queue<ulong>();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT userid FROM Players WHERE inGame = 1 AND server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        players.Enqueue(dr.GetUInt64(0));
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                return players;
            }
        }
        public static async Task<ulong> GetUNOPlayer(ulong server)
        {
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT oneCardLeft FROM Games WHERE inGame = 1 AND server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            ulong player = 0;
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
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
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                return player;
            }
        }
        public static async Task SetUNOPlayer(ulong server, ulong player)
        {
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE Games SET oneCardLeft = ? WHERE inGame = 1 AND server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = player
            };
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p2);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task SetDefaultChannel(ulong server, ulong channel)
        {
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE Games SET playChannel = ? WHERE server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = channel
            };
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p2);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task SetHasDefaultChannel(ulong server, bool hasDefault)
        {
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE Games SET hasDefaultChannel = ? WHERE server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = hasDefault
            };
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p2);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task<bool> HasDefaultChannel(ulong server)
        {
            bool yesorno = false;
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "SELECT hasDefaultChannel FROM UNObot.Games WHERE server = ?"
            };
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                        yesorno |= dr.GetByte(0) == 1;
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                return yesorno;
            }
        }
        public static async Task<bool> EnforceChannel(ulong server)
        {
            bool yesorno = false;
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "SELECT enforceChannel FROM UNObot.Games WHERE server = ?"
            };
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                        yesorno |= dr.GetByte(0) == 1;
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                return yesorno;
            }
        }
        public static async Task SetEnforceChannel(ulong server, bool enforce)
        {
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE Games SET enforceChannel = ? WHERE server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = enforce
            };
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p2);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task<ulong> GetDefaultChannel(ulong server)
        {
            ulong channel = 0;
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "SELECT playChannel FROM UNObot.Games WHERE server = ?"
            };
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        channel = dr.GetUInt64(0);
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                return channel;
            }
        }
        public static async Task<List<ulong>> GetAllowedChannels(ulong server)
        {
            List<ulong> allowedChannels = new List<ulong>();
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "SELECT allowedChannels FROM UNObot.Games WHERE server = ?"
            };
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                        allowedChannels = JsonConvert.DeserializeObject<List<ulong>>(dr.GetString(0));
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                return allowedChannels;
            }
        }
        public static async Task SetAllowedChannels(ulong server, List<ulong> allowedChannels)
        {
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE Games SET allowedChannels = ? WHERE server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = JsonConvert.SerializeObject(allowedChannels)
            };
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p2);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task<Card> GetCurrentCard(ulong server)
        {
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT currentCard FROM Games WHERE inGame = 1 AND server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            Card card = new Card();
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        card = JsonConvert.DeserializeObject<Card>(dr.GetString(0));
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                return card;
            }
        }
        public static async Task SetCurrentCard(ulong server, Card card)
        {
            string cardJSON = JsonConvert.SerializeObject(card);
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE Games SET currentCard = ? WHERE inGame = 1 AND server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = cardJSON
            };
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p2);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task<bool> IsPlayerInGame(ulong player)
        {
            bool yesorno = false;
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT inGame FROM UNObot.Players WHERE userid = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = player;
            Cmd.Parameters.Add(p1);
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        yesorno |= dr.GetByte(0) == 1;
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                return yesorno;
            }
        }
        public static async Task<bool> IsPlayerInServerGame(ulong player, ulong server)
        {
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT queue FROM UNObot.Games WHERE server = ?";
            Queue<ulong> players = new Queue<ulong>();
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = server;
            Cmd.Parameters.Add(p1);
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
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
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                return players.Contains(player);
            }
        }
        //Done?
        public static async Task<List<Card>> GetCards(ulong player)
        {
            string jsonstring = "";
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT cards FROM UNObot.Players WHERE userid = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = player;
            Cmd.Parameters.Add(p1);
            List<Card> cards = new List<Card>();
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        jsonstring = dr.GetString(0);
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                cards = JsonConvert.DeserializeObject<List<Card>>(jsonstring);
                return cards;
            }
        }
        public static async Task<bool> UserExists(ulong player)
        {
            bool exists = false;
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM UNObot.Players WHERE userid = ?)";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = player;
            Cmd.Parameters.Add(p1);
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        exists |= dr.GetInt64(0) == 1;
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                return exists;
            }
        }
        public static async Task GetUsersAndAdd(ulong server)
        {
            Queue<ulong> players = await GetUsersWithServer(server);
            //slight randomization of order
            for (int i = 0; i < UNOcore.r.Next(0, players.Count - 1); i++)
                players.Enqueue(players.Dequeue());
            if (players.Count == 0)
                ColorConsole.WriteLine("[WARN] Why is the list empty whem I'm getting players?", ConsoleColor.Yellow);
            string json = JsonConvert.SerializeObject(players);
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE UNObot.Games SET queue = ? WHERE server = ? AND inGame = 1";
            MySqlParameter p1 = new MySqlParameter();
            MySqlParameter p2 = new MySqlParameter();
            p1.Value = json;
            p2.Value = server;
            Cmd.Parameters.Add(p1);
            Cmd.Parameters.Add(p2);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                ColorConsole.WriteLine($"A MySQL error has been caught, Error {ex}", ConsoleColor.Red);
            }
        }
        public static async Task StarterCard(ulong server)
        {
            Queue<ulong> players = await GetPlayers(server);
            foreach (ulong player in players)
            {
                for (int i = 0; i < 7; i++)
                {
                    await AddCard(player, UNOcore.RandomCard());
                }
            }
        }
        public static async Task<int[]> GetStats(ulong player)
        {
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT gamesJoined,gamesPlayed,gamesWon FROM UNObot.Players WHERE userid = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = player;
            Cmd.Parameters.Add(p1);
            int[] stats = { 0, 0, 0 };
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
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
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                return stats;
            }
        }
        public static async Task<string> GetNote(ulong player)
        {
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT note FROM UNObot.Players WHERE userid = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = player;
            Cmd.Parameters.Add(p1);
            String message = null;
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
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
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
            }
            return message;
        }
        public static async Task SetNote(ulong player, string note)
        {
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE UNObot.Players SET note = ? WHERE userid = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = note;
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter();
            p2.Value = player;
            Cmd.Parameters.Add(p2);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task RemoveNote(ulong player)
        {
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE UNObot.Players SET note = NULL WHERE userid = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = player;
            Cmd.Parameters.Add(p1);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task UpdateStats(ulong player, int mode)
        {
            //1 is gamesJoined
            //2 is gamesPlayed
            //3 is gamesWon
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            switch (mode)
            {
                case 1:
                    Cmd.CommandText = "UPDATE UNObot.Players SET gamesJoined = gamesJoined + 1 WHERE userid = ?";
                    break;
                case 2:
                    Cmd.CommandText = "UPDATE UNObot.Players SET gamesPlayed = gamesPlayed + 1 WHERE userid = ?";
                    break;
                case 3:
                    Cmd.CommandText = "UPDATE UNObot.Players SET gamesWon = gamesWon + 1 WHERE userid = ?";
                    break;
                default:
                    _ = 1;
                    break;
            }
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = player;
            Cmd.Parameters.Add(p1);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task AddCard(ulong player, Card card)
        {
            List<Card> cards = await GetCards(player);
            if (cards == null)
                cards = new List<Card>();
            cards.Add(card);
            string json = JsonConvert.SerializeObject(cards);
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE UNObot.Players SET cards = ? WHERE userid = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = json;
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter();
            p2.Value = player;
            Cmd.Parameters.Add(p2);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task SetCards(ulong player, List<Card> cards)
        {
            if (cards == null)
                cards = new List<Card>();
            string json = JsonConvert.SerializeObject(cards);
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE UNObot.Players SET cards = ? WHERE userid = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = json;
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter();
            p2.Value = player;
            Cmd.Parameters.Add(p2);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
        }
        public static async Task<char> GetPrefix(ulong server)
        {
            char prefix = '!';
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT commandPrefix FROM Games WHERE server = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = server;
            Cmd.Parameters.Add(p1);
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
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
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
            }
            return prefix;
        }
        public static async Task SetPrefix(ulong server, char prefix)
        {
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE Games SET commandPrefix = ? WHERE server = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = prefix;
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter();
            p2.Value = server;
            Cmd.Parameters.Add(p2);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
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
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE UNObot.Players SET cards = ? WHERE userid = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = json;
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter();
            p2.Value = player;
            Cmd.Parameters.Add(p2);
            try
            {
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            return true;
        }
    }
}