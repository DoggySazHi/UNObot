using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace UNObot.Modules
{
    public class Basecmds : ModuleBase<SocketCommandContext>
    {
        [Command("info")]
        public async Task Info()
        {
            await ReplyAsync(
                $"{Context.Client.CurrentUser.Username} - Created by DoggySazHi\nVersion {Program.version}\nblame Aragami and FM for the existance of this");
        }
        [Command("gulag")]
        public async Task Gulag()
        {
            await ReplyAsync($"<@{Context.User.Id}> has been sent to gulag and has all of his cards converted to red blyats.");
        }
        [Command("gulag")]
        public async Task Gulag2(string user)
        {
            //extraclean
            user = user.Trim(new Char[] { ' ', '<', '>', '!', '@' });
            await ReplyAsync($"<@{user}> has been sent to gulag and has all of his cards converted to red blyats.");
        }
        [Command("nepnep")]
        public async Task Nep()
        {
            await ReplyAsync($"You got me there at \"nep\".");
        }
        [Command("ugay")]
        public async Task Ugay()
            => await ReplyAsync(
                $"<@{Context.User.Id}> no u\n");

        [Command("u gay")]
        public async Task Ugay2()
            => await ReplyAsync(
                $"<@{Context.User.Id}> no u\n");

        [Command("you gay")]
        public async Task Ugay3()
            => await ReplyAsync(
                $"<@{Context.User.Id}> no u\n");

        [Command("no u")]
        public async Task Easteregg1()
        {
            await ReplyAsync($"I claim that <@{Context.User.Id}> is triple gay. Say \"No U\" again, u ded m8.");
        }
        /*
        [Command("doggysaz"), RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task Easteregg2(string response)
        {
            var messages = await Context.Channel.GetMessagesAsync(1).FlattenAsync();

            if (!(Context.Channel is ITextChannel textchannel))
            {
                Console.WriteLine("error cast");
                return;
            }
            await textchannel.DeleteMessagesAsync(messages);
            await ReplyAsync(response);
        }
        */
        [Command("testperms"), RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task TestPerms()
        {
            string response = "Permissions:\n";
            var User = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var Perms = User.GetPermissions(Context.Channel as IGuildChannel);
            foreach (ChannelPermission c in Perms.ToList())
            {
                //todo: copy, lol
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
                //TODO: Make a warning for admin/harmful perms
                response += $"- c.ToString() | \n";
            }
            await ReplyAsync(response);
        }
        [Command("nick"), RequireOwner]
        public async Task ChangeNick(string newnick)
        {
            var User = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            await User.ModifyAsync(x =>
            {
                x.Nickname = newnick;
            });
        }
        [Command("upupdowndownleftrightleftrightbastart")]
        public async Task OldEasterEgg()
            => await ReplyAsync("lol, that's outdated");
        [Command("help"), Alias("ahh", "ahhh", "ahhhh")]
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
        [Command("help")]
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
        public async Task Credits()
        {
            await ReplyAsync("UNObot: Programmed by DoggySazHi\n" +
                "Tested by Aragami and Fm\n" +
                "Created for the UBOWS server\n\n" +
                "Stickerz was here.\n" +
                "Blame LocalDisk and Harvest for any bugs.");
        }
    }
}
