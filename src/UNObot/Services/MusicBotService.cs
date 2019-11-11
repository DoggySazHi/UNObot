using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using UNObot.Modules;
using Timer = System.Timers.Timer;

namespace UNObot.Services
{
    // Can't use Struct, needs passing by reference.
    public class Song
    {
        public string URL { get; private set; }
        public string PathCached { get; private set; }
        public ulong RequestedBy { get; private set; }
        public ulong RequestedGuild { get; private set; }
        public string Name { get; private set; }
        public string Duration { get; private set; }
        public string ThumbnailURL { get; private set; }
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
            if (PathCached == null || PathCached == "")
            {
                Console.WriteLine($"Caching {Name}");
                PathCached = "Caching...";
                PathCached = await YoutubeService.GetSingleton().Download(URL, RequestedGuild);
                if (EndCache != null)
                    EndCache.Set();
                Console.WriteLine("Finished caching.");
            }
        }

        public void SetCacheEvent(ManualResetEvent EndCache)
        {
            if (!string.IsNullOrEmpty(PathCached) && PathCached != "Caching...")
            {
                EndCache.Set();
                return;
            }
            this.EndCache = EndCache;
        }

        //TODO Playing Message in Discord Embed
        public void SetPlaying() => IsPlaying = true;
    }

    public class Player : IAsyncDisposable
    {
        public ulong Guild { get; private set; }
        public List<Song> Songs { get; private set; }
        public Song NowPlaying { get; private set; }
        //TODO make sure they don't conflict
        public bool LoopingQueue { get; private set; }
        public bool LoopingSong { get; private set; }

        public bool Paused { get; private set; }
        public bool Disposed { get; private set; }

        private bool Quit;
        private bool IsPlaying;
        private bool Caching;
        private readonly int CacheLength = 5;
        private ManualResetEvent PauseEvent;
        private ManualResetEvent QuitEvent;
        private ManualResetEvent CacheEvent;
        private Stopwatch PlayPos;
        private Timer Timeout;

        private IVoiceChannel AudioChannel;
        private IAudioClient AudioClient;
        private ISocketMessageChannel MessageChannel;

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
            Timeout = new Timer(5000);
        }

        private bool HandlingError;
        private async Task FixConnection(Exception arg)
        {
            if (!IsPlaying || HandlingError)
                return;
            HandlingError = true;
            bool PrevPlaying = !Paused;

            if (PrevPlaying)
                Console.WriteLine(TryPause());
            if (!Disposed && AudioClient?.ConnectionState != ConnectionState.Connected)
            {
                AudioClient = await AudioChannel.ConnectAsync();
                AudioClient.Disconnected += FixConnection;
                _ = MessageChannel.SendMessageAsync("Detected audio disconnection, reconnected. Use .disconnect to force the bot to leave.");
                if (PrevPlaying)
                {
                    Console.WriteLine(TryPlay());
                    Console.WriteLine("Playing.");
                }
            }
            HandlingError = false;
        }

        public async Task RunPlayer()
        {
            Console.WriteLine($"Player initialized for {Guild}");
            await FixConnection(null);
            while (Songs.Count != 0)
            {
                NowPlaying = Songs[0];
                Songs.RemoveAt(0);

                CacheEvent.Reset();
                if (NowPlaying.PathCached == null)
                    await NowPlaying.Cache();

                NowPlaying.SetCacheEvent(CacheEvent);
                Console.WriteLine(CacheEvent.WaitOne(0));
                CacheEvent.WaitOne();

                NowPlaying.SetPlaying();
                PlayPos.Restart();
                _ = MessageChannel.SendMessageAsync("", false, DisplayEmbed.DisplayNowPlaying(NowPlaying, null));
                await SendAudio(CreateStream(NowPlaying.PathCached), AudioClient.CreatePCMStream(AudioApplication.Music));
                File.Delete(NowPlaying.PathCached);
                NowPlaying = null;
                _ = Task.Run(Cache);
            }
            await AudioClient.StopAsync();
            await DisposeAsync();
            Disposed = true;
        }

        private async Task Cache()
        {
            if (Caching)
                return;
            Caching = true;
            for (int i = 0; i < Math.Min(Songs.Count, CacheLength); i++)
            {
                Song s = Songs[i];
                await s.Cache();
            }
            Caching = false;
        }

        public void Add(string URL, Tuple<string, string, string> Data, ulong User, ulong Guild)
        {
            Songs.Add(new Song(URL, Data, User, Guild));
            _ = Task.Run(Cache);
        }

        public string TryPause()
        {
            if (Songs.Count == 0 && NowPlaying == null)
                return "There is no song playing.";
            if (Paused || !PauseEvent.WaitOne(0))
            {
                Paused = true;
                return $"Player is already paused. Paused Var: {Paused} PauseEvent: {PauseEvent.WaitOne(0)}";
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
                return $"Player is already playing. Paused Var: {Paused} PauseEvent: {PauseEvent.WaitOne(0)}";
            }
            Paused = false;
            PauseEvent.Set();
            return null;
        }

        public string TrySkip()
        {
            Console.WriteLine("Attempted to skip");
            if (Songs.Count == 0)
                return "There is no song playing.";
            if (Paused || !PauseEvent.WaitOne(0))
            {
                Paused = true;
                return "Player is paused. Resume playback before skipping.";
            }
            Quit = true;
            QuitEvent.WaitOne();
            QuitEvent.Reset();
            return null;
        }

        public void Shuffle()
        {
            ThreadSafeRandom.Shuffle(Songs);
        }

        private async Task SendAudio(Stream AudioStream, AudioOutStream DiscordStream)
        {
            IsPlaying = true;
            using (AudioStream)
            {
                using (DiscordStream)
                {

                    //Adjust?
                    int bufferSize = 1024;
                    int bytesSent = 0;
                    bool Fail = false;
                    bool Exit = false;
                    byte[] buffer = new byte[bufferSize];

                    // Skip: User skipped the song.
                    // Fail: Failed to read, kill the song.
                    // Exit: Song ended.
                    // Quit: Program exiting.

                    while (!Fail && !Exit && !Quit)
                    {
                        try
                        {
                            int read = await AudioStream.ReadAsync(buffer, 0, bufferSize);
                            if (read == 0)
                            {
                                Exit = true;
                                break;
                            }

                            try
                            {
                                await DiscordStream.WriteAsync(buffer, 0, read);
                            }
                            catch (OperationCanceledException)
                            {
                                Console.WriteLine("Failed to write!");
                            }

                            if (Paused)
                            {
                                PauseEvent.Reset();
                                PlayPos.Stop();
                                PauseEvent.WaitOne();
                                PlayPos.Start();
                            }

                            bytesSent += read;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error: " + ex);
                            Fail = true;
                        }
                    }
                    //TODO download timer when loading, so we can restart if bad
                    Console.WriteLine($"Finished playing {Fail} {Exit} {Quit}");
                    PlayPos.Stop();
                    //await DiscordStream.FlushAsync();
                    Paused = false;
                }
            }
            IsPlaying = false;
            if (Quit)
                QuitEvent.Set();
        }

        public string GetPosition()
        {
            return YoutubeService.TimeString(PlayPos.Elapsed);
        }

        private Stream CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "/usr/local/bin/ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            }).StandardOutput.BaseStream;
        }

        public async ValueTask DisposeAsync()
        {
            Songs.Clear();
            Paused = false;
            Quit = true;
            if (IsPlaying)
                QuitEvent.WaitOne();
            if (AudioClient?.ConnectionState == ConnectionState.Connected)
            {
                await AudioClient?.StopAsync();
            }
            AudioClient?.Dispose();
            PauseEvent?.Dispose();
        }
    }

    public class MusicBotService
    {
        private static MusicBotService Instance;
        private List<Player> MusicPlayers = new List<Player>();

        private MusicBotService()
        {

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

        public async Task<Tuple<Player, string>> ConnectAsync(ulong Guild, IVoiceChannel AudioChannel, ISocketMessageChannel MessageChannel)
        {
            var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
            if (Players.Count == 0)
            {
                Player p = new Player(Guild, AudioChannel, await AudioChannel.ConnectAsync(), MessageChannel);
                MusicPlayers.Add(p);
                _ = Task.Run(p.RunPlayer);
                return new Tuple<Player, string>(p, null);
            }
            if (Players[0].Disposed)
            {
                Players[0] = new Player(Guild, AudioChannel, await AudioChannel.ConnectAsync(), MessageChannel);
                _ = Task.Run(Players[0].RunPlayer);
            }
            return new Tuple<Player, string>(Players[0], null);
        }

        public async Task<Tuple<Embed, string>> Add(ulong User, ulong Guild, string URL, IVoiceChannel Channel, ISocketMessageChannel MessageChannel)
        {
            Embed EmbedOut;
            string Error = null;
            try
            {
                var Result = await DisplayEmbed.DisplayAddSong(User, Guild, URL);
                EmbedOut = Result.Item1;
                var Data = Result.Item2;
                var Player = await ConnectAsync(Guild, Channel, MessageChannel);
                if (Player.Item2 != null)
                    Error = Player.Item2;
                else
                    Player.Item1.Add(URL, Data, User, Guild);
            }
            catch (Exception ex)
            {
                return new Tuple<Embed, string>(null, ex.Message);
            }

            return new Tuple<Embed, string>(EmbedOut, Error);
        }

        public string Pause(ulong User, ulong Guild, IAudioChannel Channel)
        {
            string Message;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Message = "Error: The server is not playing any music!";
                else
                {
                    string SkipMessage = Players[0].TryPause();
                    if (SkipMessage != null && SkipMessage != "")
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

        public string Play(ulong User, ulong Guild, IAudioChannel Channel)
        {
            string Message;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Message = "Error: The server is not playing any music!";
                else
                {
                    string SkipMessage = Players[0].TryPlay();
                    if (SkipMessage != null && SkipMessage != "")
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

        public string Shuffle(ulong User, ulong Guild, IAudioChannel Channel)
        {
            string Message;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Message = "Error: The server is not playing any music!";
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

        public async Task<Tuple<Embed, string>> AddList(ulong User, ulong Guild, string URL, IVoiceChannel Channel, ISocketMessageChannel MessageChannel)
        {
            Embed Display = null;
            string Message;
            try
            {
                var Playlist = await DisplayEmbed.DisplayPlaylist(User, Guild, URL);
                Display = Playlist.Item1;
                var ResultPlay = Playlist.Item2.Videos;
                var Player = await ConnectAsync(Guild, Channel, MessageChannel);
                if (Player.Item2 != null)
                    Message = Player.Item2;
                else
                {
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

        public string Skip(ulong User, ulong Guild, IVoiceChannel Channel)
        {
            //TODO Add checking for User.
            string Error = null;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Error = "The server is not playing any music!";
                else
                {
                    string SkipMessage = Players[0].TrySkip();
                    if (SkipMessage != null && SkipMessage != "")
                        Error = SkipMessage;
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }

            return Error != null ? "Skipped song." : "Error: " + Error;
        }

        //TODO Shuffle CMD, add entire playlist
        public Tuple<Embed, string> GetMusicQueue(ulong Guild)
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
                    List = DisplayEmbed.DisplaySongList(Player.NowPlaying, Player.Songs);
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
                    List = DisplayEmbed.DisplayNowPlaying(Player.NowPlaying, Player.GetPosition());
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            return new Tuple<Embed, string>(List, Error);
        }
    }
}
