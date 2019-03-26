using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

#pragma warning disable CS1701 // Assuming assembly reference matches identity
#pragma warning disable CS1702 // Assuming assembly reference matches identity

namespace UNObot.Modules
{
    public class Basecmds : ModuleBase<SocketCommandContext>
    {
        [Command("info", RunMode = RunMode.Async)]
        [Help(new string[] { ".info" }, "Get the current version of UNObot.", true, "UNObot 1.0")]
        public async Task Info()
        {
            await ReplyAsync(
                $"{Context.Client.CurrentUser.Username} - Created by DoggySazHi\nVersion {Program.version}\nCurrent Time (PST): {DateTime.Now.ToString()}");
        }

        [Command("gulag", RunMode = RunMode.Async)]
        [Help(new string[] { ".gulag" }, "Blyat.", false, "UNObot 1.4")]
        public async Task Gulag()
        {
            await ReplyAsync($"<@{Context.User.Id}> has been sent to gulag and has all of his cards converted to red blyats.");
        }

        [Command("gulag", RunMode = RunMode.Async)]
        [Help(new string[] { ".gulag (user)" }, "Blyat.", false, "UNObot 1.4")]
        public async Task Gulag2(string user)
        {
            //extraclean
            user = user.Trim(new Char[] { ' ', '<', '>', '!', '@' });
            await ReplyAsync($"<@{user}> has been sent to gulag and has all of his cards converted to red blyats.");
        }

        [Command("nepnep", RunMode = RunMode.Async)]
        [Help(new string[] { ".nepnep" }, "Wait, how did this command get in here?", false, "UNObot 1.4")]
        public async Task Nep()
        {
            await ReplyAsync($"You got me there at \"nep\".");
        }

        [Command("ugay", RunMode = RunMode.Async), Alias("u gay", "you gay", "you're gay")]
        [Help(new string[] { ".ugay" }, "That's not very nice. >:[", false, "UNObot 0.1")]
        public async Task Ugay()
            => await ReplyAsync(
                $"<@{Context.User.Id}> no u\n");

        [Command("no u", RunMode = RunMode.Async), Alias("nou")]
        [Help(new string[] { ".no u" }, "Fite me m8", false, "UNObot 1.0")]
        public async Task Easteregg1()
        {
            await ReplyAsync($"I claim that <@{Context.User.Id}> is triple gay. Say \"No U\" again, uh...");
        }

        [Command("testperms", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageGuild)]
        [Help(new string[] { ".testperms" }, "Show all permissions that UNObot has. Added for security reasons.", true, "UNObot 1.4")]
        public async Task TestPerms()
        {
            string response = "Permissions:\n";
            var User = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var Perms = User.GetPermissions(Context.Channel as IGuildChannel);
            foreach (ChannelPermission c in Perms.ToList())
            {
                response += $"- {c.ToString()} | \n";
            }
            await ReplyAsync(response);
        }
        [Command("dogtestperms", RunMode = RunMode.Async), RequireOwner]
        public async Task TestPerms2()
        {
            string response = "Permissions:\n";
            var User = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var Perms = User.GetPermissions(Context.Channel as IGuildChannel);
            foreach (ChannelPermission c in Perms.ToList())
            {
                response += $"- {c.ToString()} | \n";
            }
            await ReplyAsync(response);
        }
        [Command("nick", RunMode = RunMode.Async), RequireOwner]
        [Help(new string[] { ".nick (nickname)" }, "Change the nickname of UNObot.", false, "UNObot 2.0")]
        public async Task ChangeNick(string newnick)
        {
            var User = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            await User.ModifyAsync(x =>
            {
                x.Nickname = newnick;
            });
        }

        [Command("upupdowndownleftrightleftrightbastart", RunMode = RunMode.Async)]
        [Help(new string[] { ".upupdowndownleftrightleftrightbastart" }, "Wow, an ancient easter egg. It's still ancient.", false, "UNObot 1.4")]
        public async Task OldEasterEgg()
            => await ReplyAsync("lol, that's outdated");

