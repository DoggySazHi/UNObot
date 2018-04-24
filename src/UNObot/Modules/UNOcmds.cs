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
    }
}