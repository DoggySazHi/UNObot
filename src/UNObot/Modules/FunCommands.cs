using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using UNObot.Services;

namespace UNObot.Modules
{
    public class FunCommands : ModuleBase<SocketCommandContext>
    {
        [Command("gulag", RunMode = RunMode.Async)]
        [Help(new[] { ".gulag" }, "Blyat.", false, "UNObot 1.4")]
        public async Task Gulag()
        {
            await ReplyAsync($"<@{Context.User.Id}> has been sent to gulag and has all of his cards converted to red blyats.");
        }

        [Command("gulag", RunMode = RunMode.Async)]
        [Help(new[] { ".gulag (user)" }, "Blyat.", false, "UNObot 1.4")]
        public async Task Gulag2(string user)
        {
            //extraclean
            user = user.Trim(new Char[] { ' ', '<', '>', '!', '@' });
            await ReplyAsync($"<@{user}> has been sent to gulag and has all of his cards converted to red blyats.");
        }

        [Command("nepnep", RunMode = RunMode.Async)]
        [Help(new[] { ".nepnep" }, "Wait, how did this command get in here?", false, "UNObot 1.4")]
        public async Task Nep()
        {
            await ReplyAsync($"You got me there at \"nep\".");
        }

        [Command("ugay", RunMode = RunMode.Async), Alias("u gay", "you gay", "you're gay")]
        [Help(new[] { ".ugay" }, "That's not very nice. >:[", false, "UNObot 0.1")]
        public async Task Ugay()
            => await ReplyAsync(
                $"<@{Context.User.Id}> no u\n");

        [Command("no u", RunMode = RunMode.Async), Alias("nou")]
        [Help(new[] { ".no u" }, "Fite me m8", false, "UNObot 1.0")]
        public async Task Easteregg1()
        {
            await ReplyAsync($"I claim that <@{Context.User.Id}> is triple gay. Say \"No U\" again, uh...");
        }

        [Command("upupdowndownleftrightleftrightbastart", RunMode = RunMode.Async)]
        [Help(new[] { ".upupdowndownleftrightleftrightbastart" }, "Wow, an ancient easter egg. It's still ancient.", false, "UNObot 1.4")]
        public async Task OldEasterEgg()
            => await ReplyAsync("...Did you seriously think that would work?");

        [Command("moltthink", RunMode = RunMode.Async)]
        [Help(new[] { ".moltthink" }, "Think like Molt.", true, "UNObot 3.0 Beta 1")]
        public async Task MoltThink()
        {
            await ReplyAsync("<:moltthink:471842854591791104>");
        }
        [Command("moltthinkreact", RunMode = RunMode.Async)]
        [Help(new[] { ".moltthinkreact" }, "React by thinking as Molt.", true, "UNObot 3.0 Beta 1")]
        public async Task MoltThinkReact()
            => await MoltThinkReact(1);

        [Command("moltthinkreact", RunMode = RunMode.Async)]
        [Help(new[] { ".moltthinkreact (number of messages)" }, "React by thinking as Molt.", true, "UNObot 3.0 Beta 1")]
        public async Task MoltThinkReact(int numMessages)
        {
            IEmote emote = await Context.Client.GetGuild(420005591155605535).GetEmoteAsync(471842854591791104);
            await BaseReact(numMessages, emote);
        }

        [Command("oof", RunMode = RunMode.Async)]
        [Help(new[] { ".oof" }, "Oof.", true, "UNObot 3.0 Beta 1")]
        public async Task OOF()
        {
            await ReplyAsync("<:oof:559961296418635776>");
        }

        [Command("oofreact", RunMode = RunMode.Async)]
        [Help(new[] { ".oofreact" }, "Damn, oof.", true, "UNObot 3.0 Beta 1")]
        public async Task OOFReact()
            => await OOFReact(1);

        [Command("oofreact", RunMode = RunMode.Async)]
        [Help(new[] { ".oofreact (number of messages)" }, "Damn, oof.", true, "UNObot 3.0 Beta 1")]
        public async Task OOFReact(int numMessages)
        {
            IEmote emote = await Context.Client.GetGuild(420005591155605535).GetEmoteAsync(559961296418635776);
            await BaseReact(numMessages, emote);
        }

        [Command("calculateemote", RunMode = RunMode.Async)]
        [RequireOwner]
        [DisableDMs]
        public async Task CalculateEmote([Remainder] string Input)
        {
            await ReplyAsync($"Server: ``{Context.Guild.Id}`` Emote: ``{Input}``").ConfigureAwait(false);
        }

        [Command("emote", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task Emote(ulong Server, ulong Emote)
        {
            try
            {
                IEmote emote = await Context.Client.GetGuild(Server).GetEmoteAsync(Emote);
                await ReplyAsync(emote.ToString());
            }
            catch (Exception)
            {
                await ReplyAsync("Failed to get emote!");
            }
        }

        [Command("emotereact", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task EmoteReact(ulong Server, ulong Emote, int numMessages)
        {
            try
            {
                IEmote emote = await Context.Client.GetGuild(Server).GetEmoteAsync(Emote);
                await BaseReact(numMessages, emote);
            }
            catch (Exception)
            {
                await ReplyAsync("Failed to get emote!");
            }
        }

        public async Task BaseReact(int numMessages, IEmote emote)
        {
            var messages = await Context.Channel.GetMessagesAsync(numMessages + 1).FlattenAsync();
            var message = messages.Last();

            if (!(message is IUserMessage updatedMessage))
            {
                await ReplyAsync("Couldn't add reaction!");
                return;
            }
            //IEmote emote = await Context.Client.GetGuild(420005591155605535).GetEmoteAsync(471842854591791104);
            //Emote emote = Emote emote = Emote.Parse("<:dotnet:232902710280716288>");
            //Emoji emoji = new Emoji("👍");
            await updatedMessage.AddReactionAsync(emote).ConfigureAwait(false);

            var userMessage = await Context.Channel.GetMessagesAsync(1).FlattenAsync();

            if (!(Context.Channel is ITextChannel textchannel))
            {
                LoggerService.Log(LogSeverity.Warning, "error cast");
                return;
            }

            await textchannel.DeleteMessagesAsync(userMessage);
        }
    }
}
