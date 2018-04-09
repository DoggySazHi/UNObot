﻿using System;
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
            conn.Close();
            Console.WriteLine("Successfully connected.");
        }
        public async Task AddUser(ulong id, string usrname)
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "INSERT INTO Players (userid, username, inGame, cards) VALUES(?, ?, 1, ?) ON DUPLICATE KEY UPDATE username = ?, inGame = 1, cards = ?";
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
                Value = usrname
            };
            Cmd.Parameters.Add(p4);
            MySqlParameter p5 = new MySqlParameter
            {
                Value = "[]"
            };
            Cmd.Parameters.Add(p5);
            try
            {
                conn.Open();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                conn.Close();
            }
        }
        public async Task RemoveUser(ulong id)
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "INSERT INTO Players (userid, inGame, cards) VALUES(?, 0, ?) ON DUPLICATE KEY UPDATE inGame = 0, cards = ?"
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
                conn.Open();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                conn.Close();
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
                DiscordBot.Program.version = (string)jObject["version"];
                Console.WriteLine($"Running {DiscordBot.Program.version}!");
            }
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SET SQL_SAFE_UPDATES = 0; UPDATE UNObot.Players SET cards = ?, inGame = 0, gameName = null; SET SQL_SAFE_UPDATES = 1;";
            MySqlParameter p1 = new MySqlParameter();
            JArray empty = new JArray();
            p1.Value = empty.ToString();
            Cmd.Parameters.Add(p1);
            try
            {
                conn.Open();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                conn.Close();
            }
        }

        public async Task AddUpdateGuilds(string Guild, ushort ingame)
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "INSERT INTO Games (server, inGame) VALUES(?, 0) ON DUPLICATE KEY UPDATE inGame = ?";
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
            try
            {
                conn.Open();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                conn.Close();
            }
            
        }
        public async Task<List<ulong>> GetPlayers()
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT userid,username FROM UNObot.Players WHERE inGame = 1";
            List<ulong> players = new List<ulong>();
            conn.Open();
            using (MySqlDataReader dr = (MySqlDataReader)await Cmd.ExecuteReaderAsync())
            {
                try
                {
                    while (dr.Read())
                    {
                        players.Add(dr.GetUInt64(0));
                        await dr.NextResultAsync();
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"A MySQL error has been caught, Error {ex}");
                }
                finally
                {
                    conn.Close();
                }
                return players;
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
            conn.Open();
            using (MySqlDataReader dr = (MySqlDataReader) await Cmd.ExecuteReaderAsync())
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
                    conn.Close();
                }
                return yesorno;
            }
        }
        //Done?
        public async Task<List<DiscordBot.Modules.Card>> GetCards(ulong player)
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
            List<DiscordBot.Modules.Card> cards = new List<DiscordBot.Modules.Card>();
            conn.Open();
            using (MySqlDataReader dr = (MySqlDataReader) await Cmd.ExecuteReaderAsync())
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
                    conn.Close();
                }
                cards = JsonConvert.DeserializeObject<List<DiscordBot.Modules.Card>>(jsonstring);
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
            conn.Open();
            using (MySqlDataReader dr = (MySqlDataReader) await Cmd.ExecuteReaderAsync())
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
                    conn.Close();
                }
                return exists;
            }
        }
        public async Task StarterCard()
        {
            List<ulong> players = await GetPlayers();
            foreach (ulong player in players)
            {
                for(int i = 0; i < 7; i++)
                {
                    await AddCard(player, DiscordBot.Modules.CoreCommands.UNOcore.RandomCard());
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
            int[] stats = new int[3];
            conn.Open();
            //TODO replace all cases
            using (MySqlDataReader dr = (MySqlDataReader) await Cmd.ExecuteReaderAsync())
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
                    conn.Close();
                }
                return stats;
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
            switch(mode)
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
                conn.Open();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                conn.Close();
            }
         }
        public async Task AddCard(ulong player, DiscordBot.Modules.Card card)
        {
            if (conn == null)
                GetConnectionString();
                
            List<DiscordBot.Modules.Card> cards = await GetCards(player);
            if (cards == null)
                cards = new List<DiscordBot.Modules.Card>();
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
                conn.Open();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                conn.Close();
            }
        }
        public async Task<bool> RemoveCard(ulong player, DiscordBot.Modules.Card card)
        {
            bool foundCard = false;
            if (conn == null)
                GetConnectionString();

            List<DiscordBot.Modules.Card> cards = await GetCards(player);
            int currentPlace = 0;
            foreach (DiscordBot.Modules.Card cardindeck in cards){
                if(card.Equals(cardindeck)){
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
                conn.Open();
                await Cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"A MySQL error has been caught, Error {ex}");
            }
            finally
            {
                conn.Close();
            }
            return true;
        }
    }
}