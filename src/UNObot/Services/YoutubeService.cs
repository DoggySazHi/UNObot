using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UNObot.Modules;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;

namespace UNObot.Services
{
    public class YoutubeService
    {
        // Seconds.
        private const double DL_TIMEOUT = 5.0;
        private static readonly string DownloadPath = Path.Combine(Directory.GetCurrentDirectory(), "Music");

        private static YoutubeService Instance;
        private YoutubeClient Client;
        private YoutubeConverter Converter;

        private YoutubeService()
        {
            Client = new YoutubeClient();
            Converter = new YoutubeConverter(Client, "/usr/local/bin/ffmpeg");
            if (Directory.Exists(DownloadPath))
                Directory.Delete(DownloadPath, true);
            Directory.CreateDirectory(DownloadPath);
        }

        public static YoutubeService GetSingleton()
        {
            if (Instance == null)
                Instance = new YoutubeService();
            return Instance;
        }

        public async Task<Tuple<string, string, string>> GetInfo(string URL)
        {
            URL = URL.TrimStart('<', '>').TrimEnd('<', '>');
            if (!YoutubeClient.TryParseVideoId(URL, out string Id))
                throw new Exception("Could not get information from URL! Is the link valid?");
            var VideoData = await Client.GetVideoAsync(Id);
            var Duration = TimeString(VideoData.Duration);
            return new Tuple<string, string, string>(VideoData.Title, Duration, VideoData.Thumbnails.MediumResUrl);
        }

        public async Task<Tuple<Tuple<string, string, string>, string>> SearchVideo(string Query)
        {
            var Data = await Client.SearchVideosAsync(Query, 1);
            if (Data.Count == 0)
                throw new Exception("No results found!");
            var VideoData = Data[0];
            var Duration = TimeString(VideoData.Duration);
            return new Tuple<Tuple<string, string, string>, string>(new Tuple<string, string, string>(VideoData.Title, Duration, VideoData.Thumbnails.MediumResUrl), VideoData.GetUrl());
        }

        public async Task<Playlist> GetPlaylist(string URL)
        {
            URL = URL.TrimStart('<', '>').TrimEnd('<', '>');
            if (!YoutubeClient.TryParsePlaylistId(URL, out string Id))
                throw new Exception("Could not get playlist from URL! Is the link valid?");
            var VideoData = await Client.GetPlaylistAsync(Id);
            return VideoData;
        }

        private string PathToGuildFolder(ulong Guild)
        {
            string DirectoryPath = Path.Combine(DownloadPath, Guild.ToString());
            if (!Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);
            return DirectoryPath;
        }

        public void DeleteGuildFolder(ulong Guild, params string[] Skip)
        {
            var MusicPath = PathToGuildFolder(Guild);
            var Files = Directory.GetFiles(MusicPath);
            foreach(var Filename in Files)
            {
                try
                {
                    foreach (var FileToSkip in Skip)
                        if (FileToSkip == Filename)
                            continue;
                    var FilePath = Path.Combine(MusicPath, Filename);
                    if (File.Exists(FilePath))
                        File.Delete(FilePath);
                    else
                        Console.WriteLine("Song didn't exist?");
                }
                catch(Exception) { }
            }
        }

        public async Task<string> Download(string URL, ulong Guild)
        {
            URL = URL.TrimStart('<', '>').TrimEnd('<', '>');
            if (!YoutubeClient.TryParseVideoId(URL, out string Id))
                throw new Exception("Invalid video link!");

            Console.WriteLine("Id: " + Id);
            if(Id == null)
            {
                var StartIndex = URL.IndexOf("?v=") + 3;
                var EndIndex = URL.IndexOf("&");
                if (EndIndex < 0)
                    EndIndex = URL.Length;
                Id = URL.Substring(StartIndex, EndIndex - StartIndex);
            }

            Console.WriteLine("New Id: " + Id);
            var MediaStreams = await Client.GetVideoMediaStreamInfosAsync(Id);

            if (MediaStreams.Audio.Count == 0)
            {
                string Path = GetNextFile(Guild, "mp3");
                try
                {
                    await Converter.DownloadVideoAsync(Id, Path).ConfigureAwait(false);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    throw ex;
                }
                Console.WriteLine("Downloaded");
                return Path;
            }
            else
                return await Download(Guild, MediaStreams.Audio.WithHighestBitrate());
        }

        private async Task<string> Download(ulong Guild, AudioStreamInfo AudioStream)
        {
            string Extension = "webm";
            try
            {
                Extension = AudioStream.Container.GetFileExtension();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.WriteLine("Got Extension");

            string FileName = GetNextFile(Guild, Extension);


            await Client.DownloadMediaStreamAsync(AudioStream, FileName);
            Console.WriteLine("Downloaded");

            var StartTime = DateTime.Now;

            while ((DateTime.Now - StartTime).TotalSeconds < DL_TIMEOUT)
                if (File.Exists(FileName))
                    return FileName.Replace("\n", "").Replace(Environment.NewLine, "");

            throw new Exception("Failed to download file.");
        }

        private string GetNextFile(ulong Guild, string Extension)
        {
            string FileName = "";
            int Count = 0;
            do
            {
                FileName = Path.Combine(PathToGuildFolder(Guild), "downloadSong" + ++Count + "." + Extension);
            } while (File.Exists(FileName));
            Console.WriteLine("Saving to " + FileName);
            return FileName;
        }

        public static string TimeString(TimeSpan Ts)
            => $"{(Ts.Hours > 0 ? $"{Ts.Hours}:" : "")}{Ts.Minutes.ToString("00")}:{Ts.Seconds.ToString("00")}";
    }
}
