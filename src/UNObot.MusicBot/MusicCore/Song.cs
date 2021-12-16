using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using UNObot.MusicBot.Services;
using UNObot.Plugins;

namespace UNObot.MusicBot.MusicCore;

// Can't use Struct, needs passing by reference.
public class Song
{
    private ManualResetEvent _endCache;

    public Song(string url, Tuple<string, string, string> data, ulong user, ulong guild)
    {
        Url = url;
        Name = data.Item1;
        Duration = data.Item2;
        ThumbnailUrl = data.Item3;
        RequestedBy = user;
        RequestedGuild = guild;
    }

    public string Url { get; }
    public string PathCached { get; set; }
    public ulong RequestedBy { get; }
    public ulong RequestedGuild { get; }
    public string Name { get; }
    public string Duration { get; }
    public string ThumbnailUrl { get; }

    public async Task Cache(YoutubeService youtube, ILogger logger)
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

    public void SetCacheEvent(ManualResetEvent cacheFinished)
    {
        if (!string.IsNullOrEmpty(PathCached) && PathCached != "Caching...")
        {
            cacheFinished.Set();
            return;
        }

        _endCache = cacheFinished;
    }
}