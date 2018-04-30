using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace UNObot.Modules
{
    public class UNOcmds : ModuleBase<SocketCommandContext>
    {
        UNOdb db = new UNOdb();
        QueueHandler queueHandler = new QueueHandler();
        AFKtimer AFKtimer = new AFKtimer();

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
            Queue<ulong> players = await db.GetPlayers(Context.Guild.Id);
            await queueHandler.NextPlayer(Context.Guild.Id);
            if (players.Count == 0)
            {
                await db.ResetGame(Context.Guild.Id);
                await ReplyAsync("Game has been reset, due to nobody in-game.");
                AFKtimer.playTimers[Context.Guild.Id].Dispose();
            }
            await ReplyAsync($"{Context.User.Username} has been removed from the queue.\n");
        }
        [Command("stats")]
        public async Task Stats()
        {
            int[] stats = await db.GetStats(Context.User.Id);
            await ReplyAsync($"{Context.User.Username}'s stats:\n"
                                + $"Games joined: {stats[0]}\n"
                                + $"Games fully played: {stats[1]}\n"
                                + $"Games won: {stats[2]}");
        }
        [Command("stats")]
        public async Task Stats2(string user)
        {
            user = user.Trim(new Char[] { ' ', '<', '>', '!', '@' });
            if (!UInt64.TryParse(user, out ulong userid))
            {
                await ReplyAsync("Mention the player with this command to see their stats.");
                return;
            }
            if (!await db.UserExists(userid))
            {
                await ReplyAsync($"The user does not exist; either you have typed it wrong, or that user doesn't exist in the UNObot database.");
                return;
            }
            int[] stats = await db.GetStats(userid);
            await ReplyAsync($"<@{userid}>'s stats:\n"
                                + $"Games joined: {stats[0]}\n"
                                + $"Games fully played: {stats[1]}\n"
                                + $"Games won: {stats[2]}");
        }
        [Command("draw")]
        public async Task Draw()
        {
            if(await db.IsPlayerInGame(Context.User.Id))
            {
                if(await db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if(await db.IsServerInGame(Context.Guild.Id))
                    {
                        Card card = UNOcore.RandomCard();
                        await UserExtensions.SendMessageAsync(Context.Message.Author, "You have recieved: " + card.Color + " " + card.Value + ".");
                        await db.AddCard(Context.User.Id, card);
                        AFKtimer.ResetTimer(Context.Guild.Id);
                        return;
                    }
                    else
                        await ReplyAsync("The game has not started!");
                }
                else 
                    await ReplyAsync("You are in a game, however you are not in the right server!");
            }
            else
                await ReplyAsync("You are not in any game!");
        }
    }
}