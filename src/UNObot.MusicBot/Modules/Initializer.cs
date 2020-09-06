using Microsoft.Extensions.DependencyInjection;
using UNObot.Plugins;
using UNObot.MusicBot.Services;

namespace UNObot.MusicBot.Modules
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
            Name = "UNObot-MusicBot";
            Description = "The music bot add-on for UNObot. Probably doesn't work.";
            Author = "DoggySazHi";
            Version = "1.1.2 (4.2.8)";
            
            Services = new ServiceCollection()
                .AddSingleton<EmbedService>()
                .AddSingleton<MusicBotService>()
                .AddSingleton<YoutubeService>();

            return 0;
        }

        public int OnUnload()
        {
            return 0;
        }
    }
}