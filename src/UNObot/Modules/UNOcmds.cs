using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace DiscordBot.Modules
{
    public class UNOcmds : ModuleBase<SocketCommandContext>
    {
        [Command("seed")]
        public async Task Seed(string seed)
        {
            UNOcore.r = new Random(seed.GetHashCode());
            await ReplyAsync("Seed has been updated. I do not guarantee 100% Wild cards.");
        }
        [Command("join")]
        public async Task Join()
        {
            if (await db.IsServerInGame(Context.Guild.Id))
            {
                await ReplyAsync($"The game has already started!\n");
                return;
            }
            else if (await db.IsPlayerInGame(Context.User.Id))
            {
                await ReplyAsync($"{Context.User.Username}, you are already in game!\n");
                return;
            }
            else
                await db.AddUser(Context.User.Id, Context.User.Username, Context.Guild.Id);
            await ReplyAsync($"{Context.User.Username} has been added to the queue.\n");
        }
    }
}