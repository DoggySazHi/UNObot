using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using UNObot.Services;

namespace UNObot.Modules
{
    public class MusicBotCommands : ModuleBase<SocketCommandContext>
    {
        [Command("playerplay", RunMode = RunMode.Async), Alias("playmusic", "pm")]
        [DisableDMs]
        [Help(new[] { ".playerplay", ".playerplay (link)", ".playerplay (search)" }, "Wait, MusicBot functionality in UNObot?", true, "UNObot 3.2 Beta 1")]
        public async Task PlayMusic([Remainder] string Link)
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Loading = await ReplyAsync("Loading music... (this may take a while, but not more than 10 seconds)");

            var Result = await MusicBotService.GetSingleton().AddList(Context.User.Id, Context.Guild.Id, Link, AudioChannel, Context.Channel);
            if (Result.Item1 == null)
                Result = await MusicBotService.GetSingleton().Add(Context.User.Id, Context.Guild.Id, Link, AudioChannel, Context.Channel);
            if (Result.Item1 == null)
                Result = await MusicBotService.GetSingleton().Search(Context.User.Id, Context.Guild.Id, Link, AudioChannel, Context.Channel);
            if (Result.Item1 == null)
            {
                await Loading.ModifyAsync(o => o.Content = $"Error: {Result.Item2 ?? "I... don't know."}");
                return;
            }
            string Message = Result.Item2 ?? "";
            if (Result.Item1.Url.Contains("ixMHG0DIAK4") && Context.User.Id == 278524552462598145)
                Message += "Aw crap, here we go again...";

            await Loading.ModifyAsync(o =>
            {
                o.Content = Message;
                o.Embed = Result.Item1;
            });
        }

        [Command("playerplay", RunMode = RunMode.Async), Alias("playmusic", "pm")]
        [DisableDMs]
        public async Task Play()
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Result = await MusicBotService.GetSingleton().Play(Context.User.Id, Context.Guild.Id, AudioChannel);
            await ReplyAsync(Result);
        }

        [Command("playerplaytop", RunMode = RunMode.Async), Alias("playmusictop", "pmt")]
        [DisableDMs]
        [Help(new[] { ".playerplay", ".playerplay (link)", ".playerplay (search)" }, "Wait, MusicBot functionality in UNObot?", true, "UNObot 3.2 Beta 1")]
        public async Task PlayMusicTop([Remainder] string Link)
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Loading = await ReplyAsync("Loading music... (this may take a while, but not more than 10 seconds)");

            var Result = await MusicBotService.GetSingleton().AddList(Context.User.Id, Context.Guild.Id, Link, AudioChannel, Context.Channel, true);
            if (Result.Item1 == null)
                Result = await MusicBotService.GetSingleton().Add(Context.User.Id, Context.Guild.Id, Link, AudioChannel, Context.Channel, true);
            if (Result.Item1 == null)
                Result = await MusicBotService.GetSingleton().Search(Context.User.Id, Context.Guild.Id, Link, AudioChannel, Context.Channel, true);
            if (Result.Item1 == null)
            {
                await Loading.ModifyAsync(o => o.Content = $"Error: {Result.Item2 ?? "I... don't know."}");
                return;
            }
            string Message = Result.Item2 ?? "";
            if (Result.Item1.Url.Contains("ixMHG0DIAK4") && Context.User.Id == 278524552462598145)
                Message += "Aw crap, here we go again...";

            await Loading.ModifyAsync(o =>
            {
                o.Content = Message;
                o.Embed = Result.Item1;
            });
        }

