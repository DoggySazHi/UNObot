using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using UNObot.Services;

#pragma warning disable CS1701 // Assuming assembly reference matches identity
#pragma warning disable CS1702 // Assuming assembly reference matches identitynamespace UNObot.Modules

namespace UNObot.Modules
{
    public struct Song
    {
        public string URL;
        public ulong RequestedBy;
        public string Name;
        public string Duration;
        public string ThumbnailURL;
        public bool IsPlaying;

        public async Task Prepopulate()
        {
            if (URL == null) return;
            bool Valid = Uri.TryCreate(URL, UriKind.Absolute, out Uri uriResult)
                          && (uriResult.Scheme == "http" || uriResult.Scheme == "https");
            if (!Valid) return;
            var Result = await DownloadHelper.GetInfo(URL);
            Name = Result.Item1;
            Duration = Result.Item2;
            ThumbnailURL = Result.Item3;
        }
    }

    public class Player
    {
        public ulong Server { get; private set; }
        private Queue<Song> Songs;
        private bool LoopingQueue;
        private bool LoopingSong;

        private CancellationTokenSource _disposeToken;
        private IAudioClient AudioClient;
        /*
        private async Task AudioHelper(IAudioClient audioChannel, string Path)
        {
            using (AudioOutStream discord = audioChannel.CreatePCMStream(AudioApplication.Mixed, 1920))
            {
                Stream output = Shell.GetAudioStream(Path);

                int bufferSize = 1024;
                int bytesSent = 0;
                bool fail = false;
                bool exit = false;
                byte[] buffer = new byte[bufferSize];

                while (
                    //!Skip &&                                    // If Skip is set to true, stop sending and set back to false (with getter)
                    !fail &&                                    // After a failed attempt, stop sending
                    !exit                                       // Audio Playback has ended (No more data from FFmpeg.exe)
                        )
                {
                    try
                    {
                        int read = await output.ReadAsync(buffer, 0, bufferSize);
                        if (read == 0)
                        {
                            //No more data available
                            exit = true;
                            break;
                        }

                        await discord.WriteAsync(buffer, 0, read);

                        if (Pause)
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
                    catch (TaskCanceledException)
                    {
                        exit = true;
                    }
                    catch
                    {
                        fail = true;
                        // could not send
                    }
                }
                await discord.FlushAsync();
                await output.DisposeAsync();
            }
        }

        //Send Audio with ffmpeg
        private async Task SendAudio(IVoiceChannel ivc, string Path)
        {
            IAudioChannel audio;
            _voiceChannel = (socketMsg.Author as IGuildUser)?.VoiceChannel;
            if (ivc == null)
            {
                Print("Error joining Voice Channel!", ConsoleColor.Red);
                await socketMsg.Channel.SendMessageAsync($"I can't connect to your Voice Channel <@{socketMsg.Author}>!" + ImABot);
            }
            else
            {
                Print($"Joined Voice Channel \"{_voiceChannel.Name}\"", ConsoleColor.Magenta);
                audio = await _voiceChannel.ConnectAsync();
            }
        }
        */
        public Player(ulong Guild, IAudioClient AudioClient)
        {

        }

    }

    public class MusicBot : ModuleBase<SocketCommandContext>
    {
        List<Player> MusicPlayers = new List<Player>();

        [Command("playmusic", RunMode = RunMode.Async)]
        [Help(new string[] { ".playmusic (YouTube Link)" }, "Wait, MusicBot functionality in UNObot?", true, "UNObot 3.2 Beta 1")]
        public async Task PlayMusic([Remainder] string Link)
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                _ = ReplyAsync("Please join a VC that I can connect to!");
                return;
            }

            try
            {
                //Test for valid URL
                bool result = Uri.TryCreate(Link, UriKind.Absolute, out Uri uriResult)
                          && (uriResult.Scheme == "http" || uriResult.Scheme == "https");
                if (!result)
                    _ = ReplyAsync("That is not a valid link!");

                _ = ReplyAsync($"Program is now querying, please wait warmly...");
                var Embed = await DisplayEmbed.DisplayAddSong(Context.User.Id, Context.Guild.Id, Link);
                await ReplyAsync("", false, Embed.Item1);

                if (MusicPlayers.Where(o => o.Server == Context.Guild.Id).Count() == 0)
                    MusicPlayers.Add(new Player(Context.Guild.Id, await AudioChannel.ConnectAsync()));

                // Wait before downloading...
                var Result2 = await DownloadHelper.Download(Link);
                _ = ReplyAsync($"{Result2} has been created!");
            }
            catch (Exception ex)
            {
                _ = ReplyAsync($"Error: {ex.Message}");
            }
        }

        [Command("vctest", RunMode = RunMode.Async)]
        [Help(new string[] { ".playmusic (YouTube Link)" }, "Wait, MusicBot functionality in UNObot?", true, "UNObot 3.2 Beta 1")]
        public async Task VCTest()
        {
            var AudioChannel = (Context.User as IVoiceState)?.VoiceChannel;
            if (AudioChannel == null)
            {
                _ = ReplyAsync("Please join a VC that I can connect to!");
                return;
            }

            _ = ReplyAsync("Attempting to connect...");
            var AudioClient = await AudioChannel.ConnectAsync();

            using (var output = Shell.GetAudioStream("/Users/doggysazhi/Documents/Projects/UNObot/src/UNObot/bin/Debug/netcoreapp2.1/Music/downloadSong3.mp3"))
            {
                using (AudioOutStream discord = AudioClient.CreatePCMStream(AudioApplication.Music))
                {
                    _ = ReplyAsync("Preparing to play...");
                    try { await output.StandardOutput.BaseStream.CopyToAsync(discord); }
                    finally { await discord.FlushAsync(); }
                    _ = ReplyAsync("Playback Finished.");
                    await discord.FlushAsync();
                }
            }
        }

        [Command("vctest2", RunMode = RunMode.Async)]
        public async Task VCTest2([Remainder] string song)
        {
            var AudioChannel = (Context.User as IVoiceState)?.VoiceChannel;
            if (AudioChannel == null)
            {
                _ = ReplyAsync("Please join a VC that I can connect to!");
                return;
            }

            AudioService ads = AudioService.GetSingleton();
            await ads.JoinAudio(Context.Guild, AudioChannel);
            await ads.SendAudioAsync(Context.Guild, Context.Channel, song);
            await ads.LeaveAudio(Context.Guild);
        }
    }
}
