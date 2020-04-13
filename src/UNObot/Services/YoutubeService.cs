using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace UNObot.Services
{
    public class YoutubeService
    {
        // Seconds.
        private const double DL_TIMEOUT = 5.0;
        private const int DL_ATTEMPTS = 3;
        // Milliseconds.
        private const int DL_DELAY = 5000;
        private static readonly string DownloadPath = Path.Combine(Directory.GetCurrentDirectory(), "Music");

        private static YoutubeService Instance;
        private readonly YoutubeClient Client;
        private readonly YoutubeConverter Converter;

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
            URL = URL.Replace("<", "").Replace(">", "");
            LoggerService.Log(LogSeverity.Debug, URL);
            var VideoData = await Client.Videos.GetAsync(URL);
            var Duration = TimeString(VideoData.Duration);
            return new Tuple<string, string, string>(VideoData.Title, Duration, VideoData.Thumbnails.MediumResUrl);
        }

        public async Task<Tuple<Tuple<string, string, string>, string>> SearchVideo(string Query)
        {
            var Data = await Client.Search.GetVideosAsync(Query).ToListAsync();
            if (Data.Count == 0)
                throw new Exception("No results found!");
            var VideoData = Data[0];
            var Duration = TimeString(VideoData.Duration);
            return new Tuple<Tuple<string, string, string>, string>(new Tuple<string, string, string>(VideoData.Title, Duration, VideoData.Thumbnails.MediumResUrl), VideoData.Url);
        }

        public async Task<Playlist> GetPlaylist(string URL)
        {
            URL = URL.Replace("<", "").Replace(">", "");
            LoggerService.Log(LogSeverity.Debug, URL);
            var VideoData = await Client.Playlists.GetAsync(URL);
            return VideoData;
        }

        public async Task<List<Video>> GetPlaylistVideos(PlaylistId Id)
        {
            var Videos = await Client.Playlists.GetVideosAsync(Id).ToListAsync();
            return Videos;
        }

        public async Task<string> GetPlaylistThumbnail(PlaylistId Id)
        {
            var Video = await Client.Playlists.GetVideosAsync(Id).FirstAsync();
            return Video.Thumbnails.MediumResUrl;
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
            foreach (var Filename in Files)
            {
                try
                {
                    var FileSkip = false;
                    foreach (var FileToSkip in Skip)
                        if (FileToSkip.Contains(Filename) || Filename.Contains(FileToSkip))
                            FileSkip = true;
                    if (FileSkip)
                        continue;
                    var FilePath = Path.Combine(MusicPath, Filename);
                    if (File.Exists(FilePath))
                        File.Delete(FilePath);
                    else
                        LoggerService.Log(LogSeverity.Warning, "Song didn't exist?");
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public async Task<string> Download(string URL, ulong Guild)
        {
            URL = URL.TrimStart('<', '>').TrimEnd('<', '>');

            LoggerService.Log(LogSeverity.Debug, "New URL: " + URL);
            var Video = await Client.Videos.GetAsync(URL);
            var MediaStreams = await Client.Videos.Streams.GetManifestAsync(Video.Id);
            if (!MediaStreams.GetAudio().Any())
            {
                string Path = GetNextFile(Guild, Video.Id, "mp3");
                if (File.Exists(Path))
                    return Path;
                try
                {
                    await Converter.DownloadVideoAsync(URL, Path).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LoggerService.Log(LogSeverity.Debug, "Error downloading song!", ex);
                    throw;
                }
                LoggerService.Log(LogSeverity.Debug, "Downloaded");
                return Path;
            }
            return await Download(Guild, Video.Id.Value, MediaStreams.Streams.WithHighestBitrate());
        }

        private async Task<string> Download(ulong Guild, string Id, IStreamInfo AudioStream)
        {
            var Extension = "webm";
            try
            {
                Extension = AudioStream.Container.Name;
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Debug, "Error downloading song!", ex);
            }
            LoggerService.Log(LogSeverity.Debug, "Got Extension");

            string FileName = GetNextFile(Guild, Id, Extension);

            if (File.Exists(FileName))
                return FileName;

            for (var i = 0; i < DL_ATTEMPTS; i++)
            {
                try
                {
                    await Client.Videos.Streams.DownloadAsync(AudioStream, FileName);
                    LoggerService.Log(LogSeverity.Debug, $"Downloaded at {AudioStream.Bitrate}.");
                    break;
                }
                catch (Exception e)
                {
                    var Message = e.ToString();
                    // I really hate this. But there's no enum for web status. And the Web stuff is embedded in the library.
                    if(Message.Contains("429") || Message.Contains("too many requests", StringComparison.CurrentCultureIgnoreCase))
                        LoggerService.Log(LogSeverity.Warning, "We're getting rate-limited by YouTube!!!");
                    LoggerService.Log(LogSeverity.Error, $"Failed to download! This is attempt {i}/{DL_ATTEMPTS}. Waiting for {DL_DELAY/1000.0} seconds.", e);
                    await Task.Delay(DL_DELAY);
                }
            }

            var StartTime = DateTime.Now;

            while ((DateTime.Now - StartTime).TotalSeconds < DL_TIMEOUT)
                if (File.Exists(FileName))
                    return FileName;

            throw new Exception("Failed to download file.");
        }

        private string GetNextFile(ulong Guild, string Id, string Extension)
        {
            string FileName = Path.Combine(PathToGuildFolder(Guild), $"{Id}.{Extension}");
            LoggerService.Log(LogSeverity.Debug, "Saving to " + FileName);
            return FileName;
        }

        public static string TimeString(TimeSpan Ts)
            => $"{(Ts.Hours > 0 ? $"{Ts.Hours}:" : "")}{Ts.Minutes:00}:{Ts.Seconds:00}";
    }
}
