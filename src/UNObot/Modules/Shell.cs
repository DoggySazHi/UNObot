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
                //TODO Check the stupid URL fixing.
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
                FileName = "ffmpeg",
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
                        FileName = "ffmpeg",
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
    }

    public static class DownloadHelper
    {
        // Seconds.
        private static readonly double DL_TIMEOUT = 5.0;
        private static readonly string DownloadPath = Path.Combine(Directory.GetCurrentDirectory(), "Music");

        private static string URLFixer(string URL)
        {
            /*
            URL = URL.Trim();
            if (URL.StartsWith('<') && URL.EndsWith('>'))
                URL.Substring(1, URL.Length - 2);
            URL.Replace("?", "\\?");
            URL.Replace("=", "\\=");
            return URL;
            */
            return URL;
        }

        public static async Task<string> Download(string URL)
        {
            URL = URLFixer(URL);
            if (URL.ToLower().Contains("youtube.com"))
                return await DownloadFromYouTube(URL);
            throw new Exception("Not a YouTube URL!");
        }

        public static async Task<string> DownloadPlaylist(string URL)
        {
            URL = URLFixer(URL);
            if (URL.ToLower().Contains("youtube.com"))
                return await DownloadPlaylistFromYouTube(URL);
            throw new Exception("Not a YouTube URL!");
        }

        public static async Task<Tuple<string, string, string>> GetInfo(string URL)
        {
            URL = URLFixer(URL);
            if (URL.ToLower().Contains("youtube.com"))
                return await GetInfoFromYouTube(URL);
            throw new Exception("Not a YouTube URL!");
        }

        private static async Task<Tuple<string, string, string>> GetInfoFromYouTube(string URL)
        {
            var lines = (await Shell.RunYTDL($"-s -e --get-duration --get-thumbnail {URL}")).Split('\n');
            if (lines.Length >= 3)
                // Title, Duration, Thumbnail Link
                return new Tuple<string, string, string>(lines[0], lines[2], lines[1]);
            else
                throw new Exception("Could not get information from URL! Is the link valid?");
        }

        private static async Task<string> DownloadFromYouTube(string url)
        {
            string FileName;

            // Search for empty buffer files.

            int count = 0;
            do
            {
                FileName = Path.Combine(DownloadPath, "downloadSong" + ++count + ".mp3");
            } while (File.Exists(FileName));

            var Result = await Shell.RunYTDL($"-x --audio-format mp3 -o {FileName} {url}");

            var StartTime = DateTime.Now;

            while ((DateTime.Now - StartTime).TotalSeconds < DL_TIMEOUT)
                if (File.Exists(FileName))
                    return FileName.Replace("\n", "").Replace(Environment.NewLine, "");

            ColorConsole.WriteLine($"Could not download Song, youtube-dl responded with: {Result}", ConsoleColor.Red);
            throw new Exception("Failed to download file.");
        }

        private static async Task<string> DownloadPlaylistFromYouTube(string URL)
        {
            string FileName;
            int count = 0;
            do
            {
                FileName = Path.Combine(DownloadPath, "tempvideo" + ++count + ".mp3");
            } while (File.Exists(FileName));

            var Result = await Shell.RunYTDL($"--extract-audio --audio-format mp3 -o {FileName} {URL}");

            var StartTime = DateTime.Now;

            while ((DateTime.Now - StartTime).TotalSeconds < DL_TIMEOUT)
                if (File.Exists(FileName))
                    return FileName.Replace("\n", "").Replace(Environment.NewLine, "");

            ColorConsole.WriteLine($"Could not download Song, youtube-dl responded with: {Result}", ConsoleColor.Red);
            throw new Exception("Failed to download file.");
        }

        public static async Task<string> GetThumbnail(string URL)
        {
            var Result = await Shell.RunYTDL($"{URL} -s --get-thumbnail");
            if (Result.Contains("jpg") || Result.Contains("png"))
                return Result;

            ColorConsole.WriteLine($"Could not query thumbnail, youtube-dl responded with: {Result}", ConsoleColor.Red);
            throw new Exception("Failed to query thumbnail.");
        }
    }
}