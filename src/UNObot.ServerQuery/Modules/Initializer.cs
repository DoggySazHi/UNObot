using Microsoft.Extensions.DependencyInjection;
using UNObot.Plugins;
using UNObot.ServerQuery.Queries;
using UNObot.ServerQuery.Services;

namespace UNObot.ServerQuery.Modules
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
            Name = "UNObot-ServerQuery";
            Description = "Utilities for probing game servers.";
            Author = "DoggySazHi";
            Version = "1.3.0 (4.3 Beta 8)";

            Services = new ServiceCollection()
                .AddSingleton<QueryHandlerService>()
                .AddSingleton<MinecraftProcessorService>()
                .AddSingleton<UBOWServerLoggerService>()
                .AddSingleton<RCONManager>()
                .AddSingleton<DatabaseService>()
                .AddSingleton<EmbedService>();

            return 0;
        }

        public int OnUnload(ILogger logger)
        {
            return 0;
        }
    }
}