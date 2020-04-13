using Discord;
using Discord.Audio;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode.Videos;
using Timer = System.Timers.Timer;

namespace UNObot.Services
{
    // Can't use Struct, needs passing by reference.
    public class Song
    {
        public string URL { get; }
        public string PathCached { get; set; }
        public ulong RequestedBy { get; }
        public ulong RequestedGuild { get; }
        public string Name { get; }
        public string Duration { get; }
        public string ThumbnailURL { get; }
        public bool IsPlaying { get; private set; }

        private ManualResetEvent EndCache;

        public Song(string URL, Tuple<string, string, string> Data, ulong User, ulong Guild)
        {
            this.URL = URL;
            Name = Data.Item1;
            Duration = Data.Item2;
            ThumbnailURL = Data.Item3;
            RequestedBy = User;
            RequestedGuild = Guild;
            IsPlaying = false;
        }

        public async Task Cache()
        {
            if (string.IsNullOrEmpty(PathCached) || !File.Exists(PathCached))
            {
                LoggerService.Log(LogSeverity.Debug, $"Caching {Name}");
                PathCached = "Caching...";
                PathCached = await YoutubeService.GetSingleton().Download(URL, RequestedGuild);
                EndCache?.Set();
                LoggerService.Log(LogSeverity.Debug, "Finished caching.");
            }
        }

        public void SetCacheEvent(ManualResetEvent CacheFinished)
        {
            if (!string.IsNullOrEmpty(PathCached) && PathCached != "Caching...")
            {
                CacheFinished.Set();
                return;
            }
            EndCache = CacheFinished;
        }
        public void SetPlaying() => IsPlaying = true;
    }

    public class Player : IAsyncDisposable
    {
        public ulong Guild { get; private set; }
        public List<Song> Songs { get; private set; }
        public Song NowPlaying { get; private set; }
        public bool LoopingQueue { get; private set; }
        public bool LoopingSong { get; private set; }

        public bool Paused { get; private set; }
        private bool _Disposed;
        public bool Disposed => _Disposed || AudioClient.ConnectionState == ConnectionState.Disconnected;

        private bool Skip;
        private bool Quit;
        private bool IsPlaying;
        private bool Caching;
        private readonly int CacheLength = 5;
        private readonly ManualResetEvent PauseEvent;
        private readonly ManualResetEvent QuitEvent;
        private readonly ManualResetEvent CacheEvent;
        private readonly Stopwatch PlayPos;
        private readonly IVoiceChannel AudioChannel;
        private IAudioClient AudioClient;
        private readonly ISocketMessageChannel MessageChannel;
        private Timer autoDCTimer;
        private Process ffmpegProcess;
        private CancellationTokenSource StopAsync;

        public Player(ulong Guild, IVoiceChannel AudioChannel, IAudioClient AudioClient, ISocketMessageChannel MessageChannel)
        {
            this.Guild = Guild;
            this.AudioClient = AudioClient;
            this.AudioChannel = AudioChannel;
            this.MessageChannel = MessageChannel;
            AudioClient.Disconnected += FixConnection;
            PauseEvent = new ManualResetEvent(false);
            QuitEvent = new ManualResetEvent(false);
            CacheEvent = new ManualResetEvent(false);
            Songs = new List<Song>();
            PlayPos = new Stopwatch();
        }

        private bool HandlingError;
        private async Task FixConnection(Exception arg)
        {
            if (!IsPlaying || HandlingError)
                return;
            HandlingError = true;
            var PrevPlaying = !Paused;

            if (PrevPlaying)
                LoggerService.Log(LogSeverity.Debug, TryPause());
            if (!Disposed && AudioClient?.ConnectionState != ConnectionState.Connected)
            {
                AudioClient = await AudioChannel.ConnectAsync();
                AudioClient.Disconnected += FixConnection;
                await MessageChannel.SendMessageAsync("Detected audio disconnection, reconnected. Use .playerdc to force the bot to leave.").ConfigureAwait(false);
                if (PrevPlaying)
                {
                    LoggerService.Log(LogSeverity.Debug, TryPlay());
                    LoggerService.Log(LogSeverity.Debug, "Playing.");
                }
            }
            HandlingError = false;
        }

