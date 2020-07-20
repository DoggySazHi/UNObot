using Microsoft.Extensions.DependencyInjection;
using UNObot.Plugins;
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

        public int OnLoad()
        {
            Name = "ConnectBot";
            Description = "Forgotten since 2018‑07‑22. UNObot was born on 2018‑03‑04.";
            Author = "DoggySazHi";
            Version = "1.0.9 (4.2.0)";

            Services = new ServiceCollection()
                .AddSingleton<QueryHandlerService>()
                .AddSingleton<MinecraftProcessorService>()
                .AddSingleton<UBOWServerLoggerService>()
                .AddSingleton<RCONManager>()
                .AddSingleton<DatabaseService>()
                .AddSingleton<EmbedService>();

            return 0;
        }

        public int OnUnload()
        {
            return 0;
        }
    }
}