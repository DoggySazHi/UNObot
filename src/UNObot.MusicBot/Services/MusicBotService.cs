using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using UNObot.Plugins.TerminalCore;
using Timer = System.Timers.Timer;

namespace UNObot.MusicBot.Services
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

        internal async Task Cache(YoutubeService youtube, LoggerService logger)
        {
            if (string.IsNullOrEmpty(PathCached) || !File.Exists(PathCached))
            {
                logger.Log(LogSeverity.Debug, $"Caching {Name}");
                PathCached = "Caching...";
                PathCached = await youtube.Download(Url, RequestedGuild);
                _endCache?.Set();
                logger.Log(LogSeverity.Debug, "Finished caching.");
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

    internal class Player : IAsyncDisposable
    {
        private readonly IVoiceChannel _audioChannel;
        private readonly ManualResetEvent _cacheEvent;
        private readonly int _cacheLength = 5;
        private readonly ISocketMessageChannel _messageChannel;
        private readonly ManualResetEvent _pauseEvent;
        private readonly Stopwatch _playPos;
        private readonly ManualResetEvent _quitEvent;
        private bool _disposed;
        private IAudioClient _audioClient;
        private Timer _autoDcTimer;
        private bool _caching;
        private Process _ffmpegProcess;

        private bool _handlingError;
        private bool _isPlaying;
        private bool _quit;

        private bool _skip;
        private CancellationTokenSource _stopAsync;

        private readonly LoggerService _logger;
        private readonly YoutubeService _youtube;
        private readonly EmbedService _embed;
        
        internal Player(LoggerService logger, YoutubeService youtube, EmbedService embed, ulong guild, IVoiceChannel audioChannel, IAudioClient audioClient,
            ISocketMessageChannel messageChannel)
        {
            _logger = logger;
            _youtube = youtube;
            _embed = embed;
            
            Guild = guild;
            _audioClient = audioClient;
            _audioChannel = audioChannel;
            _messageChannel = messageChannel;
            audioClient.Disconnected += FixConnection;
            _pauseEvent = new ManualResetEvent(false);
            _quitEvent = new ManualResetEvent(false);
            _cacheEvent = new ManualResetEvent(false);
            Songs = new List<Song>();
            _playPos = new Stopwatch();
        }

        internal ulong Guild { get; }
        internal List<Song> Songs { get; }
        internal Song NowPlaying { get; private set; }
        internal bool LoopingQueue { get; private set; }
        internal bool LoopingSong { get; private set; }

        internal bool Paused { get; private set; }
        internal bool Disposed => _disposed || _audioClient.ConnectionState == ConnectionState.Disconnected;

        private async Task FixConnection(Exception arg)
        {
            if (!_isPlaying || _handlingError)
                return;
            _handlingError = true;
            var prevPlaying = !Paused;

            if (prevPlaying)
                _logger.Log(LogSeverity.Debug, TryPause());
            if (!Disposed && _audioClient?.ConnectionState != ConnectionState.Connected)
            {
                _audioClient = await _audioChannel.ConnectAsync();
                _audioClient.Disconnected += FixConnection;
                await _messageChannel
                    .SendMessageAsync(
                        "Detected audio disconnection, reconnected. Use .playerdc to force the bot to leave.")
                    .ConfigureAwait(false);
                if (prevPlaying)
                {
                    _logger.Log(LogSeverity.Debug, TryPlay());
                    _logger.Log(LogSeverity.Debug, "Playing.");
                }
            }

            _handlingError = false;
        }

        internal async Task RunPlayer()
        {
            try
            {
                _logger.Log(LogSeverity.Debug, $"Player initialized for {Guild}");
                await FixConnection(null);
                _stopAsync = new CancellationTokenSource();

                while (Songs.Count != 0)
                {
                    NowPlaying = Songs[0];
                    Songs.RemoveAt(0);

                    _cacheEvent.Reset();
                    NowPlaying.SetCacheEvent(_cacheEvent);

                    await NowPlaying.Cache(_youtube, _logger).ConfigureAwait(false);
                    _logger.Log(LogSeverity.Debug, $"Songs: {Songs.Count}");

                    _cacheEvent.WaitOne();

                    var startTime = DateTime.Now;

                    while (!File.Exists(NowPlaying.PathCached) && (DateTime.Now - startTime).TotalSeconds < 5.0)
                    {
                        //ignored
                    }

                    if (!File.Exists(NowPlaying.PathCached))
                        await _messageChannel.SendMessageAsync("Sorry, but I had a problem downloading this song...")
                            .ConfigureAwait(false);
                    
                    do
                    {
                        _playPos.Restart();
                        var message = _skip ? "Skipped song." : "";
                        _skip = false;
                        await _messageChannel
                            .SendMessageAsync(message, false, _embed.DisplayNowPlaying(NowPlaying, null))
                            .ConfigureAwait(false);
                        // Runs a forever loop to quit when the quit boolean is true (if FFMPEG decides not to quit)
                        await SendAudio(CreateStream(NowPlaying.PathCached), _stopAsync.Token, _audioChannel.Bitrate);
                    } while (LoopingSong);

                    if (LoopingQueue)
                        Songs.Add(NowPlaying);
                    if (_quit)
                    {
                        _ffmpegProcess.Kill();
                        _ffmpegProcess = null;
                    }

                    if (Songs.All(o => o.PathCached != NowPlaying.PathCached))
                        File.Delete(NowPlaying.PathCached);
                    NowPlaying.PathCached = null;

                    NowPlaying = null;
#pragma warning disable 4014
                    Task.Run(Cache).ConfigureAwait(false);
#pragma warning restore 4014
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogSeverity.Error, "MusicBot has encountered a fatal error and needs to quit.", ex);
                await _messageChannel.SendMessageAsync(
                    "Sorry, but I have encountered an error in the player's core. Please note this is a beta, sorry.");
            }
            finally
            {
                await DisposeAsync();
            }
        }

        private async Task Cache()
        {
            try
            {
                if (_caching)
                    return;
                _caching = true;
                var filesCached = new List<string>();
                for (var i = 0; i < Math.Min(Songs.Count, _cacheLength); i++)
                {
                    var s = Songs[i];
                    if (i < Math.Min(Songs.Count, _cacheLength))
                    {
                        if (string.IsNullOrWhiteSpace(s.PathCached))
                            await s.Cache(_youtube, _logger).ConfigureAwait(true);
                        filesCached.Add(s.PathCached);
                    }
                    else
                    {
                        s.PathCached = null;
                    }
                }

                // TODO fix.
                _youtube.DeleteGuildFolder(Guild, filesCached.ToArray());
                _caching = false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogSeverity.Debug, "Player Cache has encountered an error.", ex);
            }
        }

        internal void Add(string url, Tuple<string, string, string> data, ulong user, ulong guildFrom,
            bool insertAtTop = false)
        {
            var s = new Song(url, data, user, guildFrom);
            if (insertAtTop)
                Songs.Insert(0, s);
            else
                Songs.Add(s);
            Task.Run(Cache);
        }

        internal string TryPause()
        {
            if (Songs.Count == 0 && NowPlaying == null)
                return "There is no song playing.";
            if (Paused || !_pauseEvent.WaitOne(0))
            {
                Paused = true;
                return "Player is already paused.";
            }

            Paused = true;
            _pauseEvent.Reset();
            return null;
        }

        internal string TryPlay()
        {
            if (Songs.Count == 0 && NowPlaying == null)
                return "There is no song playing.";
            if (!Paused || _pauseEvent.WaitOne(0))
            {
                Paused = false;
                return "Player is already playing.";
            }

            Paused = false;
            _pauseEvent.Set();
            return null;
        }

        internal string TrySkip()
        {
            if (NowPlaying == null)
                return "There is no song playing.";
            Paused = false;
            _pauseEvent.Set();

            _quit = true;
            _skip = true;
            _quitEvent.WaitOne();
            _quitEvent.Reset();
            _pauseEvent.Reset();
            _quit = false;
            return null;
        }

        internal string TryRemove(int index, out string songName)
        {
            if (index < 1 || index > Songs.Count)
            {
                songName = null;
                return "Song is out of bounds!";
            }

            songName = Songs[index - 1].Name;
            Songs.RemoveAt(index - 1);
            return null;
        }

        internal string ToggleLoopSong()
        {
            if (NowPlaying == null && Songs.Count == 0)
                return "There is no song playing.";

            LoopingSong = !LoopingSong;
            LoopingQueue = false;
            return (LoopingSong ? "Enabled" : "Disabled") + " song loop.";
        }

        internal string ToggleLoopPlaylist()
        {
            if (NowPlaying == null && Songs.Count == 0)
                return "There are no songs in the queue.";

            LoopingSong = false;
            LoopingQueue = !LoopingQueue;

            return (LoopingQueue ? "Enabled" : "Disabled") + " queue loop.";
        }

        internal void Shuffle()
        {
            Songs.ForEach(o => o.PathCached = null);
            Songs.Shuffle();
            // Let the Cacher delete, as if you try to kill the process too early, it throws an exception while caching.
            Task.Run(Cache);
        }

        private async Task SendAudio(Stream audioStream, CancellationToken ct, int bitrate)
        {
            _logger.Log(LogSeverity.Debug, $"Audio stream created at bit rate {bitrate}");
            _isPlaying = true;
            var discordStream = _audioClient.CreatePCMStream(AudioApplication.Music, bitrate);


            //Adjust?
            var bufferSize = 1024;
            var buffer = new byte[bufferSize];
            //int bytesSent = 0;
            var fail = false;

            // For the warning log.
            var failToWrite = false;

            // Skip: User skipped the song.
            // Fail: Failed to read, kill the song.
            // Exit: Song ended.
            // Quit: Program exiting.

            while (!fail && !_quit)
            {
                var sw = new Stopwatch();
                try
                {
                    if (_audioClient.ConnectionState == ConnectionState.Disconnected)
                    {
                        _audioClient = await _audioChannel.ConnectAsync();
                        _audioClient.Disconnected += FixConnection;
                    }

                    sw.Restart();
                    var read = await audioStream.ReadAsync(buffer, 0, bufferSize, ct);
                    sw.Stop();
                    if (sw.ElapsedMilliseconds > 1000)
                        _logger.Log(LogSeverity.Warning,
                            $"Took too lsong to read from disk! Is the server lagging? Delay of {sw.ElapsedMilliseconds}ms.");
                    if (read == 0)
                        break;

                    try
                    {
                        sw.Restart();
                        await discordStream.WriteAsync(buffer, 0, read, ct);
                        sw.Stop();
                        if (sw.ElapsedMilliseconds > 1000)
                            _logger.Log(LogSeverity.Warning,
                                $"Took too long to write to Discord! Is the server lagging? Delay of {sw.ElapsedMilliseconds}ms.");
                        if (failToWrite)
                        {
                            failToWrite = false;
                            _logger.Log(LogSeverity.Info, "Successfully reconnected.");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (!failToWrite && !ct.IsCancellationRequested)
                        {
                            failToWrite = true;
                            _logger.Log(LogSeverity.Error,
                                "Failed to write! Attempting to repair Discord service.");
                            discordStream = _audioClient.CreatePCMStream(AudioApplication.Music, _audioChannel.Bitrate);
                        }
                    }

                    if (Paused)
                    {
                        _pauseEvent.Reset();
                        _playPos.Stop();
                        _pauseEvent.WaitOne();
                        _playPos.Start();
                    }

                    //bytesSent += read;
                }
                catch (Exception ex)
                {
                    _logger.Log(LogSeverity.Error, "Error while writing a song from FFMPEG!", ex);
                    fail = true;
                }
            }

            _playPos.Stop();
            // ReSharper disable twice MethodSupportsCancellation
            await discordStream.FlushAsync();
            Paused = false;
            await audioStream.FlushAsync();
            _isPlaying = false;
            if (_quit)
            {
                _quitEvent.Set();
                _quit = false;
            }

            await audioStream.DisposeAsync();
            await discordStream.DisposeAsync();
            _logger.Log(LogSeverity.Debug, "Audio stream successfully destroyed.");
        }

        internal string GetPosition()
        {
            return YoutubeService.TimeString(_playPos.Elapsed);
        }

        internal async Task CheckOnJoin()
        {
            _logger.Log(LogSeverity.Debug, "Detected someone joining a channel.");
            var users = (await _audioChannel.GetUsersAsync().FlattenAsync()).ToList();
            var isFilled = !users.All(o => o.IsBot) && users.Count >= 2;
            if (isFilled && _autoDcTimer != null)
            {
                _logger.Log(LogSeverity.Debug, "Destroyed DC Timer.");
                _autoDcTimer?.Dispose();
                _autoDcTimer = null;
            }
        }

        internal async Task CheckOnLeave()
        {
            var users = (await _audioChannel.GetUsersAsync().FlattenAsync()).ToList();
            var isEmpty = users.All(o => o.IsBot) || users.Count <= 1;
            if (isEmpty && _autoDcTimer == null)
            {
                _logger.Log(LogSeverity.Debug, "Started DC timer.");
                _autoDcTimer = new Timer
                {
                    Interval = 60 * 1000,
                    AutoReset = false,
                    Enabled = true
                };
                _autoDcTimer.Elapsed += async (sender, args) => { await DisposeAsync(); };
            }
        }

        private Stream CreateStream(string path)
        {
            var fileName = "/usr/local/bin/ffmpeg";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                fileName = @"C:\Users\William Le\Documents\Programming Projects\YTDownloader\ffmpeg.exe";
            _ffmpegProcess = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            return _ffmpegProcess?.StandardOutput.BaseStream;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (Songs.Count != 0)
                    _logger.Log(LogSeverity.Warning, "Attempted to dispose Music service with songs!");
                Songs.Clear();
                LoopingSong = false;
                LoopingQueue = false;
                _skip = false;
                Paused = false;
                _quit = true;
                //StopAsync.Cancel();
                _ffmpegProcess?.Kill(true);
                if (_isPlaying)
                    _quitEvent.WaitOne();
                await _audioClient.StopAsync();
                try
                {
                    await _audioChannel.DisconnectAsync();
                }
                catch (Exception)
                {
                    /* ignored */
                }

                _audioClient.Dispose();
                _pauseEvent.Dispose();
                _stopAsync.Dispose();
                _autoDcTimer?.Dispose();
            }
            catch (Exception e)
            {
                _logger.Log(LogSeverity.Error, "Failed to dispose MusicBot!", e);
            }
            finally
            {
                _disposed = true;
                _logger.Log(LogSeverity.Debug, $"Disposed {Guild}");
            }
        }
    }

    internal class MusicBotService
    {
        private readonly List<Player> _musicPlayers = new List<Player>();
        private readonly LoggerService _logger;
        private readonly YoutubeService _youtube;
        private readonly DiscordSocketClient _client;
        private readonly EmbedService _embed;

        public MusicBotService(LoggerService logger, YoutubeService youtube, DiscordSocketClient client, EmbedService embed)
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

        internal async ValueTask DisposeAsync()
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
#pragma warning disable 4014
                Task.Run(newPlayer.RunPlayer);
#pragma warning restore 4014
                _logger.Log(LogSeverity.Debug, "Generated new player.");
                return new Tuple<Player, string>(newPlayer, null);
            }

            if (_musicPlayers[player].Disposed)
            {
                _logger.Log(LogSeverity.Debug, "Replaced player.");
                _musicPlayers.RemoveAt(player);
                var newPlayer = new Player(_logger, _youtube, _embed, guild, audioChannel, await audioChannel.ConnectAsync(), messageChannel);
                _musicPlayers.Add(newPlayer);
#pragma warning disable 4014
                Task.Run(newPlayer.RunPlayer);
#pragma warning restore 4014
                return new Tuple<Player, string>(newPlayer, null);
            }

            _logger.Log(LogSeverity.Debug, "Returned existing player.");
            return new Tuple<Player, string>(_musicPlayers[player], null);
        }

        internal async Task<Tuple<Embed, string>> Add(ulong user, ulong guild, string url, IVoiceChannel channel,
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

        internal async Task<Tuple<Embed, string>> AddList(ulong user, ulong guild, string url, IVoiceChannel channel,
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

        internal async Task<Tuple<Embed, string>> Search(ulong user, ulong guild, string query, IVoiceChannel channel,
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

        internal async Task<string> Pause(ulong user, ulong guild, IAudioChannel channel)
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

        internal async Task<string> Play(ulong user, ulong guild, IAudioChannel channel)
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

        internal async Task<string> Shuffle(ulong user, ulong guild, IAudioChannel channel)
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

        internal async Task<string> ToggleLoop(ulong user, ulong guild, IAudioChannel channel)
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

        internal async Task<string> ToggleLoopQueue(ulong user, ulong guild, IAudioChannel channel)
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

        internal async Task<string> Disconnect(ulong user, ulong guild, IAudioChannel channel)
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

        internal async Task<string> Skip(ulong user, ulong guild, IVoiceChannel channel)
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

        internal async Task<string> Remove(ulong user, ulong guild, IVoiceChannel channel, int index)
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

        internal Tuple<Embed, string> GetMusicQueue(ulong guild, int page)
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

        internal Tuple<Embed, string> GetNowPlaying(ulong guild)
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