        public async Task RunPlayer()
        {
            try
            {
                LoggerService.Log(LogSeverity.Debug, $"Player initialized for {Guild}");
                await FixConnection(null);
                StopAsync = new CancellationTokenSource();

                while (Songs.Count != 0)
                {
                    NowPlaying = Songs[0];
                    Songs.RemoveAt(0);

                    CacheEvent.Reset();
                    NowPlaying.SetCacheEvent(CacheEvent);

                    await NowPlaying.Cache().ConfigureAwait(false);
                    LoggerService.Log(LogSeverity.Debug, $"Songs: {Songs.Count}");

                    CacheEvent.WaitOne();

                    var StartTime = DateTime.Now;

                    while (!File.Exists(NowPlaying.PathCached) && (DateTime.Now - StartTime).TotalSeconds < 5.0)
                    {
                        //ignored
                    }

                    if (!File.Exists(NowPlaying.PathCached))
                            await MessageChannel.SendMessageAsync("Sorry, but I had a problem downloading this song...")
                                .ConfigureAwait(false);

                    NowPlaying.SetPlaying();

                    do
                    {
                        PlayPos.Restart();
                        string Message = Skip ? "Skipped song." : "";
                        Skip = false;
                        await MessageChannel
                            .SendMessageAsync(Message, false, EmbedDisplayService.DisplayNowPlaying(NowPlaying, null))
                            .ConfigureAwait(false);
                        // Runs a forever loop to quit when the quit boolean is true (if FFMPEG decides not to quit)
                        await SendAudio(CreateStream(NowPlaying.PathCached, AudioChannel.Bitrate), StopAsync.Token);
                    } while (LoopingSong);

                    if (LoopingQueue)
                        Songs.Add(NowPlaying);
                    if (Quit)
                    {
                        ffmpegProcess.Kill();
                        ffmpegProcess = null;
                    }
                    if(Songs.All(o => o.PathCached != NowPlaying.PathCached))
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
                LoggerService.Log(LogSeverity.Error, "MusicBot has encountered a fatal error and needs to quit.", ex);
                await MessageChannel.SendMessageAsync("Sorry, but I have encountered an error in the player's core. Please note this is a beta, sorry.");
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
                if (Caching)
                    return;
                Caching = true;
                var FilesCached = new List<string>();
                for (var i = 0; i < Math.Min(Songs.Count, CacheLength); i++)
                {
                    var s = Songs[i];
                    if (i < Math.Min(Songs.Count, CacheLength))
                    {
                        
                        if (string.IsNullOrWhiteSpace(s.PathCached))
                            await s.Cache().ConfigureAwait(true);
                        FilesCached.Add(s.PathCached);
                    }
                    else
                    {
                        s.PathCached = null;
                    }
                }
                // TODO fix.
                YoutubeService.GetSingleton().DeleteGuildFolder(Guild, FilesCached.ToArray());
                Caching = false;
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Debug, "Player Cache has encountered an error.", ex);
            }
        }

        public void Add(string URL, Tuple<string, string, string> Data, ulong User, ulong GuildFrom, bool InsertAtTop = false)
        {
            Song s = new Song(URL, Data, User, GuildFrom);
            if (InsertAtTop)
                Songs.Insert(0, s);
            else
                Songs.Add(s);
            Task.Run(Cache);
        }

        public string TryPause()
        {
            if (Songs.Count == 0 && NowPlaying == null)
                return "There is no song playing.";
            if (Paused || !PauseEvent.WaitOne(0))
            {
                Paused = true;
                return "Player is already paused.";
            }
            Paused = true;
            PauseEvent.Reset();
            return null;
        }

        public string TryPlay()
        {
            if (Songs.Count == 0 && NowPlaying == null)
                return "There is no song playing.";
            if (!Paused || PauseEvent.WaitOne(0))
            {
                Paused = false;
                return "Player is already playing.";
            }
            Paused = false;
            PauseEvent.Set();
            return null;
        }

        public string TrySkip()
        {
            if (NowPlaying == null)
                return "There is no song playing.";
            Paused = false;
            PauseEvent.Set();

            Quit = true;
            Skip = true;
            QuitEvent.WaitOne();
            QuitEvent.Reset();
            PauseEvent.Reset();
            Quit = false;
            return null;
        }

        public string TryRemove(int Index, out string SongName)
        {
            if (Index < 1 || Index > Songs.Count)
            {
                SongName = null;
                return "Song is out of bounds!";
            }
            SongName = Songs[Index - 1].Name;
            Songs.RemoveAt(Index - 1);
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
            ThreadSafeRandom.Shuffle(Songs);
            // Let the Cacher delete, as if you try to kill the process too early, it throws an exception while caching.
            Task.Run(Cache);
        }

