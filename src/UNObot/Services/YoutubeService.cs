using System;
using System.IO;
using System.Threading.Tasks;
using UNObot.Modules;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Models.MediaStreams;

namespace UNObot.Services
{
    public class YoutubeService
    {
        // Seconds.
        private static readonly double DL_TIMEOUT = 5.0;
        private static readonly string DownloadPath = Path.Combine(Directory.GetCurrentDirectory(), "Music");

        private static YoutubeService Instance;
        private YoutubeClient Client = new YoutubeClient();

        private YoutubeService() { }

        public static YoutubeService GetSingleton()
        {
            if (Instance == null)
                Instance = new YoutubeService();
            return Instance;
        }

        public static async Task<Tuple<string, string, string>> GetInfo(string URL)
        {
            YoutubeClient Client = new YoutubeClient();

            if (!YoutubeClient.TryParseVideoId(URL, out string Id))
                throw new Exception("Could not get information from URL! Is the link valid?");
            var VideoData = await Client.GetVideoAsync(Id);
            var Duration = string.Format("{0:g}", VideoData.Duration);
            return new Tuple<string, string, string>(VideoData.Title, Duration, VideoData.Thumbnails.StandardResUrl);
        }

        public static async Task<string> Download(string URL)
        {
            YoutubeClient Client = new YoutubeClient();
            string FileName;

            // Search for empty buffer files.

            int Count = 0;
            do
            {
                FileName = Path.Combine(DownloadPath, "downloadSong" + ++Count + ".mp3");
            } while (File.Exists(FileName));

            if (!YoutubeClient.TryParseVideoId(URL, out string Id))
                throw new Exception("Invalid video link!");
            //TODO bypass converter part
            var converter = new YoutubeConverter(Client, "/usr/local/bin/ffmpeg");
            await converter.DownloadVideoAsync(Id, FileName);

            var StartTime = DateTime.Now;

            while ((DateTime.Now - StartTime).TotalSeconds < DL_TIMEOUT)
                if (File.Exists(FileName))
                    return FileName.Replace("\n", "").Replace(Environment.NewLine, "");

            throw new Exception("Failed to download file.");
        }
    }
}
