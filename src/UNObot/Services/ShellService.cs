using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace UNObot.Services
{
    internal class ShellService
    {
        private LoggerService _logger;
        public ShellService(LoggerService logger)
        {
            _logger = logger;
        }
        
        internal async Task<string> RunYtdl(string cmd)
        {
            var result = new TaskCompletionSource<string>();

            new Thread(() =>
            {
                var escapedArgs = cmd.Replace("\"", "\\\"");
                _logger.Log(LogSeverity.Debug, escapedArgs);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "youtube-dl",
                        Arguments = $"-4 {escapedArgs}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                result.SetResult(process.StandardOutput.ReadToEnd());
                process.WaitForExit();
            }).Start();

            var awaited = await result.Task;
            _logger.Log(LogSeverity.Debug, $"Shell result: {awaited}");
            if (awaited == null)
                throw new Exception("Shell failed!");
            return awaited;
        }

        // Should be a file path.
        internal Process GetAudioStream(string path)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "/usr/local/bin/ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            return Process.Start(ffmpeg);
        }

        internal async Task<string> ConvertToMp3(string path)
        {
            var result = new TaskCompletionSource<string>();

            new Thread(() =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/local/bin/ffmpeg",
                        Arguments = $"-hide_banner -loglevel panic -i ${path} -vn -ab 128k -ar 44100 -y ${path}.mp3",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    }
                };
                process.Start();
                result.SetResult(process.StandardOutput.ReadToEnd());
                process.WaitForExit();
            }).Start();

            var awaited = await result.Task;
            _logger.Log(LogSeverity.Debug, $"Shell result: {awaited}");
            if (awaited == null)
                throw new Exception("Shell failed!");
            return awaited;
        }

        internal async Task<string> GitFetch()
        {
            var result = new TaskCompletionSource<string>();

            new Thread(() =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/git",
                        Arguments = "fetch",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    }
                };
                process.Start();
                result.SetResult(process.StandardOutput.ReadToEnd());
                process.WaitForExit();
            }).Start();

            var awaited = await result.Task;
            _logger.Log(LogSeverity.Debug, $"Shell result: {awaited}");
            if (awaited == null)
                throw new Exception("Shell failed!");
            return awaited;
        }

        internal async Task<string> GitStatus()
        {
            var result = new TaskCompletionSource<string>();

            new Thread(() =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/git",
                        Arguments = "status",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    }
                };
                process.Start();
                result.SetResult(process.StandardOutput.ReadToEnd());
                process.WaitForExit();
            }).Start();

            var awaited = await result.Task;
            _logger.Log(LogSeverity.Debug, $"Shell result: {awaited}");
            if (awaited == null)
                throw new Exception("Shell failed!");
            return awaited;
        }
    }
}