        private async Task SendAudio(Stream AudioStream, CancellationToken ct)
        {
            LoggerService.Log(LogSeverity.Debug, $"Audio stream created at bit rate {AudioChannel.Bitrate}");
            IsPlaying = true;
            var DiscordStream = AudioClient.CreatePCMStream(AudioApplication.Music, AudioChannel.Bitrate);
            

            //Adjust?
            var BufferSize = 1024;
            var Buffer = new byte[BufferSize];
            //int bytesSent = 0;
            var Fail = false;

            // For the warning log.
            var FailToWrite = false;

            // Skip: User skipped the song.
            // Fail: Failed to read, kill the song.
            // Exit: Song ended.
            // Quit: Program exiting.

            while (!Fail && !Quit)
            {
                var sw = new Stopwatch();
                try
                {
                    if (AudioClient.ConnectionState == ConnectionState.Disconnected)
                    {
                        AudioClient = await AudioChannel.ConnectAsync();
                        AudioClient.Disconnected += FixConnection;
                    }

                    sw.Restart();
                    var read = await AudioStream.ReadAsync(Buffer, 0, BufferSize, ct);
                    sw.Stop();
                    if(sw.ElapsedMilliseconds > 1000)
                        LoggerService.Log(LogSeverity.Warning, $"Took too lsong to read from disk! Is the server lagging? Delay of {sw.ElapsedMilliseconds}ms.");
                    if (read == 0)
                        break;

                    try
                    {
                        sw.Restart();
                        await DiscordStream.WriteAsync(Buffer, 0, read, ct);
                        sw.Stop();
                        if(sw.ElapsedMilliseconds > 1000)
                            LoggerService.Log(LogSeverity.Warning, $"Took too long to write to Discord! Is the server lagging? Delay of {sw.ElapsedMilliseconds}ms.");
                        if (FailToWrite)
                        {
                            FailToWrite = false;
                            LoggerService.Log(LogSeverity.Info, "Successfully reconnected.");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (!FailToWrite && !ct.IsCancellationRequested)
                        {
                            FailToWrite = true;
                            LoggerService.Log(LogSeverity.Error,
                                "Failed to write! Attempting to repair Discord service.");
                            DiscordStream = AudioClient.CreatePCMStream(AudioApplication.Music, AudioChannel.Bitrate);
                        }
                    }

                    if (Paused)
                    {
                        PauseEvent.Reset();
                        PlayPos.Stop();
                        PauseEvent.WaitOne();
                        PlayPos.Start();
                    }

                    //bytesSent += read;
                }
                catch (Exception ex)
                {
                    LoggerService.Log(LogSeverity.Error, "Error while writing a song from FFMPEG!", ex);
                    Fail = true;
                }
            }
            PlayPos.Stop();
            // ReSharper disable twice MethodSupportsCancellation
            await DiscordStream.FlushAsync();
            Paused = false;
            await AudioStream.FlushAsync();
            IsPlaying = false;
            if (Quit)
            {
                QuitEvent.Set();
                Quit = false;
            }

            await AudioStream.DisposeAsync();
            await DiscordStream.DisposeAsync();
            LoggerService.Log(LogSeverity.Debug, "Audio stream successfully destroyed.");
        }

        public string GetPosition()
        {
            return YoutubeService.TimeString(PlayPos.Elapsed);
        }

        public async Task CheckOnJoin()
        {
            LoggerService.Log(LogSeverity.Debug, "Detected someone joining a channel.");
            var Users = (await AudioChannel.GetUsersAsync().FlattenAsync()).ToList();
            var IsFilled = !Users.All(o => o.IsBot) && Users.Count >= 2;
            if (IsFilled && autoDCTimer != null)
            {
                LoggerService.Log(LogSeverity.Debug, "Destroyed DC Timer.");
                autoDCTimer?.Dispose();
                autoDCTimer = null;
            }
        }

        public async Task CheckOnLeave()
        {
            var Users = (await AudioChannel.GetUsersAsync().FlattenAsync()).ToList();
            var IsEmpty = Users.All(o => o.IsBot) || Users.Count <= 1;
            if (IsEmpty && autoDCTimer == null)
            {
                LoggerService.Log(LogSeverity.Debug, "Started DC timer.");
                autoDCTimer = new Timer
                {
                    Interval = 60 * 1000,
                    AutoReset = false,
                    Enabled = true
                };
                autoDCTimer.Elapsed += async (sender, args) =>
                {
                    await DisposeAsync();
                };
            }
        }

        private Stream CreateStream(string path, int bitrate)
        {
            string FileName = "/usr/local/bin/ffmpeg";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                FileName = @"C:\Users\William Le\Documents\Programming Projects\YTDownloader\ffmpeg.exe";
            }
            ffmpegProcess = Process.Start(new ProcessStartInfo
            {
                FileName = FileName,
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -b:a {bitrate} pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            return ffmpegProcess?.StandardOutput.BaseStream;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if(Songs.Count != 0)
                    LoggerService.Log(LogSeverity.Warning, "Attempted to dispose Music service with songs!");
                Songs.Clear();
                LoopingSong = false;
                LoopingQueue = false;
                Skip = false;
                Paused = false;
                Quit = true;
                //StopAsync.Cancel();
                ffmpegProcess?.Kill(true);
                if (IsPlaying)
                    QuitEvent.WaitOne();
                await AudioClient.StopAsync();
                try { await AudioChannel.DisconnectAsync(); } catch (Exception) { /* ignored */ }
                AudioClient.Dispose();
                PauseEvent.Dispose();
                StopAsync.Dispose();
                autoDCTimer?.Dispose();
            }
            catch (Exception e)
            {
                LoggerService.Log(LogSeverity.Error, "Failed to dispose MusicBot!", e);
            }
            finally
            {
                _Disposed = true;
                LoggerService.Log(LogSeverity.Debug, $"Disposed {Guild}");
            }
        }
    }

