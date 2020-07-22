using DuplicateDetector.Services;
using DuplicateDetector.Templates;
using Microsoft.Extensions.DependencyInjection;
using UNObot.Plugins;

namespace DuplicateDetector.Modules
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
            Name = "Duplicate Detector";
            Description = "A test program to detect duplicate images. For private use only.";
            Author = "DoggySazHi";
            Version = "0.1.1 (4.2.0)";

            Services = new ServiceCollection()
                .AddSingleton(new AIConfig().Build())
                .AddSingleton<AIService>()
                .AddSingleton<IndexerService>();

            return 0;
        }

        public int OnUnload()
        {
            return 0;
        }
    }
}