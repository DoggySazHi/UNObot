using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;

namespace UNObot.Services
{
    public struct Song
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
            PathCached = await YoutubeService.GetSingleton().Download(URL, RequestedGuild);
        }

        public void SetPlaying() => IsPlaying = true;
    }

    public class Player : IAsyncDisposable
    {
        public ulong Guild { get; private set; }
        private Queue<Song> Songs;
        //TODO make sure they don't conflict
        public bool LoopingQueue { get; private set; }
        public bool LoopingSong { get; private set; }

        public bool Paused { get; private set; }

        private bool Skip;
        private bool Quit;
        private bool IsPlaying;
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
            Paused = true;
            if (Songs.Count != 0 && AudioClient?.ConnectionState != ConnectionState.Connected)
                AudioClient = await AudioChannel.ConnectAsync();
        }

        public async Task RunPlayer()
        {
            await FixConnection(null);
            while (Songs.Count != 0)
            {
                await SendAudio(CreateStream(Songs.Dequeue().PathCached), AudioClient.CreatePCMStream(AudioApplication.Music));
            }
            await AudioClient.StopAsync();
        }

        public string TryPause()
        {
            if (Songs.Count == 0)
                return "There is no song playing.";
            if (!Paused || PauseEvent.WaitOne(0))
            {
                Paused = false;
                return "Player is already playing.";
            }
            Paused = true;
            PauseEvent.Reset();
            return null;
        }

        public string TryPlay()
        {
            if (Songs.Count == 0)
                return "There is no song playing.";
            if (!Paused || !PauseEvent.WaitOne(0))
            {
                Paused = true;
                return "Player is already paused.";
            }
            Paused = false;
            PauseEvent.Set();
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

                    while (!Skip && !Fail && !Exit && !Quit)
                    {
                        try
                        {
                            int read = await AudioStream.ReadAsync(buffer, 0, bufferSize);
                            if (read == 0)
                            {
                                //No more data available
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
                    Skip = false;
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
            Songs = new Queue<Song>();
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
    }
}