    public class MusicBotService
    {
        private static MusicBotService Instance;
        private readonly List<Player> MusicPlayers = new List<Player>();

        private MusicBotService()
        {
            Program._client.UserVoiceStateUpdated += UserVoiceStateUpdated;
        }

        private async Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            if (oldState.VoiceChannel != null && newState.VoiceChannel == null)
            {
                foreach (var Player in MusicPlayers)
                    await Player.CheckOnLeave().ConfigureAwait(false);
            }
            else if (oldState.VoiceChannel == null && newState.VoiceChannel != null)
            {
                foreach (var Player in MusicPlayers)
                    await Player.CheckOnJoin().ConfigureAwait(false);
            }
        }

        public static MusicBotService GetSingleton()
        {
            if (Instance == null)
                Instance = new MusicBotService();
            return Instance;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (Player MusicPlayer in MusicPlayers)
                await MusicPlayer.DisposeAsync();
        }

        private async Task<Tuple<Player, string>> ConnectAsync(ulong Guild, IVoiceChannel AudioChannel, ISocketMessageChannel MessageChannel)
        {
            var Player = MusicPlayers.FindIndex(o => o.Guild == Guild);
            if (Player < 0)
            {
                Player NewPlayer = new Player(Guild, AudioChannel, await AudioChannel.ConnectAsync(), MessageChannel);
                MusicPlayers.Add(NewPlayer);
#pragma warning disable 4014
                Task.Run(NewPlayer.RunPlayer);
#pragma warning restore 4014
                LoggerService.Log(LogSeverity.Debug, "Generated new player.");
                return new Tuple<Player, string>(NewPlayer, null);
            }
            if (MusicPlayers[Player].Disposed)
            {
                LoggerService.Log(LogSeverity.Debug, "Replaced player.");
                MusicPlayers.RemoveAt(Player);
                var NewPlayer = new Player(Guild, AudioChannel, await AudioChannel.ConnectAsync(), MessageChannel);
                MusicPlayers.Add(NewPlayer);
#pragma warning disable 4014
                Task.Run(NewPlayer.RunPlayer);
#pragma warning restore 4014
                return new Tuple<Player, string>(NewPlayer, null);
            }
            LoggerService.Log(LogSeverity.Debug, "Returned existing player.");
            return new Tuple<Player, string>(MusicPlayers[Player], null);
        }

        public async Task<Tuple<Embed, string>> Add(ulong User, ulong Guild, string URL, IVoiceChannel Channel, ISocketMessageChannel MessageChannel, bool InsertAtTop = false)
        {
            Embed EmbedOut;
            string Error = null;
            if (InsertAtTop && !await HasPermissions(User, Guild, Channel))
                return new Tuple<Embed, string>(null, "You do not have the power to run this command!");
            try
            {
                var Information = YoutubeService.GetSingleton().GetInfo(URL);
                var Result = EmbedDisplayService.DisplayAddSong(User, Guild, URL, await Information);
                EmbedOut = Result.Item1;
                var Data = Result.Item2;
                var Player = await ConnectAsync(Guild, Channel, MessageChannel);
                if (Player.Item2 != null)
                    Error = Player.Item2;
                else
                    Player.Item1.Add(URL, Data, User, Guild, InsertAtTop);
            }
            catch (Exception ex)
            {
                return new Tuple<Embed, string>(null, ex.Message);
            }

            return new Tuple<Embed, string>(EmbedOut, Error);
        }

