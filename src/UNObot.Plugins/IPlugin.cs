﻿using Microsoft.Extensions.DependencyInjection;

namespace UNObot.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        string Author { get; }
        string Version { get; }
        IServiceCollection Services { get; }

        int OnLoad();
        int OnUnload();

        public string GetName()
        {
            return $"{Name} ({Version})";
        }
    }
}