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
    public class MusicBotCommands : ModuleBase<SocketCommandContext>
    {
        [Command("playerplay", RunMode = RunMode.Async), Alias("playmusic")]
        [Help(new string[] { ".playerplay", ".playerplay (link)", ".playerplay (search)" }, "Wait, MusicBot functionality in UNObot?", true, "UNObot 3.2 Beta 1")]
        public async Task PlayMusic([Remainder] string Link)
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Result = await MusicBotService.GetSingleton().AddList(Context.User.Id, Context.Guild.Id, Link, AudioChannel, Context.Channel);
            if (Result.Item1 == null)
                Result = await MusicBotService.GetSingleton().Add(Context.User.Id, Context.Guild.Id, Link, AudioChannel, Context.Channel);
            if (Result.Item1 == null)
                Result = await MusicBotService.GetSingleton().Search(Context.User.Id, Context.Guild.Id, Link, AudioChannel, Context.Channel);
            if (Result.Item2 != null && Result.Item2.Contains("Error"))
                _ = ReplyAsync($"Error: {Result.Item2}");
            else
                _ = ReplyAsync(Result.Item2 ?? "", false, Result.Item1);
        }

        [Command("playerplay", RunMode = RunMode.Async)]
        public async Task Play()
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Result = MusicBotService.GetSingleton().Play(Context.User.Id, Context.Guild.Id, AudioChannel);
            _ = ReplyAsync(Result);
        }

        [Command("playerpause", RunMode = RunMode.Async), Alias("pause", "pauseplayer")]
        [Help(new string[] { ".playerpause" }, "Pause the player.", true, "UNObot 3.2 Beta 2")]
        public async Task Pause()
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Result = MusicBotService.GetSingleton().Pause(Context.User.Id, Context.Guild.Id, AudioChannel);
            _ = ReplyAsync(Result);
        }

        [Command("playershuffle", RunMode = RunMode.Async), Alias("shuffle")]
        [Help(new string[] { ".playershuffle" }, "Shuffle the player.", true, "UNObot 3.2 Beta 3")]
        public async Task Shuffle()
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Result = MusicBotService.GetSingleton().Shuffle(Context.User.Id, Context.Guild.Id, AudioChannel);
            _ = ReplyAsync(Result);
        }

        [Command("playerskip", RunMode = RunMode.Async)]
        [Help(new string[] { ".playerskip" }, "Skip the current song.", true, "UNObot 3.2 Beta 3")]
        public async Task Skip()
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Result = MusicBotService.GetSingleton().Skip(Context.User.Id, Context.Guild.Id, AudioChannel);
            _ = ReplyAsync(Result);
        }

        [Command("playerloop", RunMode = RunMode.Async)]
        [Help(new string[] { ".playerloop" }, "Loop the current song.", true, "UNObot 3.2 Beta 4")]
        public async Task Loop()
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Result = MusicBotService.GetSingleton().ToggleLoop(Context.User.Id, Context.Guild.Id, AudioChannel);
            await ReplyAsync(Result);
        }

        //TODO PlayerLoopQueue won't include NowPlaying
        [Command("playerloopqueue", RunMode = RunMode.Async)]
        [Help(new string[] { ".playerloopqueue" }, "Loop the entire queue.", true, "UNObot 3.2 Beta 4")]
        public async Task LoopQueue()
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Result = MusicBotService.GetSingleton().ToggleLoopQueue(Context.User.Id, Context.Guild.Id, AudioChannel);
            await ReplyAsync(Result);
        }

        [Command("playerdc", RunMode = RunMode.Async), Alias("dc", "playerdisconnect")]
        [Help(new string[] { ".playerdc" }, "Disconnect the bot from the channel.", true, "UNObot 3.2 Beta 4")]
        public async Task Disconnect()
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(false);
                return;
            }

            var Result = await MusicBotService.GetSingleton().Disconnect(Context.User.Id, Context.Guild.Id, AudioChannel).ConfigureAwait(true);
            await ReplyAsync(Result).ConfigureAwait(false);
        }

        [Command("playernp", RunMode = RunMode.Async), Alias("playernowplaying", "np")]
        [Help(new string[] { ".playernp" }, "Get the song playing.", true, "UNObot 3.2 Beta 2")]
        public async Task NowPlaying()
        {
            var Result = MusicBotService.GetSingleton().GetNowPlaying(Context.Guild.Id);
            if (!string.IsNullOrWhiteSpace(Result.Item2))
                await ReplyAsync($"Error: {Result.Item2}");
            else
                await ReplyAsync("", false, Result.Item1);
        }

        [Command("playerqueue", RunMode = RunMode.Async)]
        [Help(new string[] { ".playerqueue", ".playerqueue (page)" }, "Get the songs in the player's queue.", true, "UNObot 3.2 Beta 2")]
        public async Task Queue()
        {
            //TODO Multiple pages!
            //TODO Not right section, but when done playing, actually disconnect user self
            var Result = MusicBotService.GetSingleton().GetMusicQueue(Context.Guild.Id, 1);
            if (!string.IsNullOrWhiteSpace(Result.Item2))
                await ReplyAsync($"Error: {Result.Item2}").ConfigureAwait(false);
            else
                await ReplyAsync("", false, Result.Item1).ConfigureAwait(false);
        }

        [Command("playerqueue", RunMode = RunMode.Async)]
        public async Task Queue(int Page)
        {
            var Result = MusicBotService.GetSingleton().GetMusicQueue(Context.Guild.Id, Page);
            if (!string.IsNullOrWhiteSpace(Result.Item2))
                await ReplyAsync($"Error: {Result.Item2}").ConfigureAwait(false);
            else
                await ReplyAsync("", false, Result.Item1).ConfigureAwait(false);
        }

        /*
        [Command("vctest1", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task VCTest1([Remainder] string Link)
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            try
            {
                _ = ReplyAsync($"Program is now querying, please wait warmly...");
                var Embed = await DisplayEmbed.DisplayAddSong(Context.User.Id, Context.Guild.Id, Link);
                await ReplyAsync("", false, Embed.Item1);

                //if (MusicPlayers.Where(o => o.Server == Context.Guild.Id).Count() == 0)
                //    MusicPlayers.Add(new Player(Context.Guild.Id, await AudioChannel.ConnectAsync()));

                // Wait before downloading...
                var Result2 = await YoutubeService.GetSingleton().Download(Link, Context.Guild.Id);
                AudioService ads = AudioService.GetSingleton();
                await ads.JoinAudio(Context.Guild, AudioChannel);
                await ads.SendAudioAsync(Context.Guild, Context.Channel, Result2);
                await ads.LeaveAudio(Context.Guild);
                File.Delete(Result2);
            }
            catch (Exception ex)
            {
                _ = ReplyAsync($"Error: {ex.Message}");
            }
        }
        

        [Command("vctest2", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task VCTest2([Remainder] string song)
        {
            var AudioChannel = (Context.User as IVoiceState)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            AudioService ads = AudioService.GetSingleton();
            await ads.JoinAudio(Context.Guild, AudioChannel);
            await ads.SendAudioAsync(Context.Guild, Context.Channel, song);
            await ads.LeaveAudio(Context.Guild);
        }
        */
    }
}
