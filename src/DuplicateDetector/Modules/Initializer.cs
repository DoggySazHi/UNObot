using System.IO;
using Discord;
using DuplicateDetector.Services;
using DuplicateDetector.Templates;
using Microsoft.Extensions.DependencyInjection;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;

namespace DuplicateDetector.Modules
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

            Name = "Duplicate Detector";
            Description = "A test program to detect duplicate images. For private use only.";
            Author = "DoggySazHi";
            Version = config != null ? config.Version : "Unknown Version";

            if (!configSuccess || config == null)
                return 1;

            Services = new ServiceCollection()
                .AddSingleton(config)
                .AddSingleton<IndexerService>()
                .AddSingleton<AIService>();

            return 0;
        }

        public int OnUnload(ILogger logger)
        {
            return 0;
        }
        
        private static bool LoadConfig(ILogger logger, out DuplicateDetectorConfig config)
        {
            var configPath = Path.Combine(PluginHelper.Directory(), "config.json");
            if (!File.Exists(configPath))
            {
                logger.Log(LogSeverity.Warning,
                    "Config doesn't exist! The file has been created, please edit all fields to be correct. Exiting.");
                new DuplicateDetectorConfig().Write(configPath);
                config = null;
                return false;
            }

            config = new DuplicateDetectorConfig(logger);
            return config.VerifyConfig();
        }
    }
}