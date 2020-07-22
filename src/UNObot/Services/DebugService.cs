using System;
using Discord;
using Microsoft.Extensions.Configuration;
using UNObot.Plugins;

namespace UNObot.Services
{
    internal class DebugService
    {
        public DebugService(IConfiguration config, ILogger logger)
        {
            if (!config["version"].Contains("Debug", StringComparison.OrdinalIgnoreCase))
                return;
            logger.Log(LogSeverity.Debug, "Logger activated!");
        }
    }
}