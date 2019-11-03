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

        //TODO Add method auto-fills this.
        public void Prepopulate(string URL, Tuple<string, string, string> Data, ulong User, ulong Guild)
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
            if (PathCached == null)
                PathCached = await YoutubeService.GetSingleton().Download(URL, RequestedGuild);
        }

        public void SetPlaying() => IsPlaying = true;
    }

    public class Player : IAsyncDisposable
    {
        public ulong Guild { get; private set; }
        private List<Song> Songs;
        //TODO make sure they don't conflict
        public bool LoopingQueue { get; private set; }
        public bool LoopingSong { get; private set; }

        public bool Paused { get; private set; }

        private bool Quit;
        private bool IsPlaying;
        private bool Caching;
        private readonly int CacheLength = 5;
        private ManualResetEvent PauseEvent;
        private ManualResetEvent QuitEvent;

        private IVoiceChannel AudioChannel;
        private IAudioClient AudioClient;

        public Player(ulong Guild, IVoiceChannel AudioChannel, IAudioClient AudioClient)
        {
            this.Guild = Guild;
            this.AudioClient = AudioClient;
            this.AudioChannel = AudioChannel;
            AudioClient.Disconnected += FixConnection;
            PauseEvent = new ManualResetEvent(false);
            QuitEvent = new ManualResetEvent(false);
        }

        private async Task FixConnection(Exception arg)
        {
            bool PrevPlaying = !Paused;

            if (PrevPlaying)
                TryPause();
            if (Songs.Count != 0 && AudioClient?.ConnectionState != ConnectionState.Connected)
            {
                AudioClient = await AudioChannel.ConnectAsync();
                if (PrevPlaying)
                    TryPlay();
            }
        }

        public async Task RunPlayer()
        {
            await FixConnection(null);
            while (Songs.Count != 0)
            {
                Song NextSong = Songs[0];
                Songs.RemoveAt(0);

                if (NextSong.PathCached == null)
                    await NextSong.Cache();
                await SendAudio(CreateStream(NextSong.PathCached), AudioClient.CreatePCMStream(AudioApplication.Music));
                File.Delete(NextSong.PathCached);
                _ = Task.Run(() => Cache());
            }
            await AudioClient.StopAsync();
        }

        private async Task Cache()
        {
            if (Caching)
                return;
            Caching = true;
            List<Song> ToCache = Songs.GetRange(0, Math.Min(CacheLength, Songs.Count));
            foreach (Song s in ToCache)
                await s.Cache();
            Caching = false;
        }

        public string TryPause()
        {
            if (Songs.Count == 0)
                return "There is no song playing.";
            if (Paused || !PauseEvent.WaitOne(0))
            {
                Paused = false;
                return "Player is already paused.";
            }
            Paused = true;
            PauseEvent.Reset();
            return null;
        }

        public string TryPlay()
        {
            if (Songs.Count == 0)
                return "There is no song playing.";
            if (!Paused || PauseEvent.WaitOne(0))
            {
                Paused = true;
                return "Player is already playing.";
            }
            Paused = false;
            PauseEvent.Set();
            return null;
        }

        public string TrySkip()
        {
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

                            await DiscordStream.WriteAsync(buffer, 0, read);

                            if (Paused)
                            {
                                PauseEvent.Reset();
                                PauseEvent.WaitOne();
                            }

                            bytesSent += read;
                        }
                        catch
                        {
                            Fail = true;
                        }
                    }
                    await DiscordStream.FlushAsync();
                    Paused = false;
                }
            }
            IsPlaying = false;
            if (Quit)
                QuitEvent.Set();
        }

        private Stream CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            }).StandardOutput.BaseStream;
        }

        public async ValueTask DisposeAsync()
        {
            Songs.Clear();
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
        private SocketGuildUser Self;

        private MusicBotService()
        {
            Self = Program._client.GetUser(Program._client.CurrentUser.Id) as SocketGuildUser;
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

        public async Task<string> ConnectAsync(ulong Guild, IVoiceChannel AudioChannel)
        {
            var Permissions = Self.GetPermissions(AudioChannel);
            if (!Permissions.Connect)
                return "No permissions to connect to the voice channel!";
            if (!Permissions.Speak)
                return "No permissions to talk in the voice channel!";

            if (MusicPlayers.FindAll(o => o.Guild == Guild).Count == 0)
                MusicPlayers.Add(new Player(Guild, AudioChannel, await AudioChannel.ConnectAsync()));
            return null;
        }

        //TODO return null if it doesn't exist, otherwise add.
        public async Task<Embed> Add(ulong User, ulong Guild, string URL)
        {
            Embed EmbedOut;
            try
            {
                //TODO move DisplayEmbed as a service.
                var Result = await DisplayEmbed.DisplayAddSong(User, Guild, URL);
                EmbedOut = Result.Item1;
            }
            catch (Exception)
            {
                return null;
            }
            return EmbedOut;
        }
    }
}
