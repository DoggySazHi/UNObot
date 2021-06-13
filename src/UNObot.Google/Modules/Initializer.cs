using Microsoft.Extensions.DependencyInjection;
using UNObot.Google.Services;
using UNObot.Plugins;

namespace UNObot.Google.Modules
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
            Name = "UNObot-Google";
            Description = "Thanks Benzo, very cool!";
            Author = "DoggySazHi";
            Version = "0.0.5 (4.2.10)";

            Services = new ServiceCollection()
                .AddSingleton<GoogleSearchService>();
            
            return 0;
        }

        public int OnUnload(ILogger logger)
        {
            return 0;
        }
    }
}