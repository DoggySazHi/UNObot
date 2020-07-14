using System;
using Discord;
using Microsoft.Extensions.Configuration;

namespace UNObot.Services
{
    internal class DebugService
    {
        public DebugService(IConfiguration config, LoggerService logger)
        {
            if (!config["version"].Contains("Debug", StringComparison.OrdinalIgnoreCase))
                return;
            logger.Log(LogSeverity.Debug, "Logger activated!");
        }
    }
}