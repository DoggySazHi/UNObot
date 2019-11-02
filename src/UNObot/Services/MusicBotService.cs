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
        public string URL;
        public string PathCached;
        public ulong RequestedBy;
        public string Name;
        public string Duration;
        public string ThumbnailURL;
        public bool IsPlaying;

        public async Task Prepopulate(string URL, Tuple<string, string, string> Data)
        {
            this.URL = URL;
            Name = Data.Item1;
            Duration = Data.Item2;
            ThumbnailURL = Data.Item3;
        }

        public async Task Cache()
        {
            PathCached = await YoutubeService.GetSingleton().Download(URL);
        }
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
        private bool IsPlaying;
        private ManualResetEvent PauseEvent;

        private CancellationTokenSource _disposeToken;
        private IVoiceChannel AudioChannel;
        private IAudioClient AudioClient;

        public Player(ulong Guild, IVoiceChannel AudioChannel, IAudioClient AudioClient)
        {
            this.Guild = Guild;
            this.AudioClient = AudioClient;
            this.AudioChannel = AudioChannel;
            AudioClient.Disconnected += FixConnection;
        }

        private async Task FixConnection(Exception arg)
        {
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

        public void TogglePause()
        {

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

                    while (
                        !Skip &&                                    // If Skip is set to true, stop sending and set back to false (with getter)
                        !Fail &&                                    // After a failed attempt, stop sending
                        !Exit                                       // Audio Playback has ended (No more data from FFmpeg.exe)
                            )
                    {
                        try
                        {
                            int read = await AudioStream.ReadAsync(buffer, 0, bufferSize, _disposeToken.Token);
                            if (read == 0)
                            {
                                //No more data available
                                Exit = true;
                                break;
                            }

                            await DiscordStream.WriteAsync(buffer, 0, read, _disposeToken.Token);

                            if (Paused)
                            {
                                bool pauseAgain;

                                do
                                {
                                    pauseAgain = await _tcs.Task;
                                    _tcs = new TaskCompletionSource<bool>();
                                } while (pauseAgain);
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
            Skip = true;
            while (IsPlaying) { }
            if (AudioClient?.ConnectionState == ConnectionState.Connected)
            {
                await AudioClient?.StopAsync();
            }
            AudioClient?.Dispose();
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
