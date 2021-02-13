using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using UNObot.MusicBot.MusicCore;
using UNObot.Plugins;

namespace UNObot.MusicBot.Services
{
    public class MusicBotService : IAsyncDisposable
    {
        private readonly List<Player> _musicPlayers = new List<Player>();
        private readonly ILogger _logger;
        private readonly YoutubeService _youtube;
        private readonly DiscordSocketClient _client;
        private readonly EmbedService _embed;

        public MusicBotService(ILogger logger, YoutubeService youtube, DiscordSocketClient client, EmbedService embed)
        {
            _logger = logger;
            _youtube = youtube;
            _client = client;
            _embed = embed;
            _client.UserVoiceStateUpdated += UserVoiceStateUpdated;
        }

        private async Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            if (oldState.VoiceChannel != null && newState.VoiceChannel == null)
                foreach (var player in _musicPlayers)
                    await player.CheckOnLeave().ConfigureAwait(false);
            else if (oldState.VoiceChannel == null && newState.VoiceChannel != null)
                foreach (var player in _musicPlayers)
                    await player.CheckOnJoin().ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var musicPlayer in _musicPlayers)
                await musicPlayer.DisposeAsync();
        }

        private async Task<Tuple<Player, string>> ConnectAsync(ulong guild, IVoiceChannel audioChannel,
            ISocketMessageChannel messageChannel)
        {
            var player = _musicPlayers.FindIndex(o => o.Guild == guild);
            if (player < 0)
            {
                var newPlayer = new Player(_logger, _youtube, _embed, guild, audioChannel, await audioChannel.ConnectAsync(), messageChannel);
                _musicPlayers.Add(newPlayer);
                _logger.Log(LogSeverity.Debug, "Generated new player.");
                return new Tuple<Player, string>(newPlayer, null);
            }

            if (_musicPlayers[player].Disposed)
            {
                _logger.Log(LogSeverity.Debug, "Replaced player.");
                _musicPlayers.RemoveAt(player);
                var newPlayer = new Player(_logger, _youtube, _embed, guild, audioChannel, await audioChannel.ConnectAsync(), messageChannel);
                _musicPlayers.Add(newPlayer);
                return new Tuple<Player, string>(newPlayer, null);
            }

            _logger.Log(LogSeverity.Debug, "Returned existing player.");
            return new Tuple<Player, string>(_musicPlayers[player], null);
        }

        public async Task<Tuple<Embed, string>> Add(ulong user, ulong guild, string url, IVoiceChannel channel,
            ISocketMessageChannel messageChannel, bool insertAtTop = false)
        {
            Embed embedOut;
            string error = null;
            if (insertAtTop && !await HasPermissions(user, guild, channel))
                return new Tuple<Embed, string>(null, "You do not have the power to run this command!");
            try
            {
                var information = _youtube.GetInfo(url);
                var result = _embed.DisplayAddSong(user, guild, url, await information);
                embedOut = result.Item1;
                var data = result.Item2;
                var player = await ConnectAsync(guild, channel, messageChannel);
                if (player.Item2 != null)
                    error = player.Item2;
                else
                    player.Item1.Add(url, data, user, guild, insertAtTop);
            }
            catch (Exception ex)
            {
                return new Tuple<Embed, string>(null, ex.Message);
            }

            return new Tuple<Embed, string>(embedOut, error);
        }

        public async Task<Tuple<Embed, string>> AddList(ulong user, ulong guild, string url, IVoiceChannel channel,
            ISocketMessageChannel messageChannel, bool insertAtTop = false)
        {
            Embed display = null;
            string message;
            if (insertAtTop && !await HasPermissions(user, guild, channel))
                return new Tuple<Embed, string>(null, "You do not have the power to run this command!");
            try
            {
                var playlist = await _embed.DisplayPlaylist(user, guild, url);
                display = playlist.Item1;
                var resultPlay = await _youtube.GetPlaylistVideos(playlist.Item2.Id);
                var player = await ConnectAsync(guild, channel, messageChannel);
                if (player.Item2 != null)
                {
                    message = player.Item2;
                }
                else
                {
                    if (insertAtTop)
                        for (var i = resultPlay.Count - 1; i >= 0; i--)
                        {
                            var video = resultPlay[i];
                            player.Item1.Add(video.Url,
                                new Tuple<string, string, string>(video.Title,
                                    YoutubeService.TimeString(video.Duration), video.Thumbnails.MediumResUrl), user,
                                guild, true);
                        }
                    else
                        foreach (var video in resultPlay)
                            player.Item1.Add($"https://www.youtube.com/watch?v={video.Id}",
                                new Tuple<string, string, string>(video.Title,
                                    YoutubeService.TimeString(video.Duration), video.Thumbnails.MediumResUrl), user,
                                guild);

                    message = $"Added {resultPlay.Count} song{(resultPlay.Count == 1 ? "" : "s")}.";
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new Tuple<Embed, string>(display, message);
        }

        public async Task<Tuple<Embed, string>> Search(ulong user, ulong guild, string query, IVoiceChannel channel,
            ISocketMessageChannel messageChannel, bool insertAtTop = false)
        {
            Embed embedOut;
            string error = null;
            if (insertAtTop && !await HasPermissions(user, guild, channel))
                return new Tuple<Embed, string>(null, "You do not have the power to run this command!");
            try
            {
                _logger.Log(LogSeverity.Verbose, "Searching videos for embed...");
                var information = await _youtube.SearchVideo(query);
                _logger.Log(LogSeverity.Verbose, "Attempting to embed...");
                var result = _embed.DisplayAddSong(user, guild, information.Item2, information.Item1);
                embedOut = result.Item1;
                var data = result.Item2;
                var player = await ConnectAsync(guild, channel, messageChannel);
                if (player.Item2 != null)
                    error = player.Item2;
                else
                    player.Item1.Add(information.Item2, data, user, guild, insertAtTop);
            }
            catch (Exception ex)
            {
                return new Tuple<Embed, string>(null, ex.Message);
            }

            return new Tuple<Embed, string>(embedOut, error);
        }

        public async Task<string> Pause(ulong user, ulong guild, IAudioChannel channel)
        {
            string message;
            try
            {
                var players = _musicPlayers.FindAll(o => o.Guild == guild);
                if (players.Count == 0 || players[0].Disposed)
                {
                    message = "Error: The server is not playing any music!";
                }
                else if (!await HasPermissions(user, guild, channel))
                {
                    message = "You do not have the power to run this command!";
                }
                else
                {
                    var skipMessage = players[0].TryPause();
                    if (!string.IsNullOrEmpty(skipMessage))
                        message = skipMessage;
                    else
                        message = "Player paused.";
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }

            return message;
        }

        public async Task<string> Play(ulong user, ulong guild, IAudioChannel channel)
        {
            string message;
            try
            {
                var players = _musicPlayers.FindAll(o => o.Guild == guild);
                if (players.Count == 0 || players[0].Disposed)
                {
                    message = "Error: The server is not playing any music!";
                }
                else if (!await HasPermissions(user, guild, channel))
                {
                    message = "You do not have the power to run this command!";
                }
                else
                {
                    var skipMessage = players[0].TryPlay();
                    if (!string.IsNullOrEmpty(skipMessage))
                        message = skipMessage;
                    else
                        message = "Player continued.";
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }

            return message;
        }

        public async Task<string> Shuffle(ulong user, ulong guild, IAudioChannel channel)
        {
            string message;
            try
            {
                var players = _musicPlayers.FindAll(o => o.Guild == guild);
                if (players.Count == 0 || players[0].Disposed)
                {
                    message = "Error: The server is not playing any music!";
                }
                else if (!await HasPermissions(user, guild, channel))
                {
                    message = "You do not have the power to run this command!";
                }
                else
                {
                    players[0].Shuffle();
                    message = "Shuffled.";
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }

            return message;
        }

        public async Task<string> ToggleLoop(ulong user, ulong guild, IAudioChannel channel)
        {
            string message;
            try
            {
                var players = _musicPlayers.FindAll(o => o.Guild == guild);
                if (players.Count == 0 || players[0].Disposed)
                    message = "Error: The server is not playing any music!";
                else if (!await HasPermissions(user, guild, channel))
                    message = "You do not have the power to run this command!";
                else
                    message = players[0].ToggleLoopSong();
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }

            return message;
        }

        public async Task<string> ToggleLoopQueue(ulong user, ulong guild, IAudioChannel channel)
        {
            string message;
            try
            {
                var players = _musicPlayers.FindAll(o => o.Guild == guild);
                if (players.Count == 0 || players[0].Disposed)
                    message = "Error: The server is not playing any music!";
                else if (!await HasPermissions(user, guild, channel))
                    message = "You do not have the power to run this command!";
                else
                    message = players[0].ToggleLoopPlaylist();
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }

            return message;
        }

        public async Task<string> Disconnect(ulong user, ulong guild, IAudioChannel channel)
        {
            string message;
            try
            {
                var players = _musicPlayers.FindAll(o => o.Guild == guild);
                if (players.Count == 0 || players[0].Disposed)
                {
                    message = "Error: The server is not playing any music!";
                }
                else if (!await HasPermissions(user, guild, channel))
                {
                    message = "You do not have the power to run this command!";
                }
                else
                {
                    await players[0].DisposeAsync();
                    message = "Successfully disconnected.";
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }

            return message;
        }

        public async Task<string> Skip(ulong user, ulong guild, IVoiceChannel channel)
        {
            string error;
            try
            {
                var players = _musicPlayers.FindAll(o => o.Guild == guild);
                if (players.Count == 0 || players[0].Disposed)
                {
                    error = "The server is not playing any music!";
                }
                else if (!await HasPermissions(user, guild, channel))
                {
                    error = "You do not have the power to run this command!";
                }
                else
                {
                    var skipMessage = players[0].TrySkip();
                    error = skipMessage;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return string.IsNullOrWhiteSpace(error) ? "" : "Error: " + error;
        }

        public async Task<string> Remove(ulong user, ulong guild, IVoiceChannel channel, int index)
        {
            string error;
            string songName = null;
            try
            {
                var players = _musicPlayers.FindAll(o => o.Guild == guild);
                if (players.Count == 0 || players[0].Disposed)
                {
                    error = "The server is not playing any music!";
                }
                else if (!await HasPermissions(user, guild, channel))
                {
                    error = "You do not have the power to run this command!";
                }
                else
                {
                    var skipMessage = players[0].TryRemove(index, out songName);
                    error = skipMessage;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            if (songName != null)
            {
                songName = songName.Replace("\\", "\\\\");
                songName = songName.Replace("`", "\\`");
            }

            return string.IsNullOrWhiteSpace(error) ? $"Removed ``{songName}`` successfully." : "Error: " + error;
        }

        public Tuple<Embed, string> GetMusicQueue(ulong guild, int page)
        {
            Embed list = null;
            string error = null;
            try
            {
                var players = _musicPlayers.FindAll(o => o.Guild == guild);
                if (players.Count == 0 || players[0].Disposed)
                {
                    error = "The server is not playing any music!";
                }
                else
                {
                    var player = players[0];
                    var result = _embed.DisplaySongList(player.NowPlaying, player.Songs, page);
                    if (result.Item1 == null)
                    {
                        error = "Invalid page number!";
                        if (result.Item2 > 1)
                            error += $" It should be between 1-{result.Item2}, inclusively.";
                        else
                            error += " There is only one page.";
                    }
                    else
                    {
                        list = result.Item1;
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return new Tuple<Embed, string>(list, error);
        }

        public Tuple<Embed, string> GetNowPlaying(ulong guild)
        {
            Embed list = null;
            string error = null;
            try
            {
                var players = _musicPlayers.FindAll(o => o.Guild == guild);
                if (players.Count == 0 || players[0].Disposed)
                {
                    error = "The server is not playing any music!";
                }
                else
                {
                    var player = players[0];
                    list = _embed.DisplayNowPlaying(player.NowPlaying, player.GetPosition());
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return new Tuple<Embed, string>(list, error);
        }

        private async Task<bool> HasPermissions(ulong caller, ulong guild, IAudioChannel audioChannel)
        {
            var users = await audioChannel.GetUsersAsync().FlattenAsync();

            var userFound = false;
            var userCount = 0;

            foreach (var user in users)
            {
                if (user.IsBot)
                    continue;
                userCount++;
                userFound |= user.Id == caller;
            }

            if (!userFound) return false;
            if (userCount == 1) return true;
            var userGuild = _client.GetGuild(guild).GetUser(caller);
            foreach (var role in userGuild.Roles)
            {
                var name = role.Name.ToLower().Trim();
                if (name == "dj" || name == "guardian")
                    return true;
            }

            return userGuild.GuildPermissions.ManageChannels || userGuild.GuildPermissions.Administrator;
        }
    }
}