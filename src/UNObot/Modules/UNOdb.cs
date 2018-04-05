using System;
using System.Collections.Generic;
using System.IO;
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
                conn.Open();
                // Perform database operations
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MySQL has encountered an error: {ex}");
            }
            conn.Close();
            Console.WriteLine("Successfully connected.");
        }
        public void AddUser(ulong id, string usrname)
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "INSERT INTO Players (userid, username, inGame) VALUES(?, ?, 1) ON DUPLICATE KEY UPDATE username = ?, inGame = 1";
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
            //for third parameter
            MySqlParameter p3 = new MySqlParameter
            {
                Value = usrname
            };
            Cmd.Parameters.Add(p3);
            try
            {
                conn.Open();
                Cmd.ExecuteNonQuery();
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
        public void RemoveUser(ulong id)
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = "INSERT INTO Players (userid, inGame, cards) VALUES(?, 0) ON DUPLICATE KEY UPDATE inGame = 0, cards = ?"
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
            try
            {
                conn.Open();
                Cmd.ExecuteNonQuery();
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
        public void CleanAll()
        {
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
                Cmd.ExecuteNonQuery();
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
        public List<ulong> GetPlayers()
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "SELECT userid,username FROM UNObot.Players WHERE inGame = 1";
            List<ulong> players = new List<ulong>();
            conn.Open();
            using (MySqlDataReader dr = Cmd.ExecuteReader())
            {
                try
                {
                    while (dr.Read())
                    {
                        players.Add(dr.GetUInt64(0));
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
        public bool IsPlayerInGame(ulong player)
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
            using (MySqlDataReader dr = Cmd.ExecuteReader())
            {
                try
                {
                    while (dr.Read())
                    {
                        if (dr.GetByte(0) == 1)
                            yesorno = true;
                        dr.NextResult();
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
        public List<DiscordBot.Modules.Card> GetCards(ulong player)
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
            using (MySqlDataReader dr = Cmd.ExecuteReader())
            {
                try
                {
                    while (dr.Read())
                    {
                        jsonstring = dr.GetString(0);
                        Console.WriteLine(jsonstring);
                        dr.NextResult();
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
        public void StarterCard()
        {
            List<ulong> players = GetPlayers();
            foreach (ulong player in players)
            {
                for(int i = 0; i < 7; i++)
                {
                    AddCard(player, DiscordBot.Modules.CoreCommands.UNOcore.RandomCard());
                }
            }
        }
        //TODO this
        public void AddCard(ulong player, DiscordBot.Modules.Card card)
        {
            if (conn == null)
                GetConnectionString();
                
            List<DiscordBot.Modules.Card> cards = GetCards(player);
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
                Cmd.ExecuteNonQuery();
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
        public bool RemoveCard(ulong player, DiscordBot.Modules.Card card)
        {
            bool foundCard = false;
            if (conn == null)
                GetConnectionString();

            List<DiscordBot.Modules.Card> cards = GetCards(player);
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
                Cmd.ExecuteNonQuery();
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