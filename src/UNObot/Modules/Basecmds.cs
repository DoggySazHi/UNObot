using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace UNObot.Modules
{
    public class Basecmds : ModuleBase<SocketCommandContext>
    {
        [Command("info")]
        [Help(new string[] { ".info" }, "Get the current version of UNObot.", true, "UNObot 1.0")]
        public async Task Info()
        {
            await ReplyAsync(
                $"{Context.Client.CurrentUser.Username} - Created by DoggySazHi\nVersion {Program.version}\nCurrent Time (PST): {DateTime.Now.ToString()}");
        }

        [Command("gulag")]
        [Help(new string[] { ".gulag" }, "Blyat.", false, "UNObot 1.4")]
        public async Task Gulag()
        {
            await ReplyAsync($"<@{Context.User.Id}> has been sent to gulag and has all of his cards converted to red blyats.");
        }

        [Command("gulag")]
        [Help(new string[] { ".gulag (user)" }, "Blyat.", false, "UNObot 1.4")]
        public async Task Gulag2(string user)
        {
            //extraclean
            user = user.Trim(new Char[] { ' ', '<', '>', '!', '@' });
            await ReplyAsync($"<@{user}> has been sent to gulag and has all of his cards converted to red blyats.");
        }

        [Command("nepnep")]
        [Help(new string[] { ".nepnep" }, "Wait, how did this command get in here?", false, "UNObot 1.4")]
        public async Task Nep()
        {
            await ReplyAsync($"You got me there at \"nep\".");
        }

        [Command("ugay"), Alias("u gay", "you gay", "you're gay")]
        [Help(new string[] { ".ugay" }, "That's not very nice. >:[", false, "UNObot 0.1")]
        public async Task Ugay()
            => await ReplyAsync(
                $"<@{Context.User.Id}> no u\n");

        [Command("no u"), Alias("nou")]
        [Help(new string[] { ".no u" }, "Fite me m8", false, "UNObot 1.0")]
        public async Task Easteregg1()
        {
            await ReplyAsync($"I claim that <@{Context.User.Id}> is triple gay. Say \"No U\" again, uh...");
        }

        [Command("testperms"), RequireUserPermission(GuildPermission.ManageGuild)]
        [Help(new string[] { ".testperms" }, "Show all permissions that UNObot has. Added for security reasons.", true, "UNObot 1.4")]
        public async Task TestPerms()
        {
            string response = "Permissions:\n";
            var User = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var Perms = User.GetPermissions(Context.Channel as IGuildChannel);
            foreach (ChannelPermission c in Perms.ToList())
            {
                response += c.ToString() + "\n";
            }
            await ReplyAsync(response);
        }
        [Command("dogtestperms"), RequireOwner]
        public async Task TestPerms2()
        {
            string response = "Permissions:\n";
            var User = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var Perms = User.GetPermissions(Context.Channel as IGuildChannel);
            foreach (ChannelPermission c in Perms.ToList())
            {
                response += $"- c.ToString() | \n";
            }
            await ReplyAsync(response);
        }
        [Command("nick"), RequireOwner]
        [Help(new string[] { ".nick (nickname)" }, "Change the nickname of UNObot.", false, "UNObot 2.0")]
        public async Task ChangeNick(string newnick)
        {
            var User = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            await User.ModifyAsync(x =>
            {
                x.Nickname = newnick;
            });
        }

        [Command("upupdowndownleftrightleftrightbastart")]
        [Help(new string[] { ".upupdowndownleftrightleftrightbastart" }, "Wow, an ancient easter egg. It's still ancient.", false, "UNObot 1.4")]
        public async Task OldEasterEgg()
            => await ReplyAsync("lol, that's outdated");

        [Command("help"), Alias("ahh", "ahhh", "ahhhh")]
        [Help(new string[] { ".help" }, "If you need help using help, you're truly lost.", true, "UNObot 1.0")]
        public async Task Help()
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

        [Command("help"), Alias("ahh", "ahhh", "ahhhh")]
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

        [Command("credits"), Alias("asdf")]
        [Help(new string[] { ".credits" }, "Wow, look at all of the victims during the making of this bot.", true, "UNObot 1.0")]
        public async Task Credits()
        {
            await ReplyAsync("UNObot: Programmed by DoggySazHi\n" +
                "Tested by Aragami and Fm (ish)\n" +
                "UNO card images created by Dmitry Fomin (Wikipedia)" +
                "Created for the UBOWS server\n\n" +
                "Stickerz was here.\n" +
                "Blame LocalDisk and Harvest for any bugs.");
        }
    }
}
