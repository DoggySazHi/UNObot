using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Plugins;
using UNObot.Plugins.Attributes;

namespace UNObot.Modules
{
    public class FunCommands : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger _logger;
        
        internal FunCommands(ILogger logger)
        {
            _logger = logger;
        }

        [Command("gulag", RunMode = RunMode.Async)]
        [Help(new[] {".gulag"}, "Blyat.", false, "UNObot 1.4")]
        internal async Task Gulag() => await Gulag("" + Context.User.Id);

        [Command("gulag", RunMode = RunMode.Async)]
        [Help(new[] {".gulag (user)"}, "Blyat.", false, "UNObot 1.4")]
        internal async Task Gulag(string user)
        {
            //extraclean
            user = user.Trim(' ', '<', '>', '!', '@');
            await ReplyAsync($"<@{user}> has been sent to gulag and has all of his cards converted to red blyats.");
        }

        [Command("nepnep", RunMode = RunMode.Async)]
        [Help(new[] {".nepnep"}, "Wait, how did this command get in here?", false, "UNObot 1.4")]
        internal async Task Nep()
        {
            await ReplyAsync("You got me there at \"nep\".");
        }

        [Command("ugay", RunMode = RunMode.Async)]
        [Alias("u gay", "you gay", "you're gay")]
        [Help(new[] {".ugay"}, "That's not very nice. >:[", false, "UNObot 0.1")]
        internal async Task Ugay()
        {
            await ReplyAsync(
                $"<@{Context.User.Id}> no u\n");
        }

        [Command("no u", RunMode = RunMode.Async)]
        [Alias("nou")]
        [Help(new[] {".no u"}, "Fite me m8", false, "UNObot 1.0")]
        internal async Task Easteregg1()
        {
            await ReplyAsync($"I claim that <@{Context.User.Id}> is triple gay. Say \"No U\" again, uh...");
        }

        [Command("upupdowndownleftrightleftrightbastart", RunMode = RunMode.Async)]
        [Help(new[] {".upupdowndownleftrightleftrightbastart"}, "Wow, an ancient easter egg. It's still ancient.",
            false, "UNObot 1.4")]
        internal async Task OldEasterEgg()
        {
            await ReplyAsync("...Did you seriously think that would work?");
        }

        [Command("moltthink", RunMode = RunMode.Async)]
        [Help(new[] {".moltthink"}, "Think like Molt.", true, "UNObot 3.0 Beta 1")]
        internal async Task MoltThink()
        {
            await ReplyAsync("<:moltthink:471842854591791104>");
        }

        [Command("moltthinkreact", RunMode = RunMode.Async)]
        [Help(new[] {".moltthinkreact"}, "React by thinking as Molt.", true, "UNObot 3.0 Beta 1")]
        internal async Task MoltThinkReact()
        {
            await MoltThinkReact(1);
        }

        [Command("moltthinkreact", RunMode = RunMode.Async)]
        [Help(new[] {".moltthinkreact (number of messages)"}, "React by thinking as Molt.", true, "UNObot 3.0 Beta 1")]
        internal async Task MoltThinkReact(int numMessages)
        {
            IEmote emote = await Context.Client.GetGuild(420005591155605535).GetEmoteAsync(471842854591791104);
            await BaseReact(numMessages, emote);
        }

        [Command("oof", RunMode = RunMode.Async)]
        [Help(new[] {".oof"}, "Oof.", true, "UNObot 3.0 Beta 1")]
        internal async Task Oof()
        {
            await ReplyAsync("<:oof:559961296418635776>");
        }

        [Command("oofreact", RunMode = RunMode.Async)]
        [Help(new[] {".oofreact"}, "Damn, oof.", true, "UNObot 3.0 Beta 1")]
        internal async Task OofReact()
        {
            await OofReact(1);
        }

        [Command("oofreact", RunMode = RunMode.Async)]
        [Help(new[] {".oofreact (number of messages)"}, "Damn, oof.", true, "UNObot 3.0 Beta 1")]
        internal async Task OofReact(int numMessages)
        {
            IEmote emote = await Context.Client.GetGuild(420005591155605535).GetEmoteAsync(559961296418635776);
            await BaseReact(numMessages, emote);
        }

        [Command("calculateemote", RunMode = RunMode.Async)]
        [RequireOwner]
        [DisableDMs]
        internal async Task CalculateEmote([Remainder] string input)
        {
            await ReplyAsync($"Server: ``{Context.Guild.Id}`` emoteID: ``{input}``").ConfigureAwait(false);
        }

        [Command("emote", RunMode = RunMode.Async)]
        [RequireOwner]
        internal async Task Emote(ulong server, ulong emoteId)
        {
            try
            {
                IEmote emote = await Context.Client.GetGuild(server).GetEmoteAsync(emoteId);
                await ReplyAsync(emote.ToString());
            }
            catch (Exception)
            {
                await ReplyAsync("Failed to get emote!");
            }
        }

        [Command("emotereact", RunMode = RunMode.Async)]
        [RequireOwner]
        internal async Task EmoteReact(ulong server, ulong emoteId, int numMessages)
        {
            try
            {
                IEmote emote = await Context.Client.GetGuild(server).GetEmoteAsync(emoteId);
                await BaseReact(numMessages, emote);
            }
            catch (Exception)
            {
                await ReplyAsync("Failed to get emote!");
            }
        }

        internal async Task BaseReact(int numMessages, IEmote emote)
        {
            var messages = await Context.Channel.GetMessagesAsync(numMessages + 1).FlattenAsync();
            var message = messages.Last();

            if (!(message is IUserMessage updatedMessage))
            {
                await ReplyAsync("Couldn't add reaction!");
                return;
            }

            //IEmote emote = await Context.Client.GetGuild(420005591155605535).GetEmoteAsync(471842854591791104);
            //emoteID emote = emoteID emote = emoteID.Parse("<:dotnet:232902710280716288>");
            //Emoji emoji = new Emoji("👍");
            await updatedMessage.AddReactionAsync(emote).ConfigureAwait(false);

            var userMessage = await Context.Channel.GetMessagesAsync(1).FlattenAsync();

            if (!(Context.Channel is ITextChannel textchannel))
            {
                _logger.Log(LogSeverity.Warning, "error cast");
                return;
            }

            await textchannel.DeleteMessagesAsync(userMessage);
        }
    }
}