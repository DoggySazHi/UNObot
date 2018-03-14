using System;
using System.Collections.Generic;
using System.IO;
using MySql.Data.MySqlClient;
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
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = id;
            Cmd.Parameters.Add(p1);

            MySqlParameter p2 = new MySqlParameter();
            p2.Value = usrname;
            Cmd.Parameters.Add(p2);
            //for third parameter
            MySqlParameter p3 = new MySqlParameter();
            p3.Value = usrname;
            Cmd.Parameters.Add(p3);
            try
            {
                conn.Open();
                Cmd.ExecuteNonQueryAsync();
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
        public void RemoveUser(ulong id, string usrname)
        {
            if (conn == null)
                GetConnectionString();
            MySqlCommand Cmd = new MySqlCommand();
            Cmd.Connection = conn;
            Cmd.CommandText = "INSERT INTO Players (userid, username, inGame) VALUES(?, ?, 0) ON DUPLICATE KEY UPDATE username = ?, inGame = 0";
            MySqlParameter p1 = new MySqlParameter();
            p1.Value = id;
            Cmd.Parameters.Add(p1);

            MySqlParameter p2 = new MySqlParameter();
            p2.Value = usrname;
            Cmd.Parameters.Add(p2);
            //for third parameter
            MySqlParameter p3 = new MySqlParameter();
            p2.Value = usrname;
            Cmd.Parameters.Add(p3);
            try
            {
                conn.Open();
                Cmd.ExecuteNonQueryAsync();
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
            Cmd.CommandText = "SELECT * FROM UNObot.Players WHERE inGame = 1";
            List<ulong> players = new List<ulong>();
            using (MySqlDataReader dr = Cmd.ExecuteReader())
            {
                try
                {
                    conn.Open();
                    Cmd.ExecuteNonQueryAsync();

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
        public void add7cards()
        {
            //Yes
        }
    }
}