using System;
using Discord;
using UNObot.Plugins;

namespace UNObot.Services;

public class DebugService
{
    public DebugService(IUNObotConfig config, ILogger logger)
    {
        if (!config.Version.Contains("Debug", StringComparison.OrdinalIgnoreCase))
            return;
        logger.Log(LogSeverity.Debug, "Logger activated!");
    }
}