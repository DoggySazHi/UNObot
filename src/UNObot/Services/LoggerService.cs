using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.TerminalCore;

namespace UNObot.Services
{
    public class LoggerService : IDisposable
    {
        private const string LogFolder = "Logs";
        private static LoggerService _instance;
        private static StreamWriter _fileLog;
        private static readonly object LockObj;
        private readonly string _currentLog;

        static LoggerService()
        {
            LockObj = new object();
        }

        private LoggerService()
        {
            _currentLog = $"{DateTime.Today:MM-dd-yyyy}.log";
            if (!Directory.Exists(LogFolder))
                Directory.CreateDirectory(LogFolder);
            _fileLog = new StreamWriter(Path.Combine(LogFolder, _currentLog), true);
            Task.Run(CompressOldLogs);

            _fileLog.WriteLineAsync();
            _fileLog.WriteLineAsync($"--- UNObot Starting at {DateTime.Now:G} ---");
            _fileLog.WriteLineAsync();

            Log(LogSeverity.Info, "Logging service started!");
        }

        public void Dispose()
        {
            if (_fileLog == null) return;
            lock (LockObj)
            {
                _fileLog.Flush();
                _fileLog.Dispose();
            }
        }

        public static LoggerService GetSingleton()
        {
            return _instance ??= new LoggerService();
        }

        private async Task CompressOldLogs()
        {
            foreach (var file in new DirectoryInfo(LogFolder).GetFiles())
            {
                if (file.Name.Contains(_currentLog) || _currentLog.Contains(file.Name)) continue;
                var compressed = false;
                await using (var originalFileStream = file.OpenRead())
                {
                    if (((File.GetAttributes(file.FullName) &
                          FileAttributes.Hidden) != FileAttributes.Hidden) & (file.Extension != ".gz"))
                    {
                        await using (var compressedFileStream = File.Create(file.FullName + ".gz"))
                        {
                            await using var compressionStream =
                                new GZipStream(compressedFileStream, CompressionMode.Compress);
                            originalFileStream.CopyTo(compressionStream);
                        }

                        compressed = true;
                    }
                }

                if (compressed)
                    file.Delete();
            }
        }

        public async Task LogDiscord(LogMessage message)
        {
            await Task.Run(() =>
            {
                Log(message.Severity,
                    $"{message.Source}: {message.Message}",
                    message.Exception);
            });
        }

        public async Task LogCommand(LogMessage message)
        {
            // Return an error message for async commands
            var textMessage = message.Message;
            if (message.Exception is CommandException command)
            {
#if DEBUG
                var _ = command.Context.Channel.SendMessageAsync($"{command.Message}");
#endif
                textMessage = command.Message;
            }

            await Task.Run(() =>
            {
                Log(message.Severity,
                    textMessage,
                    message.Exception);
            });
        }

        public static void Log(LogSeverity severity, string message, Exception exception = null)
        {
            // Don't hold the actions with a basic logger.
            Task.Run(() =>
            {
                lock (LockObj)
                {
                    Console.Write("[");
                    switch (severity)
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
                            ColorConsole.Write("WARNING", ConsoleColor.Yellow);
                            break;
                        case LogSeverity.Info:
                            ColorConsole.Write("INFO", ConsoleColor.Cyan);
                            break;
                    }

                    var time = $" {DateTime.Now:MM/dd/yyyy HH:mm:ss}] ";
                    Console.Write(time);
                    Console.WriteLine(message);

                    var outputMessage = $"[{severity.ToString().ToUpper()}{time}{message}";
                    _fileLog?.WriteLineAsync(outputMessage);

                    if (exception == null) return;

                    ColorConsole.WriteLine(exception.ToString(), ConsoleColor.Red);
                    _fileLog?.WriteLineAsync(exception.ToString());
                }
            });
        }
    }
}