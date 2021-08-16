using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;

namespace UNObot.Services
{
    public struct Log
    {
        public LogSeverity Severity { get; set; }
        public string Message { get; set; }
        public Exception Error { get; set; }
    }
    
    public class LoggerService : IDisposable, ILogger
    {
        private const string LogFolder = "Logs";
        private readonly StreamWriter _fileLog;
        private readonly string _currentLog;
        private static bool _init;
        private readonly BlockingCollection<Log> _logQueue = new BlockingCollection<Log>();

        public LoggerService()
        {
            if(_init)
                throw new InvalidOperationException("The logger should not be created directly! Use your local singleton.");
            _init = true;
            
            _currentLog = $"{DateTime.Today:MM-dd-yyyy}.log";
            if (!Directory.Exists(LogFolder))
                Directory.CreateDirectory(LogFolder);
            _fileLog = new StreamWriter(Path.Combine(LogFolder, _currentLog), true);
            Task.Run(CompressOldLogs);

            _fileLog.WriteLineAsync();
            _fileLog.WriteLineAsync($"--- UNObot Starting at {DateTime.Now:G} ---");
            _fileLog.WriteLineAsync();

            Log(LogSeverity.Info, "Logging service started!");
            
            StartLogger();
        }

        private void StartLogger()
        {
            Task.Run(() =>
            {
                try
                {
                    foreach(var log in _logQueue.GetConsumingEnumerable())
                    {
                        Console.Write('[');
                        switch (log.Severity)
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
                        Console.WriteLine(log.Message);

                        var outputMessage = $"[{log.Severity.ToString().ToUpper()}{time}{log.Message}";
                        _fileLog?.WriteLine(outputMessage);

                        if (log.Error == null) continue;

                        ColorConsole.WriteLine(log.Error.ToString(), ConsoleColor.Red);
                        _fileLog?.WriteLine(log.Error.ToString());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Async logger died!\n" + e);
                    throw;
                }
            });
        }

        public void Dispose()
        {
            if (_fileLog == null) return;
            _logQueue.CompleteAdding();
            while(_logQueue.Count > 0) {}
            _fileLog.Flush();
            _fileLog.Dispose();
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
                            await originalFileStream.CopyToAsync(compressionStream);
                        }

                        compressed = true;
                    }
                }

                if (compressed)
                    file.Delete();
            }
        }

        public Task LogDiscord(LogMessage message)
        {
            Log(message.Severity,
                $"{message.Source}: {message.Message}",
                message.Exception);
            return Task.CompletedTask;
        }

        public Task LogCommand(LogMessage message)
        {
            // Return an error message for async commands
            var textMessage = message.Message;
            if (message.Exception is CommandException command)
            {
#if DEBUG
                var _ = command.Context.ReplyAsync($"{command.Message}");
#endif
                textMessage = command.Message;
            }

            Log(message.Severity,
                textMessage,
                message.Exception);
            return Task.CompletedTask;
        }

        public void Log(LogSeverity severity, string message, Exception exception = null)
        {
            _logQueue.Add(new Log
            {
                Severity = severity,
                Message = message,
                Error = exception
            });
        }
    }
}