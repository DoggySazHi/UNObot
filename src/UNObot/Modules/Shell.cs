using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;

namespace UNObot.Modules
{
    public static class Shell
    {
        public static async Task<string> RunYTDL(string cmd)
        {
            TaskCompletionSource<string> result = new TaskCompletionSource<string>();

            new Thread(() =>
            {
                var escapedArgs = cmd.Replace("\"", "\\\"");
                Console.WriteLine(escapedArgs);

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

            string awaited = await result.Task;
            Console.WriteLine($"Shell result: {awaited}");
            if (awaited == null)
                throw new Exception("Shell failed!");
            return awaited;
        }

        // Should be a file path.
        public static Process GetAudioStream(string Path)
        {
            ProcessStartInfo ffmpeg = new ProcessStartInfo
            {
                FileName = "/usr/local/bin/ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{Path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            return Process.Start(ffmpeg);
        }

        public async static Task<string> ConvertToMP3(string Path)
        {
            TaskCompletionSource<string> result = new TaskCompletionSource<string>();

            new Thread(() =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/local/bin/ffmpeg",
                        Arguments = $"-hide_banner -loglevel panic -i ${Path} -vn -ab 128k -ar 44100 -y ${Path}.mp3",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    }
                };
                process.Start();
                result.SetResult(process.StandardOutput.ReadToEnd());
                process.WaitForExit();
            }).Start();

            string awaited = await result.Task;
            Console.WriteLine($"Shell result: {awaited}");
            if (awaited == null)
                throw new Exception("Shell failed!");
            return awaited;
        }

        public async static Task<string> GitFetch()
        {
            TaskCompletionSource<string> result = new TaskCompletionSource<string>();

            new Thread(() =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/git",
                        Arguments = $"fetch",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    }
                };
                process.Start();
                result.SetResult(process.StandardOutput.ReadToEnd());
                process.WaitForExit();
            }).Start();

            string awaited = await result.Task;
            Console.WriteLine($"Shell result: {awaited}");
            if (awaited == null)
                throw new Exception("Shell failed!");
            return awaited;
        }

        public async static Task<string> GitStatus()
        {
            TaskCompletionSource<string> result = new TaskCompletionSource<string>();

            new Thread(() =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/git",
                        Arguments = $"status",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    }
                };
                process.Start();
                result.SetResult(process.StandardOutput.ReadToEnd());
                process.WaitForExit();
            }).Start();

            string awaited = await result.Task;
            Console.WriteLine($"Shell result: {awaited}");
            if (awaited == null)
                throw new Exception("Shell failed!");
            return awaited;
        }
    }
}