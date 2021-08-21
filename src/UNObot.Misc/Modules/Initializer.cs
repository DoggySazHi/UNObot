using Discord;
using Microsoft.Extensions.DependencyInjection;
using UNObot.Misc.Services;
using UNObot.Plugins;

namespace UNObot.Misc.Modules
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
            Name = "UNObot-Misc";
            Description = "Random utilities for testing.";
            Author = "DoggySazHi";
            Version = "N/A";

            logger.Log(LogSeverity.Debug, "Misc package loaded.");
            Services = new ServiceCollection().AddTransient<DatabaseService>();
            
            return 0;
        }

        public int OnUnload(ILogger logger)
        {
            return 0;
        }
    }
}