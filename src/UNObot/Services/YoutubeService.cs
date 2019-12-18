﻿using System;
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
        public long[] Timings { get; private set; }

        private YoutubeService()
        {
            Client = new YoutubeClient();
            Converter = new YoutubeConverter(Client, "/usr/local/bin/ffmpeg");
            if (Directory.Exists(DownloadPath))
                Directory.Delete(DownloadPath, true);
            Directory.CreateDirectory(DownloadPath);
            Timings = new long[6];
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

        public void DeleteGuildFolder(ulong Guild)
        {
            Directory.Delete(PathToGuildFolder(Guild));
        }

        public async Task<string> Download(string URL, ulong Guild)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            URL = URL.TrimStart('<', '>').TrimEnd('<', '>');
            if (!YoutubeClient.TryParseVideoId(URL, out string Id))
            {
                stopWatch.Stop();
                Timings[1] = stopWatch.ElapsedMilliseconds;
                throw new Exception("Invalid video link!");
            }

            stopWatch.Stop();
            Timings[1] = stopWatch.ElapsedMilliseconds;
            stopWatch.Restart();

            var MediaStreams = await Client.GetVideoMediaStreamInfosAsync(Id);

            stopWatch.Stop();
            Timings[2] = stopWatch.ElapsedMilliseconds;
            stopWatch.Restart();

            var AudioStream = MediaStreams.Audio.WithHighestBitrate();

            stopWatch.Stop();
            Timings[3] = stopWatch.ElapsedMilliseconds;
            stopWatch.Restart();

            var Extension = AudioStream.Container.GetFileExtension();

            string FileName;

            // Search for empty buffer files.

            int Count = 0;
            do
            {
                FileName = Path.Combine(PathToGuildFolder(Guild), "downloadSong" + ++Count + "." + Extension);
            } while (File.Exists(FileName));

            stopWatch.Stop();
            Timings[0] = stopWatch.ElapsedMilliseconds;
            stopWatch.Restart();

            stopWatch.Stop();
            Timings[4] = stopWatch.ElapsedMilliseconds;
            stopWatch.Restart();

            await Client.DownloadMediaStreamAsync(AudioStream, FileName);

            stopWatch.Stop();
            Timings[5] = stopWatch.ElapsedMilliseconds;
            stopWatch.Restart();

            var StartTime = DateTime.Now;

            while ((DateTime.Now - StartTime).TotalSeconds < DL_TIMEOUT)
                if (File.Exists(FileName))
                    return FileName.Replace("\n", "").Replace(Environment.NewLine, "");

            throw new Exception("Failed to download file.");
        }

        public static string TimeString(TimeSpan Ts)
            => $"{(Ts.Hours > 0 ? $"{Ts.Hours}:" : "")}{Ts.Minutes.ToString("00")}:{Ts.Seconds.ToString("00")}";
    }
}
