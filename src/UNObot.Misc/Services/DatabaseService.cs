using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Discord;
using Newtonsoft.Json;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;
using UNObot.Plugins.Settings;

namespace UNObot.Misc.Services
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

        private class UNObotSettings
        {
            public UNObotSettings(string commandPrefix, bool hasDefaultChannel, decimal playChannel, bool enforceChannel, string allowedChannels)
            {
                CommandPrefix = commandPrefix;
                HasDefaultChannel = hasDefaultChannel;
                PlayChannel = Convert.ToUInt64(playChannel);
                EnforceChannel = enforceChannel;
                AllowedChannels = JsonConvert.DeserializeObject<ulong[]>(allowedChannels);
            }

            public string CommandPrefix { get; }
            public bool HasDefaultChannel { get; }
            public ulong PlayChannel { get; }
            public bool EnforceChannel { get; }
            public ulong[] AllowedChannels { get; }
        }
        
        public async Task Migrate()
        {
            const string fetchChannels = "SELECT server FROM UNObot.Games WHERE settings IS NULL";
            const string fetchUNObotInfo = "SELECT commandPrefix, hasDefaultChannel, playChannel, enforceChannel, allowedChannels FROM UNObot.Games WHERE server = @Server";
            const string fetchWebhooks = "SELECT channel FROM UNObot.Webhooks WHERE guild = @Server";
            const string fetchDDInfo = "SELECT channel FROM DuplicateDetector.Channels WHERE server = @Server";
            const string updateSettings = "UPDATE UNObot.Games SET settings = @Settings WHERE server = @Server";
            
            await using var db = _config.GetConnection();
            try
            {
                var result = await db.QueryAsync<ulong>(_config.ConvertSql(fetchChannels));
                foreach (var server in result)
                {
                    var unobot = await db.QueryFirstAsync<UNObotSettings>(_config.ConvertSql(fetchUNObotInfo),
                        new {Server = Convert.ToDecimal(server)});
                    var dd = await db.QueryAsync<ulong>(fetchDDInfo, new {Server = Convert.ToDecimal(server)});
                    var webhooks = await db.QueryAsync<ulong>(fetchWebhooks, new {Server = Convert.ToDecimal(server)});
                    var manager = CreateServerManager(server, unobot, dd, webhooks);
                    _logger.Log(LogSeverity.Debug, $"Server: {server}, embedded: {manager.Server}");
                    var json = JsonConvert.SerializeObject(manager,
                        new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All});
                    await db.ExecuteAsync(_config.ConvertSql(updateSettings),
                        new {Settings = json, Server = Convert.ToDecimal(server)});
                }
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
            }
        }

        private static SettingsManager CreateServerManager(ulong server, UNObotSettings unobot, IEnumerable<ulong> dd, IEnumerable<ulong> webhooks)
        {
            var manager = new SettingsManager(server);
            
            var unobotSettings = manager.CurrentSettings["UNObot"];
            unobotSettings.UpdateSetting("Prefix", new CodeBlock(unobot.CommandPrefix));
            unobotSettings.UpdateSetting("Enforce Channels", new Plugins.Settings.Boolean(unobot.EnforceChannel));
            unobotSettings.UpdateSetting("Channels Enforced", new ChannelIDList(unobot.AllowedChannels.Select(o => new ChannelID(o))));
            if (unobot.HasDefaultChannel)
                unobotSettings.UpdateSetting("Default Channel", new ChannelID(unobot.PlayChannel));

            var ddSettings = manager.CurrentSettings["DuplicateDetector"];
            ddSettings.UpdateSetting("Watch Channels", new ChannelIDList(dd.Select(o => new ChannelID(o))));

            var webhookSettings = manager.CurrentSettings["UNObot.Webhooks"];
            webhookSettings.UpdateSetting("Webhooks", new ChannelIDList(webhooks.Select(o => new ChannelID(o))));
            webhookSettings.UpdateSetting("Enable BitBucket", new Plugins.Settings.Boolean(true));
            webhookSettings.UpdateSetting("Enable OctoPrint", new Plugins.Settings.Boolean(true));
            
            return manager;
        }
    }
}