using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UNObot.Modules
{
    public class UNOdb
    {
        MySqlConnection conn;
        public void GetConnectionString()
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
                conn = new MySqlConnection((string)jObject["connStr"]);
            }
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                //ha, damn the limited encodings.
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Encoding.GetEncoding("windows-1254");
                conn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MySQL has encountered an error: {ex}");
            }
            conn.CloseAsync();
            Console.WriteLine("Successfully connected.");
        }
        public async Task AddGame(ulong server)
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "INSERT IGNORE INTO Games (server) VALUES(?)"
            };
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            try
            {
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task ResetGame(ulong server)
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "UPDATE Games SET inGame = 0, currentCard = ?, `order` = 1, oneCardLeft = 0, queue = ? WHERE server = ?"
            };
            AFKtimer afkTimer = new AFKtimer();
            //suspisous code
            afkTimer.DeleteTimer(server);
            MySqlParameter p1 = new MySqlParameter()
            {
                Value = "[]"
            };
            Cmd.Parameters.Add(p1);
            MySqlParameter p2 = new MySqlParameter()
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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task<bool> IsServerInGame(ulong server)
        {
            if (conn == null)
                GetConnectionString();
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
            await conn.OpenAsync();
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        yesorno |= dr.GetByte(0) == 1;
                        await dr.NextResultAsync();
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                finally
                {
                    await conn.CloseAsync();
                }
                return yesorno;
            }
        }
        public async Task AddUser(ulong id, string usrname, ulong server)
        {
            if (conn == null)
                GetConnectionString();
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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task AddUser(ulong id, string usrname)
        {
            if (conn == null)
                GetConnectionString();
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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task RemoveUser(ulong id)
        {
            if (conn == null)
                GetConnectionString();
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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task CleanAll()
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
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "SET SQL_SAFE_UPDATES = 0; UPDATE UNObot.Players SET cards = ?, inGame = 0, server = null, gameName = null; UPDATE Games SET inGame = 0, currentCard = ?, `order` = 1, oneCardLeft = 0, queue = ?; SET SQL_SAFE_UPDATES = 1;"
            };
            JArray empty = new JArray();
            MySqlParameter p1 = new MySqlParameter()
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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        public async Task AddGuild(ulong Guild, ushort ingame)
        => await AddGuild(Guild, ingame, 1);
        public async Task AddGuild(ulong Guild, ushort ingame, ushort gamemode)
        {
            /* 
             * 1 - In a regular game.
             * 2 - In a game that prevents seeing other players' cards.
             * 3 (maybe?) - Allows skipping of a turn after drawing 2 cards.
            */
            if (conn == null)
                GetConnectionString();
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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }

        }
        public async Task<ushort> GetGamemode(ulong server)
        {
            if (conn == null)
                GetConnectionString();
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
            await conn.OpenAsync();
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
                finally
                {
                    await conn.CloseAsync();
                }
                return gamemode;
            }
        }
        //NOTE THAT THIS GETS DIRECTLY FROM SERVER; YOU MUST AddPlayersToServer
        public async Task<Queue<ulong>> GetPlayers(ulong server)
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT queue FROM Games WHERE inGame = 1 AND server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            Queue<ulong> players = new Queue<ulong>();
            await conn.OpenAsync();
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        players = JsonConvert.DeserializeObject<Queue<ulong>>(dr.GetString(0));
                        await dr.NextResultAsync();
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
            return players;
        }
        public async Task<ulong> GetUserServer(ulong player)
        {
            if (conn == null)
                GetConnectionString();
            ulong server = 0;
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT server FROM Players WHERE inGame = 1";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = player
            };
            Cmd.Parameters.Add(p1);
            await conn.OpenAsync();
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
                finally
                {
                    await conn.CloseAsync();
                }
            }
            return server;
        }
        public async Task SetPlayers(ulong server, Queue<ulong> players)
        {
            string json = JsonConvert.SerializeObject(players);
            if (conn == null)
                GetConnectionString();
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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task<Queue<ulong>> GetUsersWithServer(ulong server)
        {
            Queue<ulong> players = new Queue<ulong>();
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT userid FROM Players WHERE inGame = 1 AND server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            await conn.OpenAsync();
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
                finally
                {
                    await conn.CloseAsync();
                }
                return players;
            }
        }

        public async Task<ulong> GetUNOPlayer(ulong server)
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT oneCardLeft FROM Games WHERE inGame = 1 AND server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            ulong player = 0;
            await conn.OpenAsync();
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        if (!await dr.IsDBNullAsync(0))
                            player = dr.GetUInt64(0);
                        await dr.NextResultAsync();
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                finally
                {
                    await conn.CloseAsync();
                }
                return player;
            }
        }
        public async Task SetUNOPlayer(ulong server, ulong player)
        {
            if (conn == null)
                GetConnectionString();
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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task SetDefaultChannel(ulong server, ulong channel)
        {
            if (conn == null)
                GetConnectionString();
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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task SetHasDefaultChannel(ulong server, bool hasDefault)
        {
            if (conn == null)
                GetConnectionString();
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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task<bool> HasDefaultChannel(ulong server)
        {
            if (conn == null)
                GetConnectionString();
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
            await conn.OpenAsync();
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
                finally
                {
                    await conn.CloseAsync();
                }
                return yesorno;
            }
        }
        public async Task<bool> EnforceChannel(ulong server)
        {
            if (conn == null)
                GetConnectionString();
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
            await conn.OpenAsync();
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
                finally
                {
                    await conn.CloseAsync();
                }
                return yesorno;
            }
        }
        public async Task SetEnforceChannel(ulong server, bool enforce)
        {
            if (conn == null)
                GetConnectionString();
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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task<ulong> GetDefaultChannel(ulong server)
        {
            if (conn == null)
                GetConnectionString();
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
            await conn.OpenAsync();
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        channel = dr.GetUInt64(0);
                        await dr.NextResultAsync();
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                finally
                {
                    await conn.CloseAsync();
                }
                return channel;
            }
        }
        public async Task<List<ulong>> GetAllowedChannels(ulong server)
        {
            if (conn == null)
                GetConnectionString();
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
            await conn.OpenAsync();
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
                finally
                {
                    await conn.CloseAsync();
                }
                return allowedChannels;
            }
        }
        public async Task SetAllowedChannels(ulong server, List<ulong> allowedChannels)
        {
            if (conn == null)
                GetConnectionString();
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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task<Card> GetCurrentCard(ulong server)
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT currentCard FROM Games WHERE inGame = 1 AND server = ?";
            MySqlParameter p1 = new MySqlParameter
            {
                Value = server
            };
            Cmd.Parameters.Add(p1);
            Card card = new Card();
            await conn.OpenAsync();
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        card = JsonConvert.DeserializeObject<Card>(dr.GetString(0));
                        await dr.NextResultAsync();
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                finally
                {
                    await conn.CloseAsync();
                }
                return card;
            }
        }
        public async Task SetCurrentCard(ulong server, Card card)
        {
            string cardJSON = JsonConvert.SerializeObject(card);
            if (conn == null)
                GetConnectionString();
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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        public async Task<bool> IsPlayerInGame(ulong player)
        {
            if (conn == null)
                GetConnectionString();
            bool yesorno = false;
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT inGame FROM UNObot.Players WHERE userid = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = player;
            Cmd.Parameters.Add(p1);
            await conn.OpenAsync();
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        if (dr.GetByte(0) == 1)
                            yesorno = true;
                        await dr.NextResultAsync();
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                finally
                {
                    await conn.CloseAsync();
                }
                return yesorno;
            }
        }

        public async Task<bool> IsPlayerInServerGame(ulong player, ulong server)
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT queue FROM UNObot.Games WHERE server = ?";
            Queue<ulong> players = new Queue<ulong>();
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = server;
            Cmd.Parameters.Add(p1);
            await conn.OpenAsync();
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        players = JsonConvert.DeserializeObject<Queue<ulong>>(dr.GetString(0));
                        await dr.NextResultAsync();
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                finally
                {
                    await conn.CloseAsync();
                }
                if (players.Contains(player))
                    return true;
                else
                    return false;
            }
        }
        //Done?
        public async Task<List<Card>> GetCards(ulong player)
        {
            if (conn == null)
                GetConnectionString();
            string jsonstring = "";
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT cards FROM UNObot.Players WHERE userid = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = player;
            Cmd.Parameters.Add(p1);
            List<Card> cards = new List<Card>();
            await conn.OpenAsync();
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        jsonstring = dr.GetString(0);
                        await dr.NextResultAsync();
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                finally
                {
                    await conn.CloseAsync();
                }
                cards = JsonConvert.DeserializeObject<List<Card>>(jsonstring);
                return cards;
            }
        }
        public async Task<bool> UserExists(ulong player)
        {
            if (conn == null)
                GetConnectionString();
            bool exists = false;
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM UNObot.Players WHERE userid = ?)";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = player;
            Cmd.Parameters.Add(p1);
            await conn.OpenAsync();
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        if (dr.GetInt64(0) == 1)
                            exists = true;
                        await dr.NextResultAsync();
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                finally
                {
                    await conn.CloseAsync();
                }
                return exists;
            }
        }
        public async Task GetUsersAndAdd(ulong server)
        {
            if (conn == null)
                GetConnectionString();
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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                ColorConsole.WriteLine($"A MySQL error has been caught, Error {ex}", ConsoleColor.Red);
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task StarterCard(ulong server)
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
        public async Task<int[]> GetStats(ulong player)
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT gamesJoined,gamesPlayed,gamesWon FROM UNObot.Players WHERE userid = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = player;
            Cmd.Parameters.Add(p1);
            int[] stats = { 0, 0, 0 };
            await conn.OpenAsync();
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        stats[0] = dr.GetInt32(0);
                        stats[1] = dr.GetInt32(1);
                        stats[2] = dr.GetInt32(2);
                        await dr.NextResultAsync();
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                finally
                {
                    await conn.CloseAsync();
                }
                return stats;
            }
        }
        public async Task<string> GetNote(ulong player)
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT note FROM UNObot.Players WHERE userid = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = player;
            Cmd.Parameters.Add(p1);
            await conn.OpenAsync();
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
                finally
                {
                    await conn.CloseAsync();
                }
            }
            return message;
        }
        public async Task SetNote(ulong player, string note)
        {
            if (conn == null)
                GetConnectionString();

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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task RemoveNote(ulong player)
        {
            if (conn == null)
                GetConnectionString();

            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "UPDATE UNObot.Players SET note = NULL WHERE userid = ?";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = player;
            Cmd.Parameters.Add(p1);
            try
            {
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task UpdateStats(ulong player, int mode)
        {
            //1 is gamesJoined
            //2 is gamesPlayed
            //3 is gamesWon
            if (conn == null)
                GetConnectionString();

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
            }

            MySqlParameter p1 = new MySqlParameter();
            p1.Value = player;
            Cmd.Parameters.Add(p1);
            try
            {
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task AddCard(ulong player, Card card)
        {
            if (conn == null)
                GetConnectionString();

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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task SetCards(ulong player, List<Card> cards)
        {
            if (conn == null)
                GetConnectionString();

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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task<char> GetPrefix(ulong server)
        {
            if (conn == null)
                GetConnectionString();

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
                finally
                {
                    await conn.CloseAsync();
                }
            }
            return prefix;
        }
        public async Task SetPrefix(ulong server, char prefix)
        {
            if (conn == null)
                GetConnectionString();

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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        public async Task<bool> RemoveCard(ulong player, Card card)
        {
            bool foundCard = false;
            if (conn == null)
                GetConnectionString();

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
                await conn.OpenAsync();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                await conn.CloseAsync();
            }
            return true;
        }
    }
}