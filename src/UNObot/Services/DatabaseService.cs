using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Discord;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;

namespace UNObot.Services
{
    internal class DatabaseService
    {
        private readonly ILogger _logger;

        internal string ConnString { get; }

        public DatabaseService(ILogger logger, IConfiguration config)
        {
            _logger = logger;
            ConnString = config.GetConnectionString();
        }

        internal async Task SetDefaultChannel(ulong server, ulong channel)
        {
            const string commandText = "UPDATE Games SET playChannel = @Channel WHERE server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new { });
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task SetHasDefaultChannel(ulong server, bool hasDefault)
        {
            const string commandText = "UPDATE Games SET hasDefaultChannel = @HasDefaultChannel WHERE server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {HasDefaultChannel = hasDefault, Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<bool> ChannelEnforced(ulong server)
        {
            const string commandText = "SELECT enforceChannel FROM UNObot.Games WHERE server = @Server";
            var result = false;

            await using var db = new MySqlConnection(ConnString);
            try
            {
                result = await db.ExecuteScalarAsync<bool>(commandText, new {Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return result;
        }

        internal async Task SetEnforceChannel(ulong server, bool enforce)
        {
            const string commandText = "UPDATE Games SET enforceChannel = @Enforce WHERE server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Enforce = enforce, Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<List<ulong>> GetAllowedChannels(ulong server)
        {
            const string commandText = "SELECT allowedChannels FROM UNObot.Games WHERE server = @Server";

            var allowedChannels = new List<ulong>();

            await using var db = new MySqlConnection(ConnString);
            try
            {
                var result = await db.ExecuteScalarAsync(commandText, new {Server = server});
                if (result.HasDBValue())
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
            const string commandText = "UPDATE Games SET allowedChannels = @AllowedChannels WHERE server = @Server";
            var json = JsonConvert.SerializeObject(allowedChannels);

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Server = server, AllowedChannels = json});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task AddUser(ulong id, string username)
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

        internal async Task AddGame(ulong server)
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

        internal async Task AddWebhook(string key, ulong guild, ulong channel, string type = "bitbucket")
        {
            const string commandText =
                "INSERT INTO UNObot.Webhooks (webhookKey, channel, guild, type) VALUES (@Key, @Channel, @Guild, @Type)";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText,
                    new {Key = key.Substring(0, 50), Channel = channel, Guild = guild, Type = type});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task DeleteWebhook(string key)
        {
            const string commandText = "DELETE FROM UNObot.Webhooks WHERE webhookKey = @Key";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Key = key.Substring(0, Math.Min(key.Length, 50))});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        internal async Task<(ulong Guild, byte Type)> GetWebhook(ulong channel, string key)
        {
            const string commandText =
                "SELECT guild, type FROM UNObot.Webhooks WHERE channel = @Channel AND webhookKey = @Key";
            ulong guild = 0;
            byte type = 0;

            await using var db = new MySqlConnection(ConnString);
            try
            {
                var result = await db.QueryFirstOrDefaultAsync(commandText,
                    new {Channel = channel, Key = key.Substring(0, Math.Min(key.Length, 50))});
                guild = result[0];
                type = result[1];
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return (guild, type);
        }

        internal async Task<string> GetPrefix(ulong server)
        {
            const string commandText = "SELECT commandPrefix FROM UNObot.Games WHERE server = @Server";
            var prefix = ".";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                prefix = await db.ExecuteScalarAsync<string>(commandText, new {Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return prefix;
        }

        internal async Task SetPrefix(ulong server, string prefix)
        {
            const string commandText = "UPDATE UNObot.Games SET commandPrefix = @Prefix WHERE server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {Prefix = prefix, Server = server});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
    }
}