        public async Task<Tuple<Embed, string>> AddList(ulong User, ulong Guild, string URL, IVoiceChannel Channel, ISocketMessageChannel MessageChannel, bool InsertAtTop = false)
        {
            Embed Display = null;
            string Message;
            if (InsertAtTop && !await HasPermissions(User, Guild, Channel))
                return new Tuple<Embed, string>(null, "You do not have the power to run this command!");
            try
            {
                var Playlist = await EmbedDisplayService.DisplayPlaylist(User, Guild, URL);
                Display = Playlist.Item1;
                var ResultPlay = await YoutubeService.GetSingleton().GetPlaylistVideos(Playlist.Item2.Id);
                var Player = await ConnectAsync(Guild, Channel, MessageChannel);
                if (Player.Item2 != null)
                    Message = Player.Item2;
                else
                {
                    if (InsertAtTop)
                        for (int i = ResultPlay.Count - 1; i >= 0; i--)
                        {
                            Video Video = ResultPlay[i];
                            Player.Item1.Add($"https://www.youtube.com/watch?v={Video.Id}",
                                new Tuple<string, string, string>(Video.Title, YoutubeService.TimeString(Video.Duration), Video.Thumbnails.MediumResUrl), User, Guild, true);
                        }
                    else
                        foreach (var Video in ResultPlay)
                            Player.Item1.Add($"https://www.youtube.com/watch?v={Video.Id}",
                                new Tuple<string, string, string>(Video.Title, YoutubeService.TimeString(Video.Duration), Video.Thumbnails.MediumResUrl), User, Guild);
                    Message = $"Added {ResultPlay.Count} song{(ResultPlay.Count == 1 ? "" : "s")}.";
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }

            return new Tuple<Embed, string>(Display, Message);
        }

        public async Task<Tuple<Embed, string>> Search(ulong User, ulong Guild, string Query, IVoiceChannel Channel, ISocketMessageChannel MessageChannel, bool InsertAtTop = false)
        {
            Embed EmbedOut;
            string Error = null;
            if (InsertAtTop && !await HasPermissions(User, Guild, Channel))
                return new Tuple<Embed, string>(null, "You do not have the power to run this command!");
            try
            {
                var Information = await YoutubeService.GetSingleton().SearchVideo(Query);
                var Result = EmbedDisplayService.DisplayAddSong(User, Guild, Information.Item2, Information.Item1);
                EmbedOut = Result.Item1;
                var Data = Result.Item2;
                var Player = await ConnectAsync(Guild, Channel, MessageChannel);
                if (Player.Item2 != null)
                    Error = Player.Item2;
                else
                    Player.Item1.Add(Information.Item2, Data, User, Guild, InsertAtTop);
            }
            catch (Exception ex)
            {
                return new Tuple<Embed, string>(null, ex.Message);
            }

            return new Tuple<Embed, string>(EmbedOut, Error);
        }

        public async Task<string> Pause(ulong User, ulong Guild, IAudioChannel Channel)
        {
            string Message;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Message = "Error: The server is not playing any music!";
                else if (!await HasPermissions(User, Guild, Channel))
                    Message = "You do not have the power to run this command!";
                else
                {
                    string SkipMessage = Players[0].TryPause();
                    if (!string.IsNullOrEmpty(SkipMessage))
                        Message = SkipMessage;
                    else
                        Message = "Player paused.";
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
            return Message;
        }

        public async Task<string> Play(ulong User, ulong Guild, IAudioChannel Channel)
        {
            string Message;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Message = "Error: The server is not playing any music!";
                else if (!await HasPermissions(User, Guild, Channel))
                    Message = "You do not have the power to run this command!";
                else
                {
                    string SkipMessage = Players[0].TryPlay();
                    if (!string.IsNullOrEmpty(SkipMessage))
                        Message = SkipMessage;
                    else
                        Message = "Player continued.";
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
            return Message;
        }

        public async Task<string> Shuffle(ulong User, ulong Guild, IAudioChannel Channel)
        {
            string Message;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Message = "Error: The server is not playing any music!";
                else if (!await HasPermissions(User, Guild, Channel))
                    Message = "You do not have the power to run this command!";
                else
                {
                    Players[0].Shuffle();
                    Message = "Shuffled.";
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
            return Message;
        }

        public async Task<string> ToggleLoop(ulong User, ulong Guild, IAudioChannel Channel)
        {
            string Message;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Message = "Error: The server is not playing any music!";
                else if (!await HasPermissions(User, Guild, Channel))
                    Message = "You do not have the power to run this command!";
                else
                    Message = Players[0].ToggleLoopSong();
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
            return Message;
        }

        public async Task<string> ToggleLoopQueue(ulong User, ulong Guild, IAudioChannel Channel)
        {
            string Message;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Message = "Error: The server is not playing any music!";
                else if (!await HasPermissions(User, Guild, Channel))
                    Message = "You do not have the power to run this command!";
                else
                    Message = Players[0].ToggleLoopPlaylist();
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
            return Message;
        }

        public async Task<string> Disconnect(ulong User, ulong Guild, IAudioChannel Channel)
        {
            string Message;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Message = "Error: The server is not playing any music!";
                else if (!await HasPermissions(User, Guild, Channel))
                    Message = "You do not have the power to run this command!";
                else
                {
                    await Players[0].DisposeAsync();
                    Message = "Successfully disconnected.";
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
            return Message;
        }

        public async Task<string> Skip(ulong User, ulong Guild, IVoiceChannel Channel)
        {
            string Error;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Error = "The server is not playing any music!";
                else if (!await HasPermissions(User, Guild, Channel))
                    Error = "You do not have the power to run this command!";
                else
                {
                    string SkipMessage = Players[0].TrySkip();
                    Error = SkipMessage;
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }

            return string.IsNullOrWhiteSpace(Error) ? "" : "Error: " + Error;
        }

        public async Task<string> Remove(ulong User, ulong Guild, IVoiceChannel Channel, int Index)
        {
            string Error;
            string SongName = null;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Error = "The server is not playing any music!";
                else if (!await HasPermissions(User, Guild, Channel))
                    Error = "You do not have the power to run this command!";
                else
                {
                    string SkipMessage = Players[0].TryRemove(Index, out SongName);
                    Error = SkipMessage;
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }

            if (SongName != null)
            {
                SongName = SongName.Replace("\\", "\\\\");
                SongName = SongName.Replace("`", "\\`");
            }
            return string.IsNullOrWhiteSpace(Error) ? $"Removed ``{SongName}`` successfully." : "Error: " + Error;
        }

        public Tuple<Embed, string> GetMusicQueue(ulong Guild, int Page)
        {
            Embed List = null;
            string Error = null;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Error = "The server is not playing any music!";
                else
                {
                    var Player = Players[0];
                    var Result = EmbedDisplayService.DisplaySongList(Player.NowPlaying, Player.Songs, Page);
                    if (Result.Item1 == null)
                    {
                        Error = $"Invalid page number!";
                        if (Result.Item2 > 1)
                            Error += $" It should be between 1-{Result.Item2}, inclusively.";
                        else
                            Error += " There is only one page.";
                    }
                    else
                        List = Result.Item1;
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            return new Tuple<Embed, string>(List, Error);
        }

        public Tuple<Embed, string> GetNowPlaying(ulong Guild)
        {
            Embed List = null;
            string Error = null;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Error = "The server is not playing any music!";
                else
                {
                    var Player = Players[0];
                    List = EmbedDisplayService.DisplayNowPlaying(Player.NowPlaying, Player.GetPosition());
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            return new Tuple<Embed, string>(List, Error);
        }

        private async Task<bool> HasPermissions(ulong Caller, ulong Guild, IAudioChannel AudioChannel)
        {
            var Users = await AudioChannel.GetUsersAsync().FlattenAsync();

            bool UserFound = false;
            int UserCount = 0;

            foreach (var User in Users)
            {
                if (User.IsBot)
                    continue;
                UserCount++;
                UserFound |= User.Id == Caller;
            }

            if (!UserFound) return false;
            if (UserCount == 1) return true;
            var UserGuild = Program._client.GetGuild(Guild).GetUser(Caller);
            foreach (var Role in UserGuild.Roles)
            {
                var Name = Role.Name.ToLower().Trim();
                if (Name == "dj" || Name == "guardian")
                    return true;
            }
            return UserGuild.GuildPermissions.ManageChannels || UserGuild.GuildPermissions.Administrator;
        }
    }
}
