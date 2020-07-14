using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using UNObot.Plugins.Helpers;

namespace UNObot.Services
{
    internal class DatabaseService
    {
        private readonly LoggerService _logger;
        private readonly IConfiguration _config;

        internal string ConnString { get; private set; }

        public DatabaseService(LoggerService logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            GetConnectionString();
        }
        
        private void GetConnectionString()
        {
            ConnString = _config["connStr"];
            //ha, damn the limited encodings.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding.GetEncoding("windows-1254");
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
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, commandText, parameters.ToArray());
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
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
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
                var result = await MySqlHelper.ExecuteScalarAsync(ConnString, commandText, parameters.ToArray());
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
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
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
                var result = await MySqlHelper.ExecuteScalarAsync(ConnString, commandText, parameters.ToArray());
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
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, commandText, parameters.ToArray());
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
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, commandText, parameters.ToArray());
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
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
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, commandText, parameters.ToArray());
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
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, commandText, p1, p2, p3, p4);
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
                await MySqlHelper.ExecuteNonQueryAsync(ConnString, commandText, parameters.ToArray());
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
            await using var dr = await MySqlHelper.ExecuteReaderAsync(ConnString, commandText, parameters.ToArray());
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
                var result = await MySqlHelper.ExecuteScalarAsync(ConnString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    username = (string) result;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return username;
        }
    }
}