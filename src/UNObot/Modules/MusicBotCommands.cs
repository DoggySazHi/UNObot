using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Plugins.Attributes;
using UNObot.Services;

namespace UNObot.Modules
{
    public class MusicBotCommands : ModuleBase<SocketCommandContext>
    {
        [Command("playerplay", RunMode = RunMode.Async)]
        [Alias("playmusic", "pm")]
        [DisableDMs]
        [Help(new[] {".playerplay", ".playerplay (link)", ".playerplay (search)"},
            "Wait, MusicBot functionality in UNObot?", true, "UNObot 3.2 Beta 1")]
        public async Task PlayMusic([Remainder] string link)
        {
            var audioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (audioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var loading = await ReplyAsync("Loading music... (this may take a while, but not more than 10 seconds)");

            var result = await MusicBotService.GetSingleton()
                .AddList(Context.User.Id, Context.Guild.Id, link, audioChannel, Context.Channel);
            if (result.Item1 == null)
                result = await MusicBotService.GetSingleton()
                    .Add(Context.User.Id, Context.Guild.Id, link, audioChannel, Context.Channel);
            if (result.Item1 == null)
                result = await MusicBotService.GetSingleton()
                    .Search(Context.User.Id, Context.Guild.Id, link, audioChannel, Context.Channel);
            if (result.Item1 == null)
            {
                await loading.ModifyAsync(o => o.Content = $"Error: {result.Item2 ?? "I... don't know."}");
                return;
            }

            var message = result.Item2 ?? "";
            if (result.Item1.Url.Contains("ixMHG0DIAK4") && Context.User.Id == 278524552462598145)
                message += "Aw crap, here we go again...";

            await loading.ModifyAsync(o =>
            {
                o.Content = message;
                o.Embed = result.Item1;
            });
        }

        [Command("playerplay", RunMode = RunMode.Async)]
        [Alias("playmusic", "pm")]
        [DisableDMs]
        public async Task Play()
        {
            var audioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (audioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var result = await MusicBotService.GetSingleton().Play(Context.User.Id, Context.Guild.Id, audioChannel);
            await ReplyAsync(result);
        }

        [Command("playerplaytop", RunMode = RunMode.Async)]
        [Alias("playmusictop", "pmt")]
        [DisableDMs]
        [Help(new[] {".playerplay", ".playerplay (link)", ".playerplay (search)"},
            "Wait, MusicBot functionality in UNObot?", true, "UNObot 3.2 Beta 1")]
        public async Task PlayMusicTop([Remainder] string link)
        {
            var audioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (audioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var loading = await ReplyAsync("Loading music... (this may take a while, but not more than 10 seconds)");

            var result = await MusicBotService.GetSingleton().AddList(Context.User.Id, Context.Guild.Id, link,
                audioChannel, Context.Channel, true);
            if (result.Item1 == null)
                result = await MusicBotService.GetSingleton().Add(Context.User.Id, Context.Guild.Id, link, audioChannel,
                    Context.Channel, true);
            if (result.Item1 == null)
                result = await MusicBotService.GetSingleton().Search(Context.User.Id, Context.Guild.Id, link,
                    audioChannel, Context.Channel, true);
            if (result.Item1 == null)
            {
                await loading.ModifyAsync(o => o.Content = $"Error: {result.Item2 ?? "I... don't know."}");
                return;
            }

            var message = result.Item2 ?? "";
            if (result.Item1.Url.Contains("ixMHG0DIAK4") && Context.User.Id == 278524552462598145)
                message += "Aw crap, here we go again...";

            await loading.ModifyAsync(o =>
            {
                o.Content = message;
                o.Embed = result.Item1;
            });
        }

        [Command("playerpause", RunMode = RunMode.Async)]
        [Alias("pause", "pauseplayer")]
        [DisableDMs]
        [Help(new[] {".playerpause"}, "Pause the player.", true, "UNObot 3.2 Beta 2")]
        public async Task Pause()
        {
            var audioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (audioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var result = await MusicBotService.GetSingleton().Pause(Context.User.Id, Context.Guild.Id, audioChannel);
            await ReplyAsync(result);
        }

        [Command("playershuffle", RunMode = RunMode.Async)]
        [Alias("shuffle")]
        [DisableDMs]
        [Help(new[] {".playershuffle"}, "Shuffle the player.", true, "UNObot 3.2 Beta 3")]
        public async Task Shuffle()
        {
            var audioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (audioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var result = await MusicBotService.GetSingleton().Shuffle(Context.User.Id, Context.Guild.Id, audioChannel);
            await ReplyAsync(result);
        }

        [Command("playerskip", RunMode = RunMode.Async)]
        [DisableDMs]
        [Help(new[] {".playerskip"}, "Skip the current song.", true, "UNObot 3.2 Beta 3")]
        public async Task Skip()
        {
            var audioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (audioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var result = await MusicBotService.GetSingleton().Skip(Context.User.Id, Context.Guild.Id, audioChannel);
            if (!string.IsNullOrWhiteSpace(result))
                await ReplyAsync(result);
        }

        [Command("playerloop", RunMode = RunMode.Async)]
        [DisableDMs]
        [Help(new[] {".playerloop"}, "Loop the current song.", true, "UNObot 3.2 Beta 4")]
        public async Task Loop()
        {
            var audioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (audioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var result = await MusicBotService.GetSingleton()
                .ToggleLoop(Context.User.Id, Context.Guild.Id, audioChannel);
            await ReplyAsync(result);
        }

        [Command("playerloopqueue", RunMode = RunMode.Async)]
        [DisableDMs]
        [Help(new[] {".playerloopqueue"}, "Loop the entire queue.", true, "UNObot 3.2 Beta 4")]
        public async Task LoopQueue()
        {
            var audioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (audioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var result = await MusicBotService.GetSingleton()
                .ToggleLoopQueue(Context.User.Id, Context.Guild.Id, audioChannel);
            await ReplyAsync(result);
        }

        [Command("playerdc", RunMode = RunMode.Async)]
        [Alias("dc", "playerdisconnect")]
        [DisableDMs]
        [Help(new[] {".playerdc"}, "Disconnect the bot from the channel.", true, "UNObot 3.2 Beta 4")]
        public async Task Disconnect()
        {
            var audioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (audioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(false);
                return;
            }

            var result = await MusicBotService.GetSingleton()
                .Disconnect(Context.User.Id, Context.Guild.Id, audioChannel).ConfigureAwait(true);
            await ReplyAsync(result).ConfigureAwait(false);
        }

        [Command("playernp", RunMode = RunMode.Async)]
        [Alias("playernowplaying", "np")]
        [DisableDMs]
        [Help(new[] {".playernp"}, "Get the song playing.", true, "UNObot 3.2 Beta 2")]
        public async Task NowPlaying()
        {
            var result = MusicBotService.GetSingleton().GetNowPlaying(Context.Guild.Id);
            if (!string.IsNullOrWhiteSpace(result.Item2))
                await ReplyAsync($"Error: {result.Item2}");
            else
                await ReplyAsync("", false, result.Item1);
        }

        [Command("playerqueue", RunMode = RunMode.Async)]
        [Alias("pq")]
        [DisableDMs]
        [Help(new[] {".playerqueue", ".playerqueue (page)"}, "Get the songs in the player's queue.", true,
            "UNObot 3.2 Beta 2")]
        public async Task Queue()
        {
            var result = MusicBotService.GetSingleton().GetMusicQueue(Context.Guild.Id, 1);
            if (!string.IsNullOrWhiteSpace(result.Item2))
                await ReplyAsync($"Error: {result.Item2}").ConfigureAwait(false);
            else
                await ReplyAsync("", false, result.Item1).ConfigureAwait(false);
        }

        [Command("playerqueue", RunMode = RunMode.Async)]
        [Alias("pq")]
        [DisableDMs]
        public async Task Queue(int page)
        {
            var result = MusicBotService.GetSingleton().GetMusicQueue(Context.Guild.Id, page);
            if (!string.IsNullOrWhiteSpace(result.Item2))
                await ReplyAsync($"Error: {result.Item2}").ConfigureAwait(false);
            else
                await ReplyAsync("", false, result.Item1).ConfigureAwait(false);
        }

        [Command("playerremove", RunMode = RunMode.Async)]
        [Alias("prm, rm")]
        [DisableDMs]
        [Help(new[] {".playerremove"}, "Remove the song.", true, "UNObot 3.2 Beta 3")]
        public async Task Remove(int index)
        {
            var audioChannel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (audioChannel == null)
            {
                await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
                return;
            }

            var result = await MusicBotService.GetSingleton()
                .Remove(Context.User.Id, Context.Guild.Id, audioChannel, index);
            await ReplyAsync(result);
        }

        /*
        [Command("vctest1", RunMode = RunMode.Async)]
        [DisableDMsAttribute]
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
        [DisableDMsAttribute]
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