        [Command("fullhelp", RunMode = RunMode.Async)]
        [Help(new string[] { ".fullhelp" }, "If you need help using help, you're truly lost.", true, "UNObot 1.0")]
        public async Task FullHelp()
        {
            await ReplyAsync("Help has been sent. Or, I think it has.");
            string Response = "```Commands: @UNOBot#4308 command/ .command\n (Required) {May be required} [Optional]\n \n";
            string OldResponse = "";
            foreach (Command cmd in Program.commands)
            {
                OldResponse = Response;
                if (cmd.Active)
                {
                    OldResponse = Response;
                    Response += $"- {cmd.CommandName}: {cmd.Help}\n";
                    if (cmd.Usages.Count > 0)
                        Response += $"Usage(s): {string.Join(", ", cmd.Usages.ToArray())}\n";
                    if (cmd.Aliases.Count > 0)
                        Response += $"Aliases: {string.Join(", ", cmd.Aliases.ToArray())}\n";
                    if (Response.Length > 1996)
                    {
                        OldResponse += "```";
                        await UserExtensions.SendMessageAsync(Context.Message.Author, OldResponse);
                        Response = "```";
                        Response += $"- {cmd.CommandName}: {cmd.Help}\n";
                        if (cmd.Usages.Count > 0)
                            Response += $"Usage(s): {string.Join(", ", cmd.Usages.ToArray())}\n";
                        if (cmd.Aliases.Count > 0)
                            Response += $"Aliases: {string.Join(", ", cmd.Aliases.ToArray())}\n";
                    }
                    Response += "\n";
                }
                if (Response.Length > 1996)
                {
                    Response = OldResponse;
                    Response += "```";
                    await UserExtensions.SendMessageAsync(Context.Message.Author, Response);
                    Response = "```\n";
                }
            }
            Response += "```";
            await UserExtensions.SendMessageAsync(Context.Message.Author, Response);
        }
        [Command("help", RunMode = RunMode.Async), Alias("ahh", "ahhh", "ahhhh")]
        public async Task Help()
        {
            var builder = new EmbedBuilder()
                .WithTitle("Quick-start guide to UNObot")
                .WithColor(new Color(UNOcore.r.Next(0, 256), UNOcore.r.Next(0, 256), UNOcore.r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Playing in {Context.Guild.Name}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Usages", "@UNOBot#4308 *commandtorun*\n.*commandtorun*")
                .AddField(".join", "Join a game in the current server.", true)
                .AddField(".start", "Start a game in the current server.\nYou must have joined beforehand.", true)
                .AddField(".play (color) (value) [new color]", "Play a card, assuming if it's your turn.\nIf you are playing a Wild card, also\nadd the color to change to.", true)
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

        [Command("help", RunMode = RunMode.Async), Alias("ahh", "ahhh", "ahhhh")]
        [Help(new string[] { ".help (command)" }, "If you need help using help, you're truly lost.", true, "UNObot 1.0")]
        public async Task Help(string cmdSearch)
        {
            string Response = "";
            int index = Program.commands.FindIndex(o => o.CommandName == cmdSearch);
            int index2 = Program.commands.FindIndex(o => o.Aliases.Contains(cmdSearch) == true);
            Command cmd;
            if (index >= 0)
                cmd = Program.commands[index];
            else if (index2 >= 0)
                cmd = Program.commands[index2];
            else
            {
                await ReplyAsync("Command was not found in the help list.");
                return;
            }
            Response += "```";
            if (!cmd.Active)
                Response += "Note: This command might be hidden or deprecated.\n";
            Response += $"- {cmd.CommandName}: {cmd.Help}\n";
            if (cmd.Usages.Count > 0)
                Response += $"Usage(s): {string.Join(", ", cmd.Usages.ToArray())}\n";
            Response += $"Introduced in {cmd.Version}.\n";
            if (cmd.Aliases.Count > 0)
                Response += $"Aliases: {string.Join(", ", cmd.Aliases.ToArray())}\n";
            Response += "```";
            await ReplyAsync(Response);
        }

        [Command("credits", RunMode = RunMode.Async), Alias("asdf")]
        [Help(new string[] { ".credits" }, "Wow, look at all of the victims during the making of this bot.", true, "UNObot 1.0")]
        public async Task Credits()
        {
            await ReplyAsync("UNObot: Programmed by DoggySazHi\n" +
                "Tested by Aragami and Fm (ish)\n" +
                "UNO card images from Wikipedia\n" +
                "Created for the UBOWS server\n\n" +
                "Stickerz was here.\n" +
                "Blame LocalDisk and Harvest for any bugs.");
        }

        [Command("invite", RunMode = RunMode.Async)]
        [Help(new string[] { ".invite" }, "You actually want the bot? Wow.", true, "UNObot 3.1.4")]
        public async Task Invite()
        {
            await ReplyAsync("If you want to add this bot to your server, use this link: \n" +
                "https://discordapp.com/api/oauth2/authorize?client_id=477616287997231105&permissions=8192&scope=bot");
        }
    }
}
