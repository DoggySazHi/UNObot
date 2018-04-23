using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordBot.Modules
{
    public class Basecmds : ModuleBase<SocketCommandContext>
    {
        [Command("info")]
        public Task Info()
        {
            return ReplyAsync(
                $"{Context.Client.CurrentUser.Username} - Created by DoggySazHi\nVersion {Program.version}\nblame Aragami and FM for the existance of this");
        }
        [Command("exit"),RequireUserPermission(GuildPermission.Administrator)]
        public Task Exit()
        {
            ReplyAsync("Sorry to be a hassle. Goodbye world!");
            Environment.Exit(0);
            return null;
        }
        [Command("gulag")]
        public Task Gulag()
        {
            return ReplyAsync($"<{@Context.User.Id}> has been sent to gulag and has all of his cards converted to red blyats.");
        }
        [Command("ugay")]
        public Task Ugay()
            => ReplyAsync(
                $"<@{Context.User.Id}> no u\n");

        [Command("u gay")]
        public Task Ugay2()
            => ReplyAsync(
                $"<@{Context.User.Id}> no u\n");

        [Command("you gay")]
        public Task Ugay3()
            => ReplyAsync(
                $"<@{Context.User.Id}> no u\n");
    }
}