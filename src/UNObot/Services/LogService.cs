using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

#pragma warning disable CS1701 // Assuming assembly reference matches identity
#pragma warning disable CS1702 // Assuming assembly reference matches identity

namespace DiscordBot.Services
{
    public class LogService
    {
        readonly DiscordSocketClient _discord;
        readonly CommandService _commands;
        readonly ILoggerFactory _loggerFactory;
        readonly ILogger _discordLogger;
        readonly ILogger _commandsLogger;

        public LogService(DiscordSocketClient discord, CommandService commands, ILoggerFactory loggerFactory)
        {
            _discord = discord;
            _commands = commands;

            _loggerFactory = ConfigureLogging(loggerFactory);
            _discordLogger = _loggerFactory.CreateLogger("discord");
            _commandsLogger = _loggerFactory.CreateLogger("commands");

            _discord.Log += LogDiscord;
            _commands.Log += LogCommand;
        }

        ILoggerFactory ConfigureLogging(ILoggerFactory factory)
        {
            factory.AddConsole();
            return factory;
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
            /*
            if (message.Exception is CommandException command)
            {
                // Don't risk blocking the logging task by awaiting a message send; ratelimits!?
                var _ = command.Context.Channel.SendMessageAsync($"Error: {command.Message}");
            }
            */

            _commandsLogger.Log(
                LogLevelFromSeverity(message.Severity),
                0,
                message,
                message.Exception,
                (_1, _2) => message.ToString(prependTimestamp: false));
            return Task.CompletedTask;
        }

        static LogLevel LogLevelFromSeverity(LogSeverity severity)
            => (LogLevel)(Math.Abs((int)severity - 5));

    }
}
