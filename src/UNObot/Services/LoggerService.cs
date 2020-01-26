using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.TerminalCore;

namespace UNObot.Services
{
    //TODO learn how to use dependency injection instead
    public class LoggerService
    {
        private static LoggerService instance;

        public static LoggerService GetSingleton()
        {
            if(instance == null)
                instance = new LoggerService();
            return instance;
        }

        private LoggerService()
        {

        }

        public async Task LogDiscord(LogMessage Message)
        {
            await Task.Run(() =>
            {
                Log(Message.Severity,
                    $"{Message.Source}: {Message.Message}",
                    Message.Exception);
            });
        }

        public async Task LogCommand(LogMessage Message)
        {
            // Return an error message for async commands
            string TextMessage = Message.Message;
            if (Message.Exception is CommandException command)
            {
#if DEBUG
                var _ = command.Context.Channel.SendMessageAsync($"{command.Message}");
#endif
                TextMessage = command.Message;
            }

            await Task.Run(() =>
            {
                Log(Message.Severity,
                    TextMessage,
                    Message.Exception);
            });
        }

        public static void Log(LogSeverity Severity, string Message, Exception Exception = null)
        {
            Console.Write("[");
            switch (Severity)
            {
                case LogSeverity.Verbose:
                    ColorConsole.Write("VERBOSE", ConsoleColor.Magenta);
                    break;
                case LogSeverity.Debug:
                    ColorConsole.Write("DEBUG", ConsoleColor.Green);
                    break;
                case LogSeverity.Error:
                    ColorConsole.Write("ERROR", ConsoleColor.Red);
                    break;
                case LogSeverity.Critical:
                    ColorConsole.Write("CRITICAL", ConsoleColor.Red);
                    break;
                case LogSeverity.Warning:
                    ColorConsole.Write("DEBUG", ConsoleColor.Yellow);
                    break;
                case LogSeverity.Info:
                    ColorConsole.Write("INFO", ConsoleColor.Cyan);
                    break;
            }
            Console.Write($" {DateTime.Now:MM/dd/yyyy HH:mm:ss}] ");
            Console.WriteLine(Message);
            
            if(Exception != null)
                ColorConsole.WriteLine(Exception.ToString(), ConsoleColor.Red);
        }
    }
}
