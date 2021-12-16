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
using UNObot.MusicBot.Services;
using UNObot.Plugins;
using UNObot.Plugins.TerminalCore;
using Timer = System.Timers.Timer;

namespace UNObot.MusicBot.MusicCore;

public class Player : IAsyncDisposable
{
    private const int CacheLength = 5;

    private readonly IVoiceChannel _audioChannel;
    private readonly ManualResetEvent _cacheEvent;
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
    private bool _hasInitialized;

    private bool _skip;
    private CancellationTokenSource _stopAsync;

    private readonly ILogger _logger;
    private readonly YoutubeService _youtube;
    private readonly EmbedService _embed;
        
    public Player(ILogger logger, YoutubeService youtube, EmbedService embed, ulong guild, IVoiceChannel audioChannel, IAudioClient audioClient,
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

    public ulong Guild { get; }
    public List<Song> Songs { get; }
    public Song NowPlaying { get; private set; }
    public bool LoopingQueue { get; private set; }
    public bool LoopingSong { get; private set; }

    public bool Paused { get; private set; }
    public bool Disposed => _disposed || _audioClient.ConnectionState == ConnectionState.Disconnected;

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

    private async Task RunPlayer()
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

                //await NowPlaying.Cache(_youtube, _logger).ConfigureAwait(false);
                _logger.Log(LogSeverity.Debug, $"Songs: {Songs.Count}");

                _cacheEvent.WaitOne(10000);

                var startTime = DateTime.Now;

                while (!File.Exists(NowPlaying.PathCached) && (DateTime.Now - startTime).TotalSeconds < 5.0)
                {
                    //ignored
                }

                if (!File.Exists(NowPlaying.PathCached))
                    await _messageChannel.SendMessageAsync("Sorry, but I had a problem downloading this song...")
                        .ConfigureAwait(false);
                else do
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
                {
                    for (var i = 1; i <= 3; i++)
                    {
                        try
                        {
                            if(File.Exists(NowPlaying.PathCached))
                                File.Delete(NowPlaying.PathCached);
                            break;
                        }
                        catch (IOException e)
                        {
                            _logger.Log(LogSeverity.Error, $"Could not delete file! Attempt {i}/3...", e);
                        }
                    }
                }

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
                "Sorry, but I have encountered an error in the player's core. Please note this is a beta.");
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
            for (var i = 0; i < Math.Min(Songs.Count, CacheLength); i++)
            {
                var s = Songs[i];
                if (i < Math.Min(Songs.Count, CacheLength))
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
            if (NowPlaying != null)
                filesCached.Add(NowPlaying.PathCached);
            _youtube.DeleteGuildFolder(Guild, filesCached.ToArray());
            _caching = false;
        }
        catch (Exception ex)
        {
            _logger.Log(LogSeverity.Debug, "Player Cache has encountered an error.", ex);
        }
    }

    public void Add(string url, Tuple<string, string, string> data, ulong user, ulong guildFrom,
        bool insertAtTop = false)
    {
        if (!_hasInitialized)
        {
            Task.Run(RunPlayer);
            _hasInitialized = true;
        }
            
        var s = new Song(url, data, user, guildFrom);
        if (insertAtTop)
            Songs.Insert(0, s);
        else
            Songs.Add(s);
        Task.Run(Cache);
    }

    public string TryPause()
    {
        if (Songs.Count == 0 && NowPlaying == null)
            return "There is no song playing.";
            
        Paused = true;
        _pauseEvent.Reset();
        return null;
    }

    public string TryPlay()
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

    public string TrySkip()
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

    public string TryRemove(int index, out string songName)
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

    public string ToggleLoopSong()
    {
        if (NowPlaying == null && Songs.Count == 0)
            return "There is no song playing.";

        LoopingSong = !LoopingSong;
        LoopingQueue = false;
        return (LoopingSong ? "Enabled" : "Disabled") + " song loop.";
    }

    public string ToggleLoopPlaylist()
    {
        if (NowPlaying == null && Songs.Count == 0)
            return "There are no songs in the queue.";

        LoopingSong = false;
        LoopingQueue = !LoopingQueue;

        return (LoopingQueue ? "Enabled" : "Disabled") + " queue loop.";
    }

    public void Shuffle()
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
                        $"Took too long to read from disk! Is the server lagging? Delay of {sw.ElapsedMilliseconds}ms.");
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
                        discordStream = _audioClient.CreatePCMStream(AudioApplication.Mixed, _audioChannel.Bitrate);
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

    public string GetPosition()
    {
        return YoutubeService.TimeString(_playPos.Elapsed);
    }

    public async Task CheckOnJoin()
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

    public async Task CheckOnLeave()
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
            _autoDcTimer.Elapsed += (_, _) => { DisposeAsync().GetAwaiter().GetResult(); };
        }
    }

    private Stream CreateStream(string path)
    {
        var fileName = "/usr/local/bin/ffmpeg";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            fileName = "ffmpeg.exe";
        // var args = $"-hide_banner -re -loglevel panic -i \"{path}\" -ac 2 -b:a {_audioChannel.Bitrate} -f s16le pipe:1";
        var args = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1";
        _logger.Log(LogSeverity.Verbose, args);
        _ffmpegProcess = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
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