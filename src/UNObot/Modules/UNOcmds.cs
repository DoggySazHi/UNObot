using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace DiscordBot.Modules
{
    public class UNOcmds : ModuleBase<SocketCommandContext>
    {
        UNObot.Modules.UNOdb db = new UNObot.Modules.UNOdb();
        
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
        [Command("leave")]
        public async Task Leave()
        {
            if(await db.IsPlayerInGame(Context.User.Id))
                await db.RemoveUser(Context.User.Id);
            else
            {
                await ReplyAsync($"{Context.User.Username}, you are already out of game!\n");
                return;
            }
            Queue<ulong> players = await db.GetPlayers();
            await NextPlayer();
            if (players.Count == 0)
            {
                Program.currentPlayer = 0;
                Program.gameStarted = false;
                Program.order = 1;
                Program.currentcard = null;
                await ReplyAsync("Game has been reset, due to nobody in-game.");
                playTimer.Dispose();
            }
            await ReplyAsync($"{Context.User.Username} has been removed from the queue.\n");
        }
    }
}