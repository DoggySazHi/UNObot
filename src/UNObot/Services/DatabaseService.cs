using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Discord;
using Newtonsoft.Json;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;
using UNObot.Plugins.Settings;

namespace UNObot.Services
{
    public class DatabaseService
    {
        private readonly IUNObotConfig _config;
        private readonly ILogger _logger;

        public DatabaseService(IUNObotConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SetDefaultChannel(ulong server, ulong channel)
        {
            const string commandText = "UPDATE UNObot.Games SET playChannel = @Channel WHERE server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { Channel = Convert.ToDecimal(channel), Server = Convert.ToDecimal(server) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task SetHasDefaultChannel(ulong server, bool hasDefault)
        {
            const string commandText = "UPDATE UNObot.Games SET hasDefaultChannel = @HasDefaultChannel WHERE server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new {HasDefaultChannel = hasDefault, Server = Convert.ToDecimal(server)});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task<bool> ChannelEnforced(ulong server)
        {
            const string commandText = "SELECT enforceChannel FROM UNObot.Games WHERE server = @Server";
            var result = false;

            await using var db = _config.GetConnection();
            try
            {
                result = await db.ExecuteScalarAsync<bool>(_config.ConvertSql(commandText), new {Server = Convert.ToDecimal(server)});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return result;
        }

        public async Task SetEnforceChannel(ulong server, bool enforce)
        {
            const string commandText = "UPDATE UNObot.Games SET enforceChannel = @Enforce WHERE server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new {Enforce = enforce, Server = Convert.ToDecimal(server)});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task<List<ulong>> GetAllowedChannels(ulong server)
        {
            const string commandText = "SELECT allowedChannels FROM UNObot.Games WHERE server = @Server";

            var allowedChannels = new List<ulong>();

            await using var db = _config.GetConnection();
            try
            {
                var result = await db.ExecuteScalarAsync(_config.ConvertSql(commandText), new {Server = Convert.ToDecimal(server)});
                if (result.HasDBValue())
                    allowedChannels = JsonConvert.DeserializeObject<List<ulong>>((string) result);
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return allowedChannels;
        }

        public async Task SetAllowedChannels(ulong server, List<ulong> allowedChannels)
        {
            const string commandText = "UPDATE UNObot.Games SET allowedChannels = @AllowedChannels WHERE server = @Server";
            var json = JsonConvert.SerializeObject(allowedChannels);

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new {Server = Convert.ToDecimal(server), AllowedChannels = json});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task RegisterUser(ulong id, string username)
        {
            const string commandText =
                "INSERT INTO UNObot.Players (userid, username) VALUES(@UserID, @Username) ON DUPLICATE KEY UPDATE username = @Username";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new {UserID = Convert.ToDecimal(id), Username = username});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task RegisterServer(ulong server)
        {
            const string commandText = "INSERT IGNORE INTO UNObot.Games (server) VALUES(@Server)";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new {Server = Convert.ToDecimal(server)});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task AddWebhook(string key, ulong guild, ulong channel, string type = "bitbucket")
        {
            const string commandText =
                "INSERT INTO UNObot.Webhooks (webhookKey, channel, guild, type) VALUES (@Key, @Channel, @Guild, @Type)";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText),
                    new {Key = key[..50], Channel = Convert.ToDecimal(channel), Guild = Convert.ToDecimal(guild), Type = type});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task DeleteWebhook(string key)
        {
            const string commandText = "DELETE FROM UNObot.Webhooks WHERE webhookKey = @Key";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new {Key = key.Substring(0, Math.Min(key.Length, 50))});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task<(ulong Guild, byte Type)> GetWebhook(ulong channel, string key)
        {
            const string commandText =
                "SELECT guild, type FROM UNObot.Webhooks WHERE channel = @Channel AND webhookKey = @Key";
            ulong guild = 0;
            byte type = 0;

            await using var db = _config.GetConnection();
            try
            {
                var result = await db.QueryFirstOrDefaultAsync(_config.ConvertSql(commandText),
                    new {Channel = Convert.ToDecimal(channel), Key = key[..Math.Min(key.Length, 50)]});
                guild = Convert.ToUInt64(result.guild);
                type = result.type;
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return (guild, type);
        }

        public async Task<string> GetPrefix(ulong server)
        {
            const string commandText = "SELECT commandPrefix FROM UNObot.Games WHERE server = @Server";
            var prefix = ".";

            await using var db = _config.GetConnection();
            try
            {
                prefix = await db.ExecuteScalarAsync<string>(_config.ConvertSql(commandText), new {Server = Convert.ToDecimal(server)});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return prefix;
        }

        public async Task SetPrefix(ulong server, string prefix)
        {
            const string commandText = "UPDATE UNObot.Games SET commandPrefix = @Prefix WHERE server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new {Prefix = prefix, Server = Convert.ToDecimal(server)});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }

        public async Task<SettingsManager> GetSettings(ulong server)
        {
            const string commandText = "SELECT settings FROM UNObot.Games WHERE server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                var result = await db.ExecuteScalarAsync<string>(_config.ConvertSql(commandText), new {Server = Convert.ToDecimal(server)});
                if (result.HasDBValue())
                    return JsonConvert.DeserializeObject<SettingsManager>(result);
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
            catch (JsonSerializationException)
            {
                // ¯\_(ツ)_/¯ Deal with it! (Recreate the settings manager)
            }

            return new SettingsManager();
        }

        public async Task SetSettings(ulong server, ulong settings)
        {
            const string commandText = "UPDATE UNObot.Games SET settings = @Settings WHERE server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { Settings = settings, Server = Convert.ToDecimal(server)});
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
    }
}