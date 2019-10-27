using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UNObot.Modules
{
    public static class Shell
    {
        public static async Task<string> Run(string cmd)
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
                        Arguments = $"{escapedArgs}",
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
            throw new Exception("Video URL not supported!");
        }

        public static async Task<string> DownloadPlaylist(string URL)
        {
            URL = URLFixer(URL);
            if (URL.ToLower().Contains("youtube.com"))
            {
                return await DownloadPlaylistFromYouTube(URL);
            }
            throw new Exception("Video URL not supported!");
        }

        public static async Task<Tuple<string, string>> GetInfo(string URL)
        {
            URL = URLFixer(URL);
            if (URL.ToLower().Contains("youtube.com"))
            {
                return await GetInfoFromYouTube(URL);
            }
            throw new Exception("Video URL not supported!");
        }

        private static async Task<Tuple<string, string>> GetInfoFromYouTube(string URL)
        {
            string Title = "No Title Found";
            string Duration = "0";

            var lines = (await Shell.Run($"-s -e --get-duration {URL}")).Split('\n');
            if (lines.Length >= 2)
            {
                Title = lines[0];
                Duration = lines[1];
            }

            return new Tuple<string, string>(Title, Duration);
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

            var Result = await Shell.Run($"-x --audio-format mp3 -o {FileName} {url}");

            var StartTime = DateTime.Now;

            while ((DateTime.Now - StartTime).TotalSeconds < DL_TIMEOUT)
            {
                if (File.Exists(FileName))
                {
                    //Return MP3 Path & Video Title
                    return FileName.Replace("\n", "").Replace(Environment.NewLine, "");
                }
            }
            Console.WriteLine($"Could not download Song, youtube-dl responded with:\n{Result}");
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

            var Result = await Shell.Run($"--extract-audio --audio-format mp3 -o {FileName} {URL}");

            var StartTime = DateTime.Now;

            while ((DateTime.Now - StartTime).TotalSeconds < DL_TIMEOUT)
            {
                if (File.Exists(FileName))
                {
                    //Return MP3 Path & Video Title
                    return FileName.Replace("\n", "").Replace(Environment.NewLine, "");
                }
            }
            //Error downloading
            Console.WriteLine($"Could not download Song, youtube-dl responded with:\n{Result}");
            throw new Exception("Failed to download file.");
        }
    }
}