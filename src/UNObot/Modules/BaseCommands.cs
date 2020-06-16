using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Services;

namespace UNObot.Modules
{
    public class BaseCommands : ModuleBase<SocketCommandContext>
    {
        [Command("info", RunMode = RunMode.Async)]
        [Help(new[] {".info"}, "Get the current version of UNObot.", true, "UNObot 1.0")]
        public async Task Info()
        {
            var (commit, build) = Program.ReadCommitBuild();
            var output =
                $"{Context.Client.CurrentUser.Username} - Created by DoggySazHi\nVersion {Program.Version}\nCurrent Time (PST): {DateTime.Now.ToString(CultureInfo.InvariantCulture)}" +
                $"\n\nCommit {Program.Commit}\nBuild #{Program.Build}";
            if (commit != Program.Commit)
                output += $"\nThere is a pending update: Commit {commit} Build #{build}.";
            await ReplyAsync(output);
        }

        [Command("testperms", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        [DisableDMs]
        [Help(new[] {".testperms"}, "Show all permissions that UNObot has. Added for security reasons.", true,
            "UNObot 1.4")]
        public async Task TestPerms()
        {
            var response = "Permissions:\n";
            var user = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var perms = user.GetPermissions(Context.Channel as IGuildChannel);
            foreach (var c in perms.ToList()) response += $"- {c.ToString()} | \n";
            await ReplyAsync(response);
        }

        [Command("dogtestperms", RunMode = RunMode.Async)]
        [RequireOwner]
        [DisableDMs]
        [Help(new[] {".dogtestperms"}, "Show all permissions that UNObot has. Added for security reasons.", false,
            "UNObot 1.4")]
        public async Task TestPerms2()
        {
            var response = "Permissions:\n";
            var user = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var perms = user.GetPermissions(Context.Channel as IGuildChannel);
            foreach (var c in perms.ToList()) response += $"- {c.ToString()} | \n";
            await ReplyAsync(response);
        }

        [Command("nick", RunMode = RunMode.Async)]
        [RequireOwner]
        [DisableDMs]
        [Help(new[] {".nick (nickname)"}, "Change the nickname of UNObot.", false, "UNObot 2.0")]
        public async Task ChangeNick(string newnick)
        {
            var user = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            await user.ModifyAsync(x => { x.Nickname = newnick; });
        }

        [Command("fullhelp", RunMode = RunMode.Async)]
        [Help(new[] {".fullhelp"}, "If you need help using help, you're truly lost.", true, "UNObot 1.0")]
        public async Task FullHelp()
        {
            if (!Context.IsPrivate)
                await ReplyAsync("HelpAttribute has been sent. Or, I think it has.");
            var response = "```Commands: @UNOBot#4308 command/ .command\n (Required) {May be required} [Optional]\n \n";
            foreach (var cmd in Program.Commands)
            {
                var oldResponse = response;
                if (cmd.Active)
                {
                    oldResponse = response;
                    response += $"- {cmd.CommandName}: {cmd.Help}\n";
                    if (cmd.Usages.Count > 0)
                        response += $"Usage(s): {string.Join(", ", cmd.Usages.ToArray())}\n";
                    if (cmd.Aliases.Count > 0)
                        response += $"Aliases: {string.Join(", ", cmd.Aliases.ToArray())}\n";
                    if (response.Length > 1996)
                    {
                        oldResponse += "```";
                        await Context.Message.Author.SendMessageAsync(oldResponse);
                        response = "```";
                        response += $"- {cmd.CommandName}: {cmd.Help}\n";
                        if (cmd.Usages.Count > 0)
                            response += $"Usage(s): {string.Join(", ", cmd.Usages.ToArray())}\n";
                        if (cmd.Aliases.Count > 0)
                            response += $"Aliases: {string.Join(", ", cmd.Aliases.ToArray())}\n";
                    }

                    response += "\n";
                }

                if (response.Length > 1996)
                {
                    response = oldResponse;
                    response += "```";
                    await Context.Message.Author.SendMessageAsync(response);
                    response = "```\n";
                }
            }

            response += "```";
            await Context.Message.Author.SendMessageAsync(response);
        }

        [Command("help", RunMode = RunMode.Async)]
        [Alias("ahh", "ahhh", "ahhhh", "commands", "command")]
        public async Task Help()
        {
            var r = ThreadSafeRandom.ThisThreadsRandom;
            var builder = new EmbedBuilder()
                .WithTitle("Quick-start guide to UNObot")
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    var guildName = $"{Context.User.Username}'s DMs";
                    if (!Context.IsPrivate)
                        guildName = Context.Guild.Name;
                    author
                        .WithName($"Playing in {guildName}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Usages", "@UNOBot#4308 *commandtorun*\n.*commandtorun*")
                .AddField(".join", "Join a game in the current server.", true)
                .AddField(".start", "Start a game in the current server.\nYou must have joined beforehand.", true)
                .AddField(".play (color) (value) [new color]",
                    "Play a card, assuming if it's your turn.\nIf you are playing a Wild card, also\nadd the color to change to.",
                    true)
                .AddField(".hand", "See which cards you have, as well as\nwhich ones you can play.", true)
                .AddField(".game", "See everything about the current game.\nThis also shows the current card.", true)
                .AddField(".draw", "Draw a card. Duh. Can be used indefinitely.", true)
                .AddField(".quickplay", "Auto-magically draw and play the first valid card that comes out.", true)
                .AddField(".fullhelp", "See an extended listing of commands.\nNice!", true);
            var embed = builder.Build();
            await Context.Channel.SendMessageAsync(
                ":+1: got cha fam",
                embed: embed);
        }

        [Command("playerhelp", RunMode = RunMode.Async)]
        [Alias("playercommand", "playercommands", "playercmd", "playercmds")]
        public async Task PlayerHelp()
        {
            var r = ThreadSafeRandom.ThisThreadsRandom;
            var builder = new EmbedBuilder()
                .WithTitle("Quick-start guide to UNObot-MusicBot")
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    var guildName = $"{Context.User.Username}'s DMs";
                    if (!Context.IsPrivate)
                        guildName = Context.Guild.Name;
                    author
                        .WithName($"Playing in {guildName}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Usages", "@UNOBot#4308 *commandtorun*\n.*commandtorun*")
                .AddField(".playerplay (Link)", "Add a song to the queue, or continue if the player is paused.", true)
                .AddField(".playerpause", "Pause the player. Duh.", true)
                .AddField(".playershuffle", "Shuffle the contents of the queue.", true)
                .AddField(".playerskip", "Skip the current song playing to the next one in the queue.", true)
                .AddField(".playerqueue", "Display the contents of the queue.", true)
                .AddField(".playernp", "Find out what song is playing currently.", true)
                .AddField(".playerloop", "Loop the current song playing.", true)
                .AddField(".playerloopqueue", "Loop the contents of the queue.", true);
            var embed = builder.Build();
            await Context.Channel.SendMessageAsync(
                "",
                embed: embed);
        }

        [Command("help", RunMode = RunMode.Async)]
        [Alias("ahh", "ahhh", "ahhhh")]
        [Help(new[] {".help (command)"}, "If you need help using help, you're truly lost.", true, "UNObot 1.0")]
        public async Task Help(string cmdSearch)
        {
            var response = "";
            var index = Program.Commands.FindIndex(o => o.CommandName == cmdSearch);
            var index2 = Program.Commands.FindIndex(o => o.Aliases.Contains(cmdSearch));
            Command cmd;
            if (index >= 0)
            {
                cmd = Program.Commands[index];
            }
            else if (index2 >= 0)
            {
                cmd = Program.Commands[index2];
            }
            else
            {
                await ReplyAsync("Command was not found in the help list.");
                return;
            }

            response += "```";
            if (!cmd.Active)
                response += "Note: This command might be hidden or deprecated.\n";
            response += $"- {cmd.CommandName}: {cmd.Help}\n";
            if (cmd.Usages.Count > 0)
                response += $"Usage(s): {string.Join(", ", cmd.Usages.ToArray())}\n";
            response += $"Introduced in {cmd.Version}.\n";
            if (cmd.Aliases.Count > 0)
                response += $"Aliases: {string.Join(", ", cmd.Aliases.ToArray())}\n";
            response += "```";
            await ReplyAsync(response);
        }

        [Command("credits", RunMode = RunMode.Async)]
        [Alias("asdf")]
        [Help(new[] {".credits"}, "Wow, look at all of the victims during the making of this bot.", true, "UNObot 1.0")]
        public async Task Credits()
        {
            await ReplyAsync("UNObot: Programmed by DoggySazHi\n" +
                             "Tested by dabadcuber5, Aragami and Fm (ish)\n" +
                             "UNO card images from Wikipedia\n" +
                             "Created for the UBOWS server\n\n" +
                             "Stickerz was here.\n" +
                             "Blame LocalDisk and Harvest for any bugs.");
        }

        [Command("invite", RunMode = RunMode.Async)]
        [Help(new[] {".invite"}, "You actually want the bot? Wow.", true, "UNObot 3.1.4")]
        public async Task Invite()
        {
            await ReplyAsync("If you want to add this bot to your server, use this link: \n" +
                             "https://discordapp.com/api/oauth2/authorize?client_id=477616287997231105&permissions=8192&scope=bot");
        }
    }
}