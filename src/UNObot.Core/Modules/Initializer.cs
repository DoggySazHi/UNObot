using Microsoft.Extensions.DependencyInjection;
using UNObot.Core.Services;
using UNObot.Plugins;

namespace UNObot.Core.Modules
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
            Name = "UNObot-Core";
            Description = "The core UNO commands the bot is not known for.";
            Author = "DoggySazHi";
            Version = "1.0.3 (4.2.9)";
            
            Services = new ServiceCollection()
                .AddSingleton<AFKTimerService>()
                .AddSingleton<EmbedService>()
                .AddSingleton<InputHandlerService>()
                .AddSingleton<QueueHandlerService>()
                .AddSingleton<DatabaseService>()
                .AddSingleton<UNOPlayCardService>();
            
            return 0;
        }

        public int OnUnload()
        {
            return 0;
        }
    }
}