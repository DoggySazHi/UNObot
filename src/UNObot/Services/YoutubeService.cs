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
        private const double DlTimeout = 5.0;

        private const int DlAttempts = 3;

        // Milliseconds.
        private const int DlDelay = 5000;
        private static readonly string DownloadPath = Path.Combine(Directory.GetCurrentDirectory(), "Music");

        private static YoutubeService _instance;
        private readonly YoutubeClient _client;
        private readonly YoutubeConverter _converter;

        private YoutubeService()
        {
            _client = new YoutubeClient();
            _converter = new YoutubeConverter(_client, "/usr/local/bin/ffmpeg");
            if (Directory.Exists(DownloadPath))
                Directory.Delete(DownloadPath, true);
            Directory.CreateDirectory(DownloadPath);
        }

        public static YoutubeService GetSingleton()
        {
            return _instance ??= new YoutubeService();
        }

        public async Task<Tuple<string, string, string>> GetInfo(string url)
        {
            url = url.Replace("<", "").Replace(">", "");
            LoggerService.Log(LogSeverity.Debug, url);
            var videoData = await _client.Videos.GetAsync(url);
            var duration = TimeString(videoData.Duration);
            return new Tuple<string, string, string>(videoData.Title, duration, videoData.Thumbnails.MediumResUrl);
        }

        public async Task<Tuple<Tuple<string, string, string>, string>> SearchVideo(string query)
        {
            LoggerService.Log(LogSeverity.Verbose, "Searching videos...");
            var data = await _client.Search.GetVideosAsync(query).ToListAsync();
            if (data.Count == 0)
                throw new Exception("No results found!");
            var videoData = data[0];
            var duration = TimeString(videoData.Duration);
            LoggerService.Log(LogSeverity.Verbose, "Found video.");
            return new Tuple<Tuple<string, string, string>, string>(
                new Tuple<string, string, string>(videoData.Title, duration, videoData.Thumbnails.MediumResUrl),
                videoData.Url);
        }

        public async Task<Playlist> GetPlaylist(string url)
        {
            url = url.Replace("<", "").Replace(">", "");
            LoggerService.Log(LogSeverity.Debug, url);
            var videoData = await _client.Playlists.GetAsync(url);
            return videoData;
        }

        public async Task<List<Video>> GetPlaylistVideos(PlaylistId id)
        {
            var videos = await _client.Playlists.GetVideosAsync(id).ToListAsync();
            return videos;
        }

        public async Task<string> GetPlaylistThumbnail(PlaylistId id)
        {
            var video = await _client.Playlists.GetVideosAsync(id).FirstAsync();
            return video.Thumbnails.MediumResUrl;
        }

        private string PathToGuildFolder(ulong guild)
        {
            var directoryPath = Path.Combine(DownloadPath, guild.ToString());
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            return directoryPath;
        }

        public void DeleteGuildFolder(ulong guild, params string[] skip)
        {
            var musicPath = PathToGuildFolder(guild);
            var files = Directory.GetFiles(musicPath);
            foreach (var filename in files)
                try
                {
                    var fileSkip = false;
                    foreach (var fileToSkip in skip)
                        if (fileToSkip.Contains(filename) || filename.Contains(fileToSkip))
                            fileSkip = true;
                    if (fileSkip)
                        continue;
                    var filePath = Path.Combine(musicPath, filename);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    else
                        LoggerService.Log(LogSeverity.Warning, "Song didn't exist?");
                }
                catch (Exception)
                {
                    // ignored
                }
        }

        public async Task<string> Download(string url, ulong guild)
        {
            url = url.TrimStart('<', '>').TrimEnd('<', '>');

            LoggerService.Log(LogSeverity.Debug, "New URL: " + url);
            var video = await _client.Videos.GetAsync(url);
            var mediaStreams = await _client.Videos.Streams.GetManifestAsync(video.Id);
            if (!mediaStreams.GetAudio().Any())
            {
                var path = GetNextFile(guild, video.Id, "mp3");
                if (File.Exists(path))
                    return path;
                try
                {
                    await _converter.DownloadVideoAsync(url, path).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LoggerService.Log(LogSeverity.Debug, "Error downloading song!", ex);
                    throw;
                }

                LoggerService.Log(LogSeverity.Debug, "Downloaded");
                return path;
            }

            return await Download(guild, video.Id.Value, mediaStreams.Streams.WithHighestBitrate());
        }

        private async Task<string> Download(ulong guild, string id, IStreamInfo audioStream)
        {
            var extension = "webm";
            try
            {
                extension = audioStream.Container.Name;
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Debug, "Error downloading song!", ex);
            }

            LoggerService.Log(LogSeverity.Debug, "Got Extension");

            var fileName = GetNextFile(guild, id, extension);

            if (File.Exists(fileName))
                return fileName;

            for (var i = 0; i < DlAttempts; i++)
                try
                {
                    await _client.Videos.Streams.DownloadAsync(audioStream, fileName);
                    LoggerService.Log(LogSeverity.Debug, $"Downloaded at {fileName}.");
                    break;
                }
                catch (Exception e)
                {
                    var message = e.ToString();
                    // I really hate this. But there's no enum for web status. And the Web stuff is embedded in the library.
                    if (message.Contains("429") ||
                        message.Contains("too many requests", StringComparison.CurrentCultureIgnoreCase))
                        LoggerService.Log(LogSeverity.Warning, "We're getting rate-limited by YouTube!!!");
                    LoggerService.Log(LogSeverity.Error,
                        $"Failed to download! This is attempt {i}/{DlAttempts}. Waiting for {DlDelay / 1000.0} seconds.",
                        e);
                    await Task.Delay(DlDelay);
                }

            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalSeconds < DlTimeout)
                if (File.Exists(fileName))
                    return fileName;

            throw new Exception("Failed to download file; couldn't find!");
        }

        private string GetNextFile(ulong guild, string id, string extension)
        {
            var fileName = Path.Combine(PathToGuildFolder(guild), $"{id}.{extension}");
            LoggerService.Log(LogSeverity.Debug, "Saving to " + fileName);
            return fileName;
        }

        public static string TimeString(TimeSpan ts)
        {
            return $"{(ts.Hours > 0 ? $"{ts.Hours}:" : "")}{ts.Minutes:00}:{ts.Seconds:00}";
        }
    }
}