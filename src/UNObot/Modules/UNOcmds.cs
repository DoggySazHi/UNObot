using System;
using System.Collections.Generic;
using System.Linq;
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
        [Command("draw"), Alias("take", "dr", "tk")]
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

        [Command("deck"), Alias("hand", "cards", "d", "h")]
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
                        var sorted = list.OrderBy((arg) => arg.Color).ThenBy((arg) => arg.Value).ToList<Card>();
                        await db.SetCards(Context.User.Id, sorted);
                        string response = $"Current card: {await db.GetCurrentCard(Context.Guild.Id)}\nCards available:\n";
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

        [Command("skip"), Alias("s")]
        public async Task Skip()
        {
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                if (await db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await db.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await queueHandler.GetCurrentPlayer(Context.Guild.Id))
                        {
                            if (await db.GetGamemode(Context.Guild.Id) == 3)
                            {
                                List<Card> playerCards = await db.GetCards(Context.User.Id);
                                Card currentCard = await db.GetCurrentCard(Context.Guild.Id);
                                bool found = false;
                                foreach (Card c in playerCards)
                                {
                                    if (c.Color == currentCard.Color || c.Value == currentCard.Value)
                                    {
                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                {
                                    await ReplyAsync("You cannot skip because you have a card that matches the criteria!");
                                    return;
                                }
                                await queueHandler.NextPlayer(Context.Guild.Id);
                                await db.AddCard(Context.User.Id, UNOcore.RandomCard());
                                await db.AddCard(Context.User.Id, UNOcore.RandomCard());
                                await ReplyAsync($"You have drawn two cards. It is now <@{await queueHandler.GetCurrentPlayer(Context.Guild.Id)}>'s turn.");
                                AFKtimer.ResetTimer(Context.Guild.Id);
                            }
                            else
                                await ReplyAsync("The current game doesn't allow skipping!");
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
        [Command("card"), Alias("top", "c")]
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
        [Command("quickplay"), Alias("quickdraw", "autoplay", "autodraw", "qp", "qd", "ap", "ad")]
        public async Task QuickPlay()
        {
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                if (await db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await db.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await queueHandler.GetCurrentPlayer(Context.Guild.Id))
                        {
                            //TODO - kete
                            //TODO - add the normal playcard check (ingame)
                            List<Card> playerCards = await db.GetCards(Context.User.Id);
                            Card currentCard = await db.GetCurrentCard(Context.Guild.Id);
                            bool found = false;
                            foreach (Card c in playerCards)
                            {
                                if (c.Color == currentCard.Color || c.Value == currentCard.Value)
                                {
                                    found = true;
                                    await UserExtensions.SendMessageAsync(Context.Message.Author, "Played the first card that matched the criteria!");
                                    await ReplyAsync(await playCard.Play(c.Color, c.Value, null, Context.User.Id, Context.Guild.Id));
                                    break;
                                }
                            }
                            if (!found)
                            {
                                int cardsDrawn = 0;
                                List<Card> cardsTaken = new List<Card>();
                                String response = "Cards drawn:\n";
                                while (true)
                                {
                                    Card rngcard = UNOcore.RandomCard();
                                    await db.AddCard(Context.User.Id, rngcard);
                                    cardsTaken.Add(rngcard);
                                    cardsDrawn++;
                                    if (rngcard.Color == currentCard.Color || rngcard.Value == currentCard.Value)
                                    {
                                        foreach (Card cardTake in cardsTaken)
                                        {
                                            response += cardTake + "\n";
                                        }
                                        await ReplyAsync($"You have drawn {cardsDrawn} cards.");
                                        await UserExtensions.SendMessageAsync(Context.Message.Author, response);
                                        await ReplyAsync(await playCard.Play(rngcard.Color, rngcard.Value, null, Context.User.Id, Context.Guild.Id));
                                        break;
                                    }
                                }
                            }
                            AFKtimer.ResetTimer(Context.Guild.Id);
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
        [Command("players"), Alias("users", "pl")]
        public async Task Players()
        {
            await db.AddGame(Context.Guild.Id);
            await db.AddUser(Context.User.Id, Context.User.Username);
            if (await db.IsServerInGame(Context.Guild.Id))
            {
                ushort gameMode = await db.GetGamemode(Context.Guild.Id);
                ulong currentPlayer = await queueHandler.GetCurrentPlayer(Context.Guild.Id);
                string response = $"Current player: <@{currentPlayer}>\n";
                Card currentCard = await db.GetCurrentCard(Context.Guild.Id);
                response += $"Current card: {currentCard}\n";
                foreach (ulong player in await db.GetPlayers(Context.Guild.Id))
                {
                    List<Card> loserlist = await db.GetCards(player);
                    if (gameMode == 2)
                    {
                        int random = 0;
                        while (random == 0)
                            random = Math.Abs(loserlist.Count + UNOcore.r.Next(-10, 10));
                        response += $"- <@{player}> has {random} (?) cards left.\n";
                    }
                    else
                    {
                        response += $"- <@{player}> has {loserlist.Count} cards left.\n";
                    }
                }
                await ReplyAsync(response);
            }
            else
                await ReplyAsync("The game has not started!");
        }
        [Command("queue"), Alias("q")]
        public async Task Queue()
        {
            await db.AddGame(Context.Guild.Id);
            await db.AddUser(Context.User.Id, Context.User.Username);
            Queue<ulong> currqueue = await db.GetUsersWithServer(Context.Guild.Id);
            if (await db.IsServerInGame(Context.Guild.Id))
            {
                await ReplyAsync("Since the server is already in a game, you can also use .players!");
                await Players();
                return;
            }
            if (currqueue.Count <= 0)
            {
                await ReplyAsync("There is nobody in the queue. Join with .join!");
                return;
            }
            string Response = "Current queue players:\n";
            foreach (ulong player in currqueue)
                Response += $"- <@{player}>\n";
            await ReplyAsync(Response);
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
                    await db.AddGuild(Context.Guild.Id, 1, 1);
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
                    while (true)
                    {
                        if (currentCard.Color == "Wild")
                            currentCard = UNOcore.RandomCard();
                        else
                            break;
                    }
                    switch (currentCard.Value)
                    {
                        case "+2":
                            var curuser = await queueHandler.GetCurrentPlayer(Context.Guild.Id);
                            await db.AddCard(curuser, UNOcore.RandomCard());
                            await db.AddCard(curuser, UNOcore.RandomCard());
                            break;
                        case "Reverse":
                            await queueHandler.ReversePlayers(Context.Guild.Id);
                            await ReplyAsync($"What? The order has been reversed! Now, it's <@{await queueHandler.GetCurrentPlayer(Context.Guild.Id)}>'s turn.");
                            break;
                        case "Skip":
                            await queueHandler.NextPlayer(Context.Guild.Id);
                            await ReplyAsync($"What's this? A skip? Oh well, now it's <@{await queueHandler.GetCurrentPlayer(Context.Guild.Id)}>'s turn.");
                            break;
                    }
                    await db.SetCurrentCard(Context.Guild.Id, currentCard);
                    await ReplyAsync($"Current card: {currentCard.ToString()}\n");
                    await db.StarterCard(Context.Guild.Id);
                    AFKtimer.StartTimer(Context.Guild.Id);
                }
            }
            else
                await ReplyAsync("You have not joined a game!");
        }
        [Command("start")]

        public async Task Start(string mode)
        {
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                await db.AddGame(Context.Guild.Id);
                await db.AddUser(Context.User.Id, Context.User.Username);
                if (await db.IsServerInGame(Context.Guild.Id))
                    await ReplyAsync("The game has already started!");
                else
                {
                    switch (mode)
                    {
                        //TODO - Modify addguild cmd 
                        case "private":
                            await ReplyAsync("Playing in privacy!");
                            await db.AddGuild(Context.Guild.Id, 1, 2);
                            break;
                        case "fast":
                            //TODO - Add skip command. Check if fast mode is on.
                            //Skip only when you can't play.
                            await ReplyAsync("Playing in fast mode!");
                            await db.AddGuild(Context.Guild.Id, 1, 3);
                            break;
                        case "normal":
                            await ReplyAsync("Playing in normal mode!");
                            await db.AddGuild(Context.Guild.Id, 1, 1);
                            break;
                        default:
                            await ReplyAsync("That's not a valid mode!");
                            return;
                    }
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
        [Command("play"), Priority(2), Alias("put", "place", "p")]
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
        [Command("play"), Priority(1), Alias("put", "place", "p")]
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
        [Command("enforcechannels"), RequireUserPermission(GuildPermission.ManageChannels), Alias("forcechannels")]
        public async Task EnforceChannel()
        {
            //start check
            var allowedChannels = await db.GetAllowedChannels(Context.Guild.Id);
            var currentChannels = Context.Guild.TextChannels.ToList();
            var currentChannelsIDs = new List<ulong>();
            foreach (var channel in currentChannels)
                currentChannelsIDs.Add(channel.Id);
            if (allowedChannels.Except(currentChannelsIDs).Any())
            {
                foreach (var toRemove in allowedChannels.Except(currentChannelsIDs))
                    allowedChannels.Remove(toRemove);
                await db.SetAllowedChannels(Context.Guild.Id, allowedChannels);
            }
            //end check
            if (allowedChannels.Count == 0)
            {
                await Context.Channel.SendMessageAsync("Error: Cannot enable enforcechannels if there are no allowed channels!");
                return;
            }
            bool enforce = await db.EnforceChannel(Context.Guild.Id);
            await db.SetEnforceChannel(Context.Guild.Id, !enforce);
            if (!enforce)
                await ReplyAsync($":white_check_mark: Currently enforcing UNObot to only respond to messages in the filter.");
            else
                await ReplyAsync($":white_check_mark: Currently allowing UNObot to respond to messages from anywhere.");
        }
        [Command("addallowedchannel"), RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task AddAllowedChannel()
        {
            if (await db.GetDefaultChannel(Context.Guild.Id) == Context.Channel.Id && await db.HasDefaultChannel(Context.Guild.Id))
                await ReplyAsync("The default UNO channel has been set to this already; there is no need to add this as a default channel.");
            else if ((await db.GetAllowedChannels(Context.Guild.Id)).Contains(Context.Channel.Id))
                await ReplyAsync("This channel is already allowed! To see all channels, use .listallowedchannels.");
            else
            {
                var currentChannels = await db.GetAllowedChannels(Context.Guild.Id);
                currentChannels.Add(Context.Channel.Id);
                await db.SetAllowedChannels(Context.Guild.Id, currentChannels);
                await ReplyAsync($"Added #{Context.Channel.Name} to the list of allowed channels. Make sure you .enforcechannels for this to work.");
            }
        }
        [Command("listallowedchannels"), RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task ListAllowedChannels()
        {
            var allowedChannels = await db.GetAllowedChannels(Context.Guild.Id);
            //start check
            var currentChannels = Context.Guild.TextChannels.ToList();
            var currentChannelsIDs = new List<ulong>();
            foreach (var channel in currentChannels)
                currentChannelsIDs.Add(channel.Id);
            if (allowedChannels.Except(currentChannelsIDs).Any())
            {
                foreach (var toRemove in allowedChannels.Except(currentChannelsIDs))
                    allowedChannels.Remove(toRemove);
                await db.SetAllowedChannels(Context.Guild.Id, allowedChannels);
            }
            //end check
            bool enforced = await db.EnforceChannel(Context.Guild.Id);
            string yesno = enforced ? "Currently enforcing channels." : "Not enforcing channels.";
            string response = $"{yesno}\nCurrent channels allowed: \n";
            if (allowedChannels.Count == 0)
            {
                await ReplyAsync("There are no channels that are currently allowed. Add them with .addallowedchannel, and .enforcechannels.");
                return;
            }
            foreach (ulong id in allowedChannels)
                response += $"- <#{id}>\n";
            await ReplyAsync(response);
        }
        [Command("removeallowedchannel"), RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RemoveAllowedChannel()
        {
            //start check
            var allowedChannels = await db.GetAllowedChannels(Context.Guild.Id);
            var currentChannels = Context.Guild.TextChannels.ToList();
            var currentChannelsIDs = new List<ulong>();
            foreach (var channel in currentChannels)
                currentChannelsIDs.Add(channel.Id);
            if (allowedChannels.Except(currentChannelsIDs).Any())
            {
                foreach (var toRemove in allowedChannels.Except(currentChannelsIDs))
                    allowedChannels.Remove(toRemove);
                await db.SetAllowedChannels(Context.Guild.Id, allowedChannels);
            }
            //end check
            if (allowedChannels.Contains(Context.Channel.Id))
            {
                allowedChannels.Remove(Context.Channel.Id);
                await db.SetAllowedChannels(Context.Guild.Id, allowedChannels);
                await ReplyAsync($"Removed <#{Context.Channel.Id}> from the allowed channels!");
            }
            else
                await ReplyAsync("This channel was never an allowed channel.");
        }
        [Command("emergency"), RequireUserPermission(GuildPermission.ManageMessages), Alias("em", "leaveserver")]
        public async Task Emergency()
        {
            //todo add link
            await ReplyAsync("If a rogue bot has taken over this account, it will be disabled with the use of this command.\n" +
                             $"Currently on **{Context.Guild.Name}**, goodbye world!\n" +
                             "To reinvite the bot, please use this link: https://discordapp.com/api/oauth2/authorize?client_id=477616287997231105&permissions=8192&scope=bot");
            await Context.Guild.LeaveAsync();
        }
    }
}