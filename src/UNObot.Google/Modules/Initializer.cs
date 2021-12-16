using System.IO;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using UNObot.Google.Services;
using UNObot.Google.Templates;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;

namespace UNObot.Google.Modules;

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
            
        Name = "UNObot-Google";
        Description = "Thanks Benzo, very cool!";
        Author = "DoggySazHi";
        Version = config != null ? config.Version : "Unknown Version";

        if (!configSuccess || config == null)
            return 1;

        Services = new ServiceCollection()
            .AddSingleton(config)
            .AddSingleton<GoogleSearchService>();
            
        return 0;
    }

    public int OnUnload(ILogger logger)
    {
        return 0;
    }
        
    private static bool LoadConfig(ILogger logger, out GoogleConfig config)
    {
        var configPath = Path.Combine(PluginHelper.Directory(), "config.json");
        if (!File.Exists(configPath))
        {
            logger.Log(LogSeverity.Warning,
                "Google Config doesn't exist! The file has been created, please edit all fields to be correct. Exiting.");
            new GoogleConfig().Write(configPath);
            config = null;
            return false;
        }

        config = new GoogleConfig(logger, configPath);
        return config.VerifyConfig();
    }
}