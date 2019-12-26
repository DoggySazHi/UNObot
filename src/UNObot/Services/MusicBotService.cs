﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using UNObot.Modules;
using YoutubeExplode.Models;
using Timer = System.Timers.Timer;

namespace UNObot.Services
{
    // Can't use Struct, needs passing by reference.
    public class Song
    {
        public string URL { get; private set; }
        public string PathCached { get; set; }
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
            if (string.IsNullOrEmpty(PathCached))
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
        public bool Disposed { get { return _Disposed || AudioClient.ConnectionState == ConnectionState.Disconnected; } }

        private bool Quit;
        private bool IsPlaying;
        private bool Caching;
        private readonly int CacheLength = 5;
        private readonly ManualResetEvent PauseEvent;
        private readonly ManualResetEvent QuitEvent;
        private readonly ManualResetEvent CacheEvent;
        private readonly Stopwatch PlayPos;
        private readonly Timer Timeout;

        private readonly IVoiceChannel AudioChannel;
        private IAudioClient AudioClient;
        private readonly ISocketMessageChannel MessageChannel;

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
                _ = MessageChannel.SendMessageAsync("Detected audio disconnection, reconnected. Use .playerdc to force the bot to leave.");
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
                NowPlaying.SetCacheEvent(CacheEvent);
                if (NowPlaying.PathCached == null)
                    await NowPlaying.Cache().ConfigureAwait(false);

                CacheEvent.WaitOne();

                NowPlaying.SetPlaying();

                do
                {
                    PlayPos.Restart();
                    _ = MessageChannel.SendMessageAsync("", false, EmbedDisplayService.DisplayNowPlaying(NowPlaying, null));
                    await SendAudio(CreateStream(NowPlaying.PathCached), AudioClient.CreatePCMStream(AudioApplication.Music, AudioChannel.Bitrate)).ConfigureAwait(false);
                }
                while (LoopingSong);

                if (LoopingQueue)
                    Songs.Add(NowPlaying);

                File.Delete(NowPlaying.PathCached);
                NowPlaying.PathCached = null;

