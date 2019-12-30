using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace UNObot.Services
{
    public class LoggerService
    {
        readonly ILogger _discordLogger;
        readonly ILogger _commandsLogger;

        // ReSharper disable once UnusedParameter.Local
        public LoggerService(DiscordSocketClient discord, CommandService commands, ILoggerFactory loggerFactory)
        {
            var loggerFactory1 = ConfigureLogging();
            _discordLogger = loggerFactory1.CreateLogger("discord");
            _commandsLogger = loggerFactory1.CreateLogger("commands");

            discord.Log += LogDiscord;
            commands.Log += LogCommand;
        }

        ILoggerFactory ConfigureLogging()
        {
            return LoggerFactory.Create(builder => builder.AddConsole());
        }

        Task LogDiscord(LogMessage message)
        {
            _discordLogger.Log(
                LogLevelFromSeverity(message.Severity),
                0,
                message,
                message.Exception,
                (_1, _2) => message.ToString(prependTimestamp: false));
            return Task.CompletedTask;
        }

        Task LogCommand(LogMessage message)
        {
            // Return an error message for async commands

#if DEBUG
            if (message.Exception is CommandException command)
            {
                // Don't risk blocking the logging task by awaiting a message send; ratelimits!?
                var _ = command.Context.Channel.SendMessageAsync($"Error: {command.Message}");
            }
#endif

            _commandsLogger.Log(
                LogLevelFromSeverity(message.Severity),
                0,
                message,
                message.Exception,
                (_1, _2) => message.ToString());
            return Task.CompletedTask;
        }

        static LogLevel LogLevelFromSeverity(LogSeverity severity)
            => (LogLevel)(Math.Abs((int)severity - 5));

    }
}
