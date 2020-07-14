using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using UNObot.Plugins.TerminalCore;
using YoutubeExplode.Playlists;

namespace UNObot.MusicBot.Services
{
    internal class EmbedService
    {
        private readonly YoutubeService _youtube;
        private readonly IConfiguration _config;
        private readonly DiscordSocketClient _client;
        
        public EmbedService(YoutubeService youtube, IConfiguration config, DiscordSocketClient client)
        {
            _youtube = youtube;
            _config = config;
            _client = client;
        }
        
        internal Tuple<Embed, Tuple<string, string, string>> DisplayAddSong(ulong userId, ulong serverId,
            string songUrl, Tuple<string, string, string> information)
        {
            var server = _client.GetGuild(serverId).Name;
            var username = _client.GetUser(userId).Username;
            var r = ThreadSafeRandom.ThisThreadsRandom;

            var builder = new EmbedBuilder()
                .WithTitle(information.Item1)
                .WithUrl(songUrl)
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {_config["version"]} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithThumbnailUrl(information.Item3)
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Added in {server}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Duration", information.Item2)
                .AddField("Requested By", username);
            var embed = builder.Build();
            return new Tuple<Embed, Tuple<string, string, string>>(embed, information);
        }

        internal Embed DisplayNowPlaying(Song song, string currentDuration)
        {
            var username = _client.GetUser(song.RequestedBy).Username;
            var servername = _client.GetGuild(song.RequestedGuild).Name;
            var r = ThreadSafeRandom.ThisThreadsRandom;

            var builder = new EmbedBuilder()
                .WithTitle(song.Name)
                .WithUrl(song.Url)
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {_config["version"]} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithThumbnailUrl(song.ThumbnailUrl)
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Playing in {servername}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Duration",
                    $"{(string.IsNullOrEmpty(currentDuration) ? "" : $"{currentDuration}/")}{song.Duration}")
                .AddField("Requested By", username);
            return builder.Build();
        }

        internal async Task<Tuple<Embed, Playlist>> DisplayPlaylist(ulong userId, ulong serverId, string songUrl)
        {
            var username = _client.GetUser(userId).Username;
            var servername = _client.GetGuild(serverId).Name;
            var r = ThreadSafeRandom.ThisThreadsRandom;
            var playlist = await _youtube.GetPlaylist(songUrl);
            var thumbnail = await _youtube.GetPlaylistThumbnail(playlist.Id);

            var builder = new EmbedBuilder()
                .WithTitle(playlist.Title)
                .WithUrl(playlist.Url)
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {_config["version"]} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithThumbnailUrl(thumbnail)
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Playing in {servername}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Description", $"{playlist.Description}")
                .AddField("Author", $"{playlist.Author}")
                .AddField("Requested By", username);
            return new Tuple<Embed, Playlist>(builder.Build(), playlist);
        }

        internal Tuple<Embed, int> DisplaySongList(Song nowPlaying, List<Song> songs, int page)
        {
            var containers = new List<StringBuilder>();
            var r = ThreadSafeRandom.ThisThreadsRandom;
            var server = _client.GetGuild(nowPlaying.RequestedGuild).Name;

            var index = 0;

            if (songs.Count == 0)
                containers.Add(new StringBuilder("There are no songs queued."));

            while (index < songs.Count)
            {
                var list = new StringBuilder();

                for (var i = 0; i < 9; i++)
                {
                    if (index >= songs.Count)
                        break;
                    var s = songs[index++];
                    var username = _client.GetUser(s.RequestedBy).Username;
                    var nextLine = $"``{index}.``[{s.Name}]({s.Url}) |``{s.Duration} Requested by: {username}``\n\n";
                    if (list.Length + nextLine.Length > 1024)
                    {
                        index--;
                        break;
                    }

                    list.Append(nextLine);
                }

                containers.Add(list);
            }

            if (page <= 0 || page > containers.Count)
                return new Tuple<Embed, int>(null, containers.Count);
            return new Tuple<Embed, int>(
                DisplaySongList(server, r, page, containers.Count, containers[page - 1], nowPlaying), containers.Count);
        }

        private Embed DisplaySongList(string server, Random r, int page, int maxPages, StringBuilder list,
            Song nowPlaying)
        {
            var builder = new EmbedBuilder()
                .WithTitle("Now Playing")
                .WithDescription(
                    $"[{nowPlaying.Name}]({nowPlaying.Url}) |``{nowPlaying.Duration} Requested by: {_client.GetUser(nowPlaying.RequestedBy).Username}``")
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {_config["version"]} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Page {page}/{maxPages} | Playing in {server}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Queued", list.ToString());

            return builder.Build();
        }
    }
}