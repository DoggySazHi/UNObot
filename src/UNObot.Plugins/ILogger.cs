using System;
using Discord;

namespace UNObot.Plugins;

public interface ILogger
{
    void Log(LogSeverity severity, string message, Exception exception = null);
}