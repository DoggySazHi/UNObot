using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Plugins;
using UNObot.Plugins.Attributes;
using UNObot.Plugins.Helpers;

namespace UNObot.Modules
{
    public class FunCommands : ModuleBase<UNObotCommandContext>
    {
        private readonly ILogger _logger;
        
        public FunCommands(ILogger logger)
        {
            _logger = logger;
        }

        [Command("gulag", RunMode = RunMode.Async)]
        [Help(new[] {".gulag"}, "Blyat.", false, "UNObot 1.4")]
        public async Task Gulag() => await Gulag("" + Context.User.Id);

        [Command("gulag", RunMode = RunMode.Async)]
        [Help(new[] {".gulag (user)"}, "Blyat.", false, "UNObot 1.4")]
        public async Task Gulag(string user)
        {
            // Remove all pinging separators
            user = user.Trim(' ', '<', '>', '!', '@');
            await ReplyAsync($"<@{user}> has been sent to gulag and has all of his cards converted to red blyats.");
        }

        [Command("nepnep", RunMode = RunMode.Async)]
        [Help(new[] {".nepnep"}, "Wait, how did this command get in here?", false, "UNObot 1.4")]
        public async Task Nep()
        {
            await ReplyAsync("You got me there at \"nep\".");
        }

        [Command("ugay", RunMode = RunMode.Async)]
        [Alias("u gay", "you gay", "you're gay")]
        [Help(new[] {".ugay"}, "That's not very nice. >:[", false, "UNObot 0.1")]
        public async Task Ugay()
        {
            await ReplyAsync(
                $"<@{Context.User.Id}> no u\n");
        }

        [Command("no u", RunMode = RunMode.Async)]
        [Alias("nou")]
        [Help(new[] {".no u"}, "Fite me m8", false, "UNObot 1.0")]
        public async Task Easteregg1()
        {
            await ReplyAsync($"I claim that <@{Context.User.Id}> is triple gay. Say \"No U\" again, uh...");
        }

        [Command("upupdowndownleftrightleftrightbastart", RunMode = RunMode.Async)]
        [Help(new[] {".upupdowndownleftrightleftrightbastart"}, "Wow, an ancient easter egg. It's still ancient.",
            false, "UNObot 1.4")]
        public async Task OldEasterEgg()
        {
            await ReplyAsync("...Did you seriously think that would work?");
        }

        [Command("moltthink", RunMode = RunMode.Async)]
        [Help(new[] {".moltthink"}, "Think like Molt.", true, "UNObot 3.0 Beta 1")]
        public async Task MoltThink()
        {
            await ReplyAsync("<:moltthink:471842854591791104>");
        }

        [Command("moltthinkreact", RunMode = RunMode.Async)]
        [Help(new[] {".moltthinkreact"}, "React by thinking as Molt.", true, "UNObot 3.0 Beta 1")]
        public async Task MoltThinkReact()
        {
            await MoltThinkReact(1);
        }

        [Command("moltthinkreact", RunMode = RunMode.Async)]
        [Help(new[] {".moltthinkreact (number of messages)"}, "React by thinking as Molt.", true, "UNObot 3.0 Beta 1")]
        public async Task MoltThinkReact(int numMessages)
        {
            IEmote emote = await Context.Client.GetGuild(420005591155605535).GetEmoteAsync(471842854591791104);
            await BaseReact(numMessages, emote);
        }

        [Command("oof", RunMode = RunMode.Async)]
        [Help(new[] {".oof"}, "Oof.", true, "UNObot 3.0 Beta 1")]
        public async Task Oof()
        {
            await ReplyAsync("<:oof:559961296418635776>");
        }

        [Command("oofreact", RunMode = RunMode.Async)]
        [Help(new[] {".oofreact"}, "Damn, oof.", true, "UNObot 3.0 Beta 1")]
        public async Task OofReact()
        {
            await OofReact(1);
        }

        [Command("oofreact", RunMode = RunMode.Async)]
        [Help(new[] {".oofreact (number of messages)"}, "Damn, oof.", true, "UNObot 3.0 Beta 1")]
        public async Task OofReact(int numMessages)
        {
            IEmote emote = await Context.Client.GetGuild(420005591155605535).GetEmoteAsync(559961296418635776);
            await BaseReact(numMessages, emote);
        }

        [Command("say", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task Say([Remainder] string input)
        {
            try
            {
                await ReplyAsync(input);
            }
            catch (Exception)
            {
                await PluginHelper.GhostMessage(Context, "Failed to get emote!");
            }
        }

        [Command("emotereact", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task EmoteReact(string input, int numMessages = 1)
        {
            try
            {
                if (ulong.TryParse(input, out _))
                    input = $"<:a:{input}>"; // Why can't we just use the ID directly?
                IEmote emote = Emote.Parse(input);
                await BaseReact(numMessages, emote);
            }
            catch (Exception)
            {
                await PluginHelper.GhostMessage(Context, "Failed to get emote!");
            }
        }

        private async Task BaseReact(int numMessages, IEmote emote)
        {
            var messages = await Context.Channel.GetMessagesAsync(numMessages + 1).FlattenAsync();
            var message = messages.Last();

            if (message is not IUserMessage updatedMessage)
            {
                await PluginHelper.GhostMessage(Context, "Reaction could not be added!");
                return;
            }

            //IEmote emote = await Context.Client.GetGuild(420005591155605535).GetEmoteAsync(471842854591791104);
            //emoteID emote = emoteID emote = emoteID.Parse("<:dotnet:232902710280716288>");
            //Emoji emoji = new Emoji("👍");
            await updatedMessage.AddReactionAsync(emote).ConfigureAwait(false);

            var userMessage = await Context.Channel.GetMessagesAsync(1).FlattenAsync();

            if (Context.Channel is not ITextChannel channel)
            {
                _logger.Log(LogSeverity.Warning, "It's not a text channel.");
                return;
            }

            await channel.DeleteMessagesAsync(userMessage);
        }
    }
}