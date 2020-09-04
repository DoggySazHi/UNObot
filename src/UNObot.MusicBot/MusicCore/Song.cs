using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using UNObot.MusicBot.Services;
using UNObot.Plugins;
using YoutubeExplode.Exceptions;

namespace UNObot.MusicBot.MusicCore
{
    // Can't use Struct, needs passing by reference.
    internal class Song
    {
        private ManualResetEvent _endCache;

        internal Song(string url, Tuple<string, string, string> data, ulong user, ulong guild)
        {
            Url = url;
            Name = data.Item1;
            Duration = data.Item2;
            ThumbnailUrl = data.Item3;
            RequestedBy = user;
            RequestedGuild = guild;
        }

        internal string Url { get; }
        internal string PathCached { get; set; }
        internal ulong RequestedBy { get; }
        internal ulong RequestedGuild { get; }
        internal string Name { get; }
        internal string Duration { get; }
        internal string ThumbnailUrl { get; }

        internal async Task Cache(YoutubeService youtube, ILogger logger)
        {
            if (string.IsNullOrEmpty(PathCached) || !File.Exists(PathCached))
            {
                try
                {
                    logger.Log(LogSeverity.Debug, $"Caching {Name}");
                    PathCached = "Caching...";
                    PathCached = await youtube.Download(Url, RequestedGuild);

                    logger.Log(LogSeverity.Debug, "Finished caching.");
                }
                finally
                {
                    _endCache?.Set();
                }
            }
        }

        internal void SetCacheEvent(ManualResetEvent cacheFinished)
        {
            if (!string.IsNullOrEmpty(PathCached) && PathCached != "Caching...")
            {
                cacheFinished.Set();
                return;
            }

            _endCache = cacheFinished;
        }
    }
}