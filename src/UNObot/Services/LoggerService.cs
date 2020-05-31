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
        private static LoggerService instance;
        private static StreamWriter fileLog;
        private const string LogFolder = "Logs";
        private readonly string CurrentLog;
        private static readonly object lockObj;

        public static LoggerService GetSingleton()
        {
            return instance ??= new LoggerService();
        }

        static LoggerService()
        {
            lockObj = new object();
        }

        private LoggerService()
        {
            CurrentLog = $"{DateTime.Today:MM-dd-yyyy}.log";
            if (!Directory.Exists(LogFolder))
                Directory.CreateDirectory(LogFolder);
            fileLog = new StreamWriter(Path.Combine(LogFolder, CurrentLog), true);
            Task.Run(CompressOldLogs);

            fileLog.WriteLineAsync();
            fileLog.WriteLineAsync($"--- UNObot Starting at {DateTime.Now:G} ---");
            fileLog.WriteLineAsync();

            Log(LogSeverity.Info, "Logging service started!");
        }

        private async Task CompressOldLogs()
        {
            foreach (var file in new DirectoryInfo(LogFolder).GetFiles())
            {
                if (file.Name.Contains(CurrentLog) || CurrentLog.Contains(file.Name)) continue;
                var compressed = false;
                await using (var originalFileStream = file.OpenRead())
                {
                    if ((File.GetAttributes(file.FullName) & 
                         FileAttributes.Hidden) != FileAttributes.Hidden & file.Extension != ".gz")
                    {
                        await using (var compressedFileStream = File.Create(file.FullName + ".gz"))
                        {
                            await using var compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress);
                            originalFileStream.CopyTo(compressionStream);
                        }

                        compressed = true;
                    }
                }
                if(compressed)
                    file.Delete();
            }
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
            // Don't hold the actions with a basic logger.
            Task.Run(() =>
            {
                lock (lockObj)
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
                            ColorConsole.Write("WARNING", ConsoleColor.Yellow);
                            break;
                        case LogSeverity.Info:
                            ColorConsole.Write("INFO", ConsoleColor.Cyan);
                            break;
                    }

                    var Time = $" {DateTime.Now:MM/dd/yyyy HH:mm:ss}] ";
                    Console.Write(Time);
                    Console.WriteLine(Message);

                    var OutputMessage = $"[{Severity.ToString().ToUpper()}{Time}{Message}";
                    fileLog?.WriteLineAsync(OutputMessage);

                    if (Exception == null) return;

                    ColorConsole.WriteLine(Exception.ToString(), ConsoleColor.Red);
                    fileLog?.WriteLineAsync(Exception.ToString());
                }
            });
        }

        public void Dispose()
        {
            if (fileLog == null) return;
            lock (lockObj)
            {
                fileLog.Flush();
                fileLog.Dispose();
            }
        }
    }
}