        [Command("playerpause", RunMode = RunMode.Async), Alias("pause", "pauseplayer")]
        [DisableDMs]
        [Help(new[] { ".playerpause" }, "Pause the player.", true, "UNObot 3.2 Beta 2")]
        public async Task Pause()
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Result = await MusicBotService.GetSingleton().Pause(Context.User.Id, Context.Guild.Id, AudioChannel);
            await ReplyAsync(Result);
        }

        [Command("playershuffle", RunMode = RunMode.Async), Alias("shuffle")]
        [DisableDMs]
        [Help(new[] { ".playershuffle" }, "Shuffle the player.", true, "UNObot 3.2 Beta 3")]
        public async Task Shuffle()
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Result = await MusicBotService.GetSingleton().Shuffle(Context.User.Id, Context.Guild.Id, AudioChannel);
            await ReplyAsync(Result);
        }

        [Command("playerskip", RunMode = RunMode.Async)]
        [DisableDMs]
        [Help(new[] { ".playerskip" }, "Skip the current song.", true, "UNObot 3.2 Beta 3")]
        public async Task Skip()
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Result = await MusicBotService.GetSingleton().Skip(Context.User.Id, Context.Guild.Id, AudioChannel);
            if(!string.IsNullOrWhiteSpace(Result))
                await ReplyAsync(Result);
        }

        [Command("playerloop", RunMode = RunMode.Async)]
        [DisableDMs]
        [Help(new[] { ".playerloop" }, "Loop the current song.", true, "UNObot 3.2 Beta 4")]
        public async Task Loop()
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }
            var Result = await MusicBotService.GetSingleton().ToggleLoop(Context.User.Id, Context.Guild.Id, AudioChannel);
            await ReplyAsync(Result);
        }

        [Command("playerloopqueue", RunMode = RunMode.Async)]
        [DisableDMs]
        [Help(new[] { ".playerloopqueue" }, "Loop the entire queue.", true, "UNObot 3.2 Beta 4")]
        public async Task LoopQueue()
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Result = await MusicBotService.GetSingleton().ToggleLoopQueue(Context.User.Id, Context.Guild.Id, AudioChannel);
            await ReplyAsync(Result);
        }

        [Command("playerdc", RunMode = RunMode.Async), Alias("dc", "playerdisconnect")]
        [DisableDMs]
        [Help(new[] { ".playerdc" }, "Disconnect the bot from the channel.", true, "UNObot 3.2 Beta 4")]
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
        [DisableDMs]
        [Help(new[] { ".playernp" }, "Get the song playing.", true, "UNObot 3.2 Beta 2")]
        public async Task NowPlaying()
        {
            var Result = MusicBotService.GetSingleton().GetNowPlaying(Context.Guild.Id);
            if (!string.IsNullOrWhiteSpace(Result.Item2))
                await ReplyAsync($"Error: {Result.Item2}");
            else
                await ReplyAsync("", false, Result.Item1);
        }

        [Command("playerqueue", RunMode = RunMode.Async), Alias("pq")]
        [DisableDMs]
        [Help(new[] { ".playerqueue", ".playerqueue (page)" }, "Get the songs in the player's queue.", true, "UNObot 3.2 Beta 2")]
        public async Task Queue()
        {
            var Result = MusicBotService.GetSingleton().GetMusicQueue(Context.Guild.Id, 1);
            if (!string.IsNullOrWhiteSpace(Result.Item2))
                await ReplyAsync($"Error: {Result.Item2}").ConfigureAwait(false);
            else
                await ReplyAsync("", false, Result.Item1).ConfigureAwait(false);
        }

        [Command("playerqueue", RunMode = RunMode.Async), Alias("pq")]
        [DisableDMs]
        public async Task Queue(int Page)
        {
            var Result = MusicBotService.GetSingleton().GetMusicQueue(Context.Guild.Id, Page);
            if (!string.IsNullOrWhiteSpace(Result.Item2))
                await ReplyAsync($"Error: {Result.Item2}").ConfigureAwait(false);
            else
                await ReplyAsync("", false, Result.Item1).ConfigureAwait(false);
        }

        [Command("playerremove", RunMode = RunMode.Async), Alias("prm, rm")]
        [DisableDMs]
        [Help(new[] { ".playerremove" }, "Remove the song.", true, "UNObot 3.2 Beta 3")]
        public async Task Remove(int Index)
        {
            var AudioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (AudioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var Result = await MusicBotService.GetSingleton().Remove(Context.User.Id, Context.Guild.Id, AudioChannel, Index);
            await ReplyAsync(Result);
        }

        /*
        [Command("vctest1", RunMode = RunMode.Async)]
        [DisableDMs]
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
        [DisableDMs]
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
