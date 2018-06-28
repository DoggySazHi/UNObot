using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace UNObot.Modules
{
    public class UNOcmds : ModuleBase<SocketCommandContext>
    {
        readonly UNOdb db = new UNOdb();
        readonly QueueHandler queueHandler = new QueueHandler();
        readonly AFKtimer AFKtimer = new AFKtimer();
        readonly PlayCard playCard = new PlayCard();

        [Command("seed")]
        public async Task Seed(string seed)
        {
            UNOcore.r = new Random(seed.GetHashCode());
            await ReplyAsync("Seed has been updated. I do not guarantee 100% Wild cards.");
        }
        [Command("join")]
        public async Task Join()
        {
            await db.AddGame(Context.Guild.Id);
            await db.AddUser(Context.User.Id, Context.User.Username);
            if (await db.IsServerInGame(Context.Guild.Id))
            {
                await ReplyAsync($"The game has already started in this server!\n");
                return;
            }
            else if (await db.IsPlayerInGame(Context.User.Id))
            {
                await ReplyAsync($"{Context.User.Username}, you are already in a game!\n");
                return;
            }
            else
                await db.AddUser(Context.User.Id, Context.User.Username, Context.Guild.Id);
            await ReplyAsync($"{Context.User.Username} has been added to the queue.\n");
        }
        [Command("leave")]
        public async Task Leave()
        {
            await db.AddGame(Context.Guild.Id);
            await db.AddUser(Context.User.Id, Context.User.Username);
            if (await db.IsPlayerInGame(Context.User.Id) && Context.Guild.Id == await db.GetUserServer(Context.User.Id))
            {
                await db.RemoveUser(Context.User.Id);
                await queueHandler.RemovePlayer(Context.User.Id, Context.Guild.Id);
            }
            else
            {
                await ReplyAsync($"{Context.User.Username}, you are already out of game! Note that you must run this command in the server you are playing in.\n");
                return;
            }
            if (await db.IsServerInGame(Context.Guild.Id) && (await queueHandler.PlayerCount(Context.Guild.Id) == 0))
            {
                await db.ResetGame(Context.Guild.Id);
                await ReplyAsync($"Due to {Context.User.Username}'s departure, the game has been reset.");
                return;
            }
            await ReplyAsync($"{Context.User.Username} has been removed from the queue.\n");
            if (await queueHandler.PlayerCount(Context.Guild.Id) > 1 && await db.IsServerInGame(Context.Guild.Id))
            {
                await ReplyAsync($"It is now <@{await queueHandler.GetCurrentPlayer(Context.Guild.Id)}>'s turn.");
            }
        }
        [Command("stats")]
        public async Task Stats()
        {
            int[] stats = await db.GetStats(Context.User.Id);
            string note = await db.GetNote(Context.User.Id);
            if (!await db.UserExists(Context.User.Id))
            {
                await ReplyAsync("You do not currently exist in the database.");
            }
            if (note != null)
            {
                await ReplyAsync($"NOTE: {note}");
            }
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
            string note = await db.GetNote(userid);
            if (note != null)
            {
                await ReplyAsync($"NOTE: {note}");
            }
            await ReplyAsync($"<@{userid}>'s stats:\n"
                                + $"Games joined: {stats[0]}\n"
                                + $"Games fully played: {stats[1]}\n"
                                + $"Games won: {stats[2]}");
        }
        [Command("draw"), Alias("take")]
        public async Task Draw()
        {
            await db.AddGame(Context.Guild.Id);
            await db.AddUser(Context.User.Id, Context.User.Username);
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                if (await db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await db.IsServerInGame(Context.Guild.Id))
                    {
                        if (await queueHandler.GetCurrentPlayer(Context.Guild.Id) == Context.User.Id)
                        {
                            Card card = UNOcore.RandomCard();
                            await UserExtensions.SendMessageAsync(Context.Message.Author, "You have recieved: " + card.Color + " " + card.Value + ".");
                            await db.AddCard(Context.User.Id, card);
                            AFKtimer.ResetTimer(Context.Guild.Id);
                            return;
                        }
                        else
                            await ReplyAsync("Why draw now? Draw when it's your turn!");
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

        [Command("deck"), Alias("hand", "cards")]
        public async Task Deck()
        {
            await db.AddGame(Context.Guild.Id);
            await db.AddUser(Context.User.Id, Context.User.Username);
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                if (await db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await db.IsServerInGame(Context.Guild.Id))
                    {
                        List<Card> list = await db.GetCards(Context.User.Id);
                        string response = "Cards available:\n";
                        foreach (Card card in list)
                        {
                            response += card.Color + " " + card.Value + "\n";
                        }
                        await UserExtensions.SendMessageAsync(Context.Message.Author, response);
                    }
                    else
                        await ReplyAsync("The game has not started!");
                }
                else
                    await ReplyAsync("The game has not started, or you are not in the right server!");
            }
            else
                await ReplyAsync("You are not in any game!");
        }
        /*
        if(await db.IsPlayerInGame(Context.User.Id))
            {
                if(await db.IsPlayerInServerGame(Context.User.Id,Context.Guild.Id))
                {
                    if(await db.IsServerInGame(Context.Guild.Id))
                    {
                        //Do something
                    }
                    else
                        await ReplyAsync("The game has not started!");
                }
                else
                    await ReplyAsync("You are in a game, however you are not in the right server!");
            }
            else
                await ReplyAsync("You are not in any game!");
        */
        [Command("card"), Alias("top")]
        public async Task Card()
        {
            await db.AddGame(Context.Guild.Id);
            await db.AddUser(Context.User.Id, Context.User.Username);
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                if (await db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await db.IsServerInGame(Context.Guild.Id))
                    {
                        Card currentCard = await db.GetCurrentCard(Context.Guild.Id);
                        await ReplyAsync("Current card: " + currentCard);
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
        [Command("players"), Alias("users")]
        public async Task Players()
        {
            await db.AddGame(Context.Guild.Id);
            await db.AddUser(Context.User.Id, Context.User.Username);
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                if (await db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await db.IsServerInGame(Context.Guild.Id))
                    {
                        ulong currentPlayer = await queueHandler.GetCurrentPlayer(Context.Guild.Id);
                        string response = $"Current player: <@{currentPlayer}>\n";
                        Card currentCard = await db.GetCurrentCard(Context.Guild.Id);
                        response += $"Current card: {currentCard}";
                        foreach (ulong player in await db.GetPlayers(Context.Guild.Id))
                        {
                            List<Card> loserlist = await db.GetCards(player);
                            response += $"- <@{player}> has {loserlist.Count} cards left.\n";
                        }
                        await ReplyAsync(response);
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
        [Command("uno")]
        public async Task UNOcmd()
        {
            await db.AddGame(Context.Guild.Id);
            await db.AddUser(Context.User.Id, Context.User.Username);
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                if (await db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await db.IsServerInGame(Context.Guild.Id))
                    {
                        if (await db.GetUNOPlayer(Context.Guild.Id) == Context.User.Id)
                        {
                            await ReplyAsync("Great, you have one card left! Everyone still has a chance however, so keep going!");
                            await db.SetUNOPlayer(Context.Guild.Id, 0);
                        }
                        else
                        {
                            await ReplyAsync("Uh oh, you still have more than one card! Two cards have been added to your hand.");
                            await db.AddCard(Context.User.Id, UNOcore.RandomCard());
                            await db.AddCard(Context.User.Id, UNOcore.RandomCard());
                        }
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
        [Command("start")]

        public async Task Start()
        {
            
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                await db.AddGame(Context.Guild.Id);
                await db.AddUser(Context.User.Id, Context.User.Username);
                if (await db.IsServerInGame(Context.Guild.Id))
                    await ReplyAsync("The game has already started!");
                else
                {
                    await db.AddGuild(Context.Guild.Id, 1);
                    await db.GetUsersAndAdd(Context.Guild.Id);
                    foreach (ulong player in await db.GetPlayers(Context.Guild.Id))
                    {
                        await db.UpdateStats(player, 1);
                    }
                    await ReplyAsync("Game has started. All information about your cards will be PMed.\n" +
                            "You have been given 7 cards; PM \"deck\" to view them.\n" +
                            "Remember; you have 1 minute and 30 seconds to place a card.\n" +
                            $"The first player is <@{await queueHandler.GetCurrentPlayer(Context.Guild.Id)}>.\n");
                    Card currentCard = UNOcore.RandomCard();
                    await db.SetCurrentCard(Context.Guild.Id, currentCard);
                    await ReplyAsync($"Current card: {currentCard.ToString()}\n");
                    await db.StarterCard(Context.Guild.Id);
                    AFKtimer.StartTimer(Context.Guild.Id);
                }
            }
            else
                await ReplyAsync("You have not joined a game!");
        }
        [Command("play"), Priority(2), Alias("put", "place")]
        public async Task Play(string color, string value)
        {
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                if (await db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await db.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await queueHandler.GetCurrentPlayer(Context.Guild.Id))
                        {
                            if (color.ToLower() == "wild")
                            {
                                await ReplyAsync("You need to rerun the command, but also add what color should it represent.\nEx. play Wild Color Green");
                            }
                            else
                            {
                                AFKtimer.ResetTimer(Context.Guild.Id);
                                await ReplyAsync(await playCard.Play(color, value, null, Context.User.Id, Context.Guild.Id));
                            }
                        }
                        else
                            await ReplyAsync("It is not your turn!");
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
        [Command("play"), Priority(1), Alias("put", "place")]
        public async Task PlayWild(string color, string value, string wild)
        {
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                if (await db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await db.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await queueHandler.GetCurrentPlayer(Context.Guild.Id))
                        {
                            AFKtimer.ResetTimer(Context.Guild.Id);
                            await ReplyAsync(await playCard.Play(color, value, wild, Context.User.Id, Context.Guild.Id));
                        }
                        else
                            await ReplyAsync("It is not your turn!");
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
        [Command("setdefaultchannel"), RequireUserPermission(GuildPermission.ManageChannels), Alias("adddefaultchannel")]
        public async Task SetDefaultChannel()
        {
            await ReplyAsync($":white_check_mark: Set default UNO channel to #{Context.Channel.Name}.");
            await db.SetDefaultChannel(Context.Guild.Id, Context.Channel.Id);
            await db.SetHasDefaultChannel(Context.Guild.Id, true);
        }
        [Command("removedefaultchannel"), RequireUserPermission(GuildPermission.ManageChannels), Alias("deletedefaultchannel")]
        public async Task RemoveDefaultChannel()
        {
            await ReplyAsync($":white_check_mark: Removed default UNO channel, assuming there was one.");
            await db.SetDefaultChannel(Context.Guild.Id, Context.Guild.DefaultChannel.Id);
            await db.SetHasDefaultChannel(Context.Guild.Id, false);
        }
    }
}