                NowPlaying = null;
                _ = Task.Run(Cache).ConfigureAwait(false);
            }
            await DisposeAsync();
        }

        private async Task Cache()
        {
            if (Caching)
                return;
            Caching = true;
            List<string> FilesCached = new List<string>();
            for (int i = 0; i < Math.Min(Songs.Count, CacheLength); i++)
            {
                Song s = Songs[i];
                if(string.IsNullOrWhiteSpace(s.PathCached) || s.PathCached != "Caching...")
                await s.Cache().ConfigureAwait(false);
                FilesCached.Add(s.PathCached);
            }
            YoutubeService.GetSingleton().DeleteGuildFolder(Guild, FilesCached.ToArray());
            Caching = false;
        }

        public void Add(string URL, Tuple<string, string, string> Data, ulong User, ulong Guild, bool InsertAtTop = false)
        {
            Song s = new Song(URL, Data, User, Guild);
            if (InsertAtTop)
                Songs.Insert(0, s);
            else
                Songs.Add(s);
            _ = Task.Run(Cache);
        }

        public string TryPause()
        {
            if (Songs.Count == 0 && NowPlaying == null)
                return "There is no song playing.";
            if (Paused || !PauseEvent.WaitOne(0))
            {
                Paused = true;
                return $"Player is already paused.";
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
                return $"Player is already playing.";
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
            try //Just brute force it.
            {
                YoutubeService.GetSingleton().DeleteGuildFolder(Guild);
            }
            catch(Exception)
            {
                YoutubeService.GetSingleton().DeleteGuildFolder(Guild);
            }
            ThreadSafeRandom.Shuffle(Songs);
            _ = Task.Run(Cache).ConfigureAwait(false);
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
                            if (AudioClient.ConnectionState == ConnectionState.Disconnected)
                            {
                                AudioClient = await AudioChannel.ConnectAsync();
                                AudioClient.Disconnected += FixConnection;
                            }

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
                    //TODO Flush might break things
                    PlayPos.Stop();
                    await DiscordStream.FlushAsync();
                    Paused = false;
                }
                await AudioStream.FlushAsync();
            }
            IsPlaying = false;
            if (Quit)
            {
                QuitEvent.Set();
                Quit = false;
            }
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
            try
            {
                Songs.Clear();
                LoopingSong = false;
                LoopingQueue = false;
                Paused = false;
                Quit = true;
                if (IsPlaying)
                    QuitEvent.WaitOne();
                await AudioClient.StopAsync();
                try { await AudioChannel.DisconnectAsync(); } catch (Exception) { }
                AudioClient.Dispose();
                PauseEvent.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to dispose: " + e);
            }
            finally
            {
                _Disposed = true;
                Console.WriteLine($"Disposed {Guild}");
            }
        }
    }

    public class MusicBotService
    {
        private static MusicBotService Instance;
        private readonly List<Player> MusicPlayers = new List<Player>();

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
            var Player = MusicPlayers.FindIndex(o => o.Guild == Guild);
            if (Player < 0)
            {
                Player NewPlayer = new Player(Guild, AudioChannel, await AudioChannel.ConnectAsync(), MessageChannel);
                MusicPlayers.Add(NewPlayer);
                _ = Task.Run(NewPlayer.RunPlayer);
                Console.WriteLine("Generated new player.");
                return new Tuple<Player, string>(NewPlayer, null);
            }
            if (MusicPlayers[Player].Disposed)
            {
                Console.WriteLine("Replaced player.");
                MusicPlayers.RemoveAt(Player);
                var NewPlayer = new Player(Guild, AudioChannel, await AudioChannel.ConnectAsync(), MessageChannel);
                MusicPlayers.Add(NewPlayer);
                _ = Task.Run(NewPlayer.RunPlayer);
                return new Tuple<Player, string>(NewPlayer, null);
            }
            Console.WriteLine("Returned existing player.");
            return new Tuple<Player, string>(MusicPlayers[Player], null);
        }

        public async Task<Tuple<Embed, string>> Add(ulong User, ulong Guild, string URL, IVoiceChannel Channel, ISocketMessageChannel MessageChannel, bool InsertAtTop = false)
        {
            Embed EmbedOut;
            string Error = null;
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

        public async Task<Tuple<Embed, string>> Search(ulong User, ulong Guild, string Query, IVoiceChannel Channel, ISocketMessageChannel MessageChannel, bool InsertAtTop = false)
        {
            Embed EmbedOut;
            string Error = null;
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

        public async Task<Tuple<Embed, string>> AddList(ulong User, ulong Guild, string URL, IVoiceChannel Channel, ISocketMessageChannel MessageChannel, bool InsertAtTop = false)
        {
            Embed Display = null;
            string Message;
            try
            {
                var Playlist = await EmbedDisplayService.DisplayPlaylist(User, Guild, URL);
                Display = Playlist.Item1;
                var ResultPlay = Playlist.Item2.Videos;
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
                                new Tuple<string, string, string>(Video.Title, YoutubeService.TimeString(Video.Duration), Video.Thumbnails.MediumResUrl), User, Guild, InsertAtTop);
                        }
                    foreach (var Video in ResultPlay)
                        Player.Item1.Add($"https://www.youtube.com/watch?v={Video.Id}",
                            new Tuple<string, string, string>(Video.Title, YoutubeService.TimeString(Video.Duration), Video.Thumbnails.MediumResUrl), User, Guild, InsertAtTop);
                    Message = $"Added {ResultPlay.Count} song{(ResultPlay.Count == 1 ? "" : "s")}.";
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }

            return new Tuple<Embed, string>(Display, Message);
        }

        public async Task<string> Skip(ulong User, ulong Guild, IVoiceChannel Channel)
        {
            string Error = null;
            try
            {
                var Players = MusicPlayers.FindAll(o => o.Guild == Guild);
                if (Players.Count == 0 || Players[0].Disposed)
                    Error = "The server is not playing any music!";
                else if(!await HasPermissions(User, Guild, Channel))
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

            return string.IsNullOrWhiteSpace(Error) ? "Skipped song." : "Error: " + Error;
        }

        public async Task<string> Remove(ulong User, ulong Guild, IVoiceChannel Channel, int Index)
        {
            string Error = null;
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
                SongName.Replace("\\", "\\\\");
                SongName.Replace("`", "\\`");
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

            foreach(var User in Users)
            {
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
