using System.IO;
using ConnectBot.Services;
using ConnectBot.Templates;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;

namespace ConnectBot.Modules
{
    public class Initializer : IPlugin
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Author { get; private set; }
        public string Version { get; private set; }
        public IServiceCollection Services { get; private set; }

        public int OnLoad(ILogger logger)
        {
            var configSuccess = LoadConfig(logger, out var config);
            
            Name = "ConnectBot";
            Description = "Forgotten since 2018‑07‑22. UNObot was born on 2018‑03‑04.";
            Author = "DoggySazHi";
            Version = config != null ? config.Version : "Unknown Version";

            if (!configSuccess || config == null)
                return 1;

            Services = new ServiceCollection()
                .AddSingleton(config)
                .AddSingleton<AFKTimerService>()
                .AddSingleton<DatabaseService>()
                .AddSingleton<GameService>()
                .AddSingleton<ConfigService>()
                .AddSingleton<ButtonHandler>();
            return 0;
        }

        public int OnUnload(ILogger logger)
        {
            return 0;
        }

        private static bool LoadConfig(ILogger logger, out ConnectBotConfig config)
        {
            var configPath = Path.Combine(PluginHelper.Directory(), "config.json");
            if (!File.Exists(configPath))
            {
                logger.Log(LogSeverity.Warning,
                    "Config doesn't exist! The file has been created, please edit all fields to be correct. Exiting.");
                new ConnectBotConfig().Write(configPath);
                config = null;
                return false;
            }

            config = new ConnectBotConfig(logger);
            return config.VerifyConfig();
        }
    }
}