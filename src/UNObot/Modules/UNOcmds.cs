using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

#pragma warning disable CS1701 // Assuming assembly reference matches identity
#pragma warning disable CS1702 // Assuming assembly reference matches identity

namespace UNObot.Modules
{
    public class UNOcmds : ModuleBase<SocketCommandContext>
    {
        readonly PlayCard playCard = new PlayCard();

        [Command("seed", RunMode = RunMode.Async)]
        [Help(new string[] { ".seed (seed)" }, "Cheat like Aragami and hope the RNG favors you. Deprecated due to a different RNG algorithm.", false, "UNObot 1.0")]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task Seed([Remainder]string seed)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            /*
            UNOcore.r = new Random(seed.GetHashCode());
            await ReplyAsync("Seed has been updated. I do not guarantee 100% Wild cards.");
            */
        }
        [Command("join", RunMode = RunMode.Async), Help(new string[] { ".join" }, "Join the queue in the current server.", true, "UNObot 0.1")]
        public async Task Join()
        {
            await UNOdb.AddGame(Context.Guild.Id);
            await UNOdb.AddUser(Context.User.Id, Context.User.Username);
            if (await UNOdb.IsServerInGame(Context.Guild.Id))
            {
                await ReplyAsync($"The game has already started in this server!\n");
                return;
            }
            if (await UNOdb.IsPlayerInGame(Context.User.Id))
            {
                await ReplyAsync($"{Context.User.Username}, you are already in a game!\n");
                return;
            }
            await UNOdb.AddUser(Context.User.Id, Context.User.Username, Context.Guild.Id);
            await ReplyAsync($"{Context.User.Username} has been added to the queue.\n");
        }
        [Command("leave", RunMode = RunMode.Async)]
        [Help(new string[] { ".leave" }, "Leave the queue (or game) in the current server.", true, "UNObot 0.2")]
        public async Task Leave()
        {
            await UNOdb.AddGame(Context.Guild.Id);
            await UNOdb.AddUser(Context.User.Id, Context.User.Username);
            if (await UNOdb.IsPlayerInGame(Context.User.Id) && Context.Guild.Id == await UNOdb.GetUserServer(Context.User.Id))
            {
                await UNOdb.RemoveUser(Context.User.Id);
                await QueueHandler.RemovePlayer(Context.User.Id, Context.Guild.Id);
            }
            else
            {
                await ReplyAsync($"{Context.User.Username}, you are already out of game! Note that you must run this command in the server you are playing in.\n");
                return;
            }
            if (await UNOdb.IsServerInGame(Context.Guild.Id) && (await QueueHandler.PlayerCount(Context.Guild.Id) == 0))
            {
                await UNOdb.ResetGame(Context.Guild.Id);
                await ReplyAsync($"Due to {Context.User.Username}'s departure, the game has been reset.");
                return;
            }
            await ReplyAsync($"{Context.User.Username} has been removed from the queue.\n");
            if (await QueueHandler.PlayerCount(Context.Guild.Id) > 1 && await UNOdb.IsServerInGame(Context.Guild.Id))
            {
                await ReplyAsync($"It is now <@{await QueueHandler.GetCurrentPlayer(Context.Guild.Id)}>'s turn.");
            }
        }
        [Command("stats", RunMode = RunMode.Async)]
        [Help(new string[] { ".stats" }, "Get the statistics of you or another player to see if they are a noob, pro, or hacker.", true, "UNObot 1.4")]
        public async Task Stats()
        {
            int[] stats = await UNOdb.GetStats(Context.User.Id);
            string note = await UNOdb.GetNote(Context.User.Id);
            if (!await UNOdb.UserExists(Context.User.Id))
            {
                await ReplyAsync("You do not currently exist in the database. Maybe you should play a game.");
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
        [Command("stats", RunMode = RunMode.Async)]
        [Help(new string[] { ".stats (ping another player, or their ID)" }, "Get the statistics of you or another player to see if they are a noob, pro, or hacker.", true, "UNObot 1.4")]
        public async Task Stats2([Remainder] string user)
        {
            user = user.Trim();
            //Style of Username#XXXX or Username XXXX
            if ((user.Contains('#') || user.Contains(' ')) && user.Length >= 6 && int.TryParse(user.Substring(user.Length - 4), out int discriminator))
            {
                var userObj = Program._client.GetUser(user.Substring(0, user.Length - 5), discriminator.ToString());
                //Negative one is only passed in because it cannot convert to ulong; it will fail the TryParse and give a "Mention the player..." error.
                user = userObj != null ? userObj.Id.ToString() : (-1).ToString();
            }
            user = user.Trim(new char[] { ' ', '<', '>', '!', '@' });
            if (!ulong.TryParse(user, out ulong userid))
            {
                await ReplyAsync("Mention the player with this command to see their stats. Or if you want to be polite, try using their ID.");
                return;
            }
            if (!await UNOdb.UserExists(userid))
            {
                await ReplyAsync($"The user does not exist; either you have typed it wrong, or that user doesn't exist in the UNObot database.");
                return;
            }
            int[] stats = await UNOdb.GetStats(userid);
            string note = await UNOdb.GetNote(userid);
            if (note != null)
            {
                await ReplyAsync($"NOTE: {note}");
            }
            await ReplyAsync($"{Program._client.GetUser(userid).Username}'s stats:\n"
                                + $"Games joined: {stats[0]}\n"
                                + $"Games fully played: {stats[1]}\n"
                                + $"Games won: {stats[2]}");
        }
        [Command("setnote", RunMode = RunMode.Async)]
        [Help(new string[] { ".setnote" }, "Set a note about yourself. Write nothing to delete your message", true, "UNObot 2.1")]
        public async Task SetNote()
        {
            await UNOdb.RemoveNote(Context.User.Id);
            await ReplyAsync("Successfully removed note!");
        }
        [Command("setnote", RunMode = RunMode.Async)]
        [Help(new string[] { ".setnote" }, "Set a note about yourself. Write nothing to delete your message", true, "UNObot 2.1")]
        public async Task SetNote([Remainder]string text)
        {
            text = text.Trim().Normalize();
            if (text == "")
                text = "???";
            else if (text.ToLower().Contains("discord") && text.ToLower().Contains("gg"))
            {
                await ReplyAsync("You are not allowed to put invites!");
                return;
            }
            await UNOdb.SetNote(Context.User.Id, text);
            await ReplyAsync("Successfully set note!");
        }

        [Command("welcome", RunMode = RunMode.Async)]
        public async Task Welcome()
        {
            string response = "Permissions:\n";
            var User = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var Perms = User.GetPermissions(Context.Channel as IGuildChannel);
            foreach (ChannelPermission c in Perms.ToList())
            {
                response += $"- {c.ToString()} | \n";
            }
            Console.WriteLine(response);
            await ReplyAsync("UNObot was already succcessfully initialized in this server. But thank you.");
        }

        [Command("setusernote", RunMode = RunMode.Async), RequireOwner]
        [Help(new string[] { ".setusernote" }, "Set a note about others. This command can only be ran by DoggySazHi.", false, "UNObot 2.1")]
        public async Task SetNote(string user, [Remainder]string text)
        {
            user = user.Trim(new Char[] { ' ', '<', '>', '!', '@' });
            if (!UInt64.TryParse(user, out ulong userid))
            {
                await ReplyAsync("Mention the player with this command to see their stats.");
                return;
            }
            if (!await UNOdb.UserExists(userid))
            {
                await ReplyAsync($"The user does not exist; either you have typed it wrong, or that user doesn't exist in the UNObot database.");
                return;
            }
            if (text.Trim().Normalize() == "")
                text = "???";
            await UNOdb.SetNote(userid, text);
            await ReplyAsync("Successfully set note!");
        }
        [Command("removenote", RunMode = RunMode.Async)]
        [Help(new string[] { ".removenote" }, "Remove your current note.", true, "UNObot 2.1")]
        public async Task RemoveNote()
        {
            await UNOdb.RemoveNote(Context.User.Id);
            await ReplyAsync("Successfully removed note!");
        }
        [Command("draw", RunMode = RunMode.Async), Alias("take", "dr", "tk")]
        [Help(new string[] { ".draw" }, "Draw a randomized card, which is based off probabilities instead of the real deck.", true, "UNObot 0.2")]
        public async Task Draw()
        {
            await UNOdb.AddGame(Context.Guild.Id);
            await UNOdb.AddUser(Context.User.Id, Context.User.Username);
            if (await UNOdb.IsPlayerInGame(Context.User.Id))
            {
                if (await UNOdb.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNOdb.IsServerInGame(Context.Guild.Id))
                    {
                        if (await QueueHandler.GetCurrentPlayer(Context.Guild.Id) == Context.User.Id)
                        {
                            Card card = UNOcore.RandomCard();
                            await UserExtensions.SendMessageAsync(Context.Message.Author, "You have recieved: " + card.Color + " " + card.Value + ".");
                            await UNOdb.AddCard(Context.User.Id, card);
                            AFKtimer.ResetTimer(Context.Guild.Id);
                            return;
                        }
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

        [Command("deck", RunMode = RunMode.Async), Alias("hand", "cards", "d", "h")]
        [Help(new string[] { ".deck" }, "View all of the cards you possess.", true, "UNObot 0.2")]
        public async Task Deck()
        {
            await UNOdb.AddGame(Context.Guild.Id);
            await UNOdb.AddUser(Context.User.Id, Context.User.Username);
            if (await UNOdb.IsPlayerInGame(Context.User.Id))
            {
                if (await UNOdb.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNOdb.IsServerInGame(Context.Guild.Id))
                    {
                        int num = (await UNOdb.GetCards(Context.User.Id)).Count;
                        await UserExtensions.SendMessageAsync(Context.Message.Author,
                            $"You have {num} {(num == 1 ? "card" : "cards")} left.", false,
                            await DisplayEmbed.DisplayCards(Context.User.Id, Context.Guild.Id));
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
        if(await UNOdb.IsPlayerInGame(Context.User.Id))
            {
                if(await UNOdb.IsPlayerInServerGame(Context.User.Id,Context.Guild.Id))
                {
                    if(await UNOdb.IsServerInGame(Context.Guild.Id))
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

        [Command("skip", RunMode = RunMode.Async), Alias("s")]
        [Help(new string[] { ".skip" }, "Skip your turn if the game is in fast mode. However, you are forced to draw two cards.", true, "UNObot 2.7")]
        public async Task Skip()
        {
            if (await UNOdb.IsPlayerInGame(Context.User.Id))
            {
                if (await UNOdb.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNOdb.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await QueueHandler.GetCurrentPlayer(Context.Guild.Id))
                        {
                            if (await UNOdb.GetGamemode(Context.Guild.Id) == 3)
                            {
                                List<Card> playerCards = await UNOdb.GetCards(Context.User.Id);
                                Card currentCard = await UNOdb.GetCurrentCard(Context.Guild.Id);
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
                                await QueueHandler.NextPlayer(Context.Guild.Id);
                                await UNOdb.AddCard(Context.User.Id, UNOcore.RandomCard());
                                await UNOdb.AddCard(Context.User.Id, UNOcore.RandomCard());
                                await ReplyAsync($"You have drawn two cards. It is now <@{await QueueHandler.GetCurrentPlayer(Context.Guild.Id)}>'s turn.");
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
        [Command("card", RunMode = RunMode.Async), Alias("top", "c")]
        [Help(new string[] { ".card" }, "See the most recently placed card.", true, "UNObot 0.2")]
        public async Task Card()
        {
            await UNOdb.AddGame(Context.Guild.Id);
            await UNOdb.AddUser(Context.User.Id, Context.User.Username);
            if (await UNOdb.IsPlayerInGame(Context.User.Id))
            {
                if (await UNOdb.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNOdb.IsServerInGame(Context.Guild.Id))
                    {
                        Card currentCard = await UNOdb.GetCurrentCard(Context.Guild.Id);
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
        [Command("quickplay", RunMode = RunMode.Async), Alias("quickdraw", "autoplay", "autodraw", "qp", "qd", "ap", "ad")]
        [Help(new string[] { ".quickplay" }, "Autodraw/play the first card possible. This is very inefficient, and should only be used if you are saving a wild card, or you don't have usable cards left.", true, "UNObot 2.4")]
        public async Task QuickPlay()
        {
            if (await UNOdb.IsPlayerInGame(Context.User.Id))
            {
                if (await UNOdb.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNOdb.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await QueueHandler.GetCurrentPlayer(Context.Guild.Id))
                        {
                            List<Card> playerCards = await UNOdb.GetCards(Context.User.Id);
                            Card currentCard = await UNOdb.GetCurrentCard(Context.Guild.Id);
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
                                    await UNOdb.AddCard(Context.User.Id, rngcard);
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
                                    if (rngcard.Color == "Wild")
                                    {
                                        foreach (Card cardTake in cardsTaken)
                                        {
                                            response += cardTake + "\n";
                                        }
                                        response += ($"\n\nYou have drawn {cardsDrawn} cards, however the autodrawer has stopped at a Wild card.\n" +
                                                         "If you want to draw for a regular card, run the command again.");
                                        await UserExtensions.SendMessageAsync(Context.Message.Author, response);
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
        [Command("players", RunMode = RunMode.Async), Alias("users", "pl")]
        [Help(new string[] { ".players" }, "See all players in the game, as well as the amount of cards they have. Note however that if the server is running in private mode, it will not show the exact amount of cards that they have.", false, "UNObot 1.0")]
        public async Task Players()
        {
            await ReplyAsync(".players has been deprecated and has been replaced with .game.");
            await Game();
        }
        [Command("game", RunMode = RunMode.Async), Help(new string[] { ".displayembed" }, "Display all information about the current game.", true, "UNObot 3.0")]
        public async Task Game()
        {
            await UNOdb.AddGame(Context.Guild.Id);
            await UNOdb.AddUser(Context.User.Id, Context.User.Username);
            if (await UNOdb.IsServerInGame(Context.Guild.Id))
                await ReplyAsync($"It is now <@{await QueueHandler.GetCurrentPlayer(Context.Guild.Id)}>'s turn.", false, await Modules.DisplayEmbed.DisplayGame(Context.Guild.Id));
            else
                await ReplyAsync("The game has not started!");
        }
        [Command("queue", RunMode = RunMode.Async), Alias("q")]
        [Help(new string[] { ".queue" }, "See which players are currently waiting to play a game.", true, "UNObot 2.4")]
        public async Task Queue()
        {
            await UNOdb.AddGame(Context.Guild.Id);
            await UNOdb.AddUser(Context.User.Id, Context.User.Username);
            Queue<ulong> currqueue = await UNOdb.GetUsersWithServer(Context.Guild.Id);
            if (await UNOdb.IsServerInGame(Context.Guild.Id))
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
        [Command("uno", RunMode = RunMode.Async), Alias("u")]
        [Help(new string[] { ".uno" }, "Quickly use this when you have one card left.", true, "UNObot 0.2")]
        public async Task UNOcmd()
        {
            await UNOdb.AddGame(Context.Guild.Id);
            await UNOdb.AddUser(Context.User.Id, Context.User.Username);
            if (await UNOdb.IsPlayerInGame(Context.User.Id))
            {
                if (await UNOdb.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNOdb.IsServerInGame(Context.Guild.Id))
                    {
                        if (await UNOdb.GetUNOPlayer(Context.Guild.Id) == Context.User.Id)
                        {
                            await ReplyAsync("Great, you have one card left! Everyone still has a chance however, so keep going!");
                            await UNOdb.SetUNOPlayer(Context.Guild.Id, 0);
                        }
                        else
                        {
                            await ReplyAsync("Uh oh, you still have more than one card! Two cards have been added to your hand.");
                            await UNOdb.AddCard(Context.User.Id, UNOcore.RandomCard());
                            await UNOdb.AddCard(Context.User.Id, UNOcore.RandomCard());
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
        [Command("start", RunMode = RunMode.Async)]
        [Help(new string[] { ".start" }, "Start the game you have joined in the current server. Now, you can also add an option to it, which currently include \"fast\", which allows the skip command, and \"private\", preventing others to see the exact amount of cards you have.", true, "UNObot 0.2")]
        public async Task Start()
        {
            await Start("normal");
        }
        [Command("start", RunMode = RunMode.Async)]
        [Help(new string[] { ".start (gamemode)" }, "Start the game you have joined in the current server. Now, you can also add an option to it, which currently include \"fast\", which allows the skip command, and \"private\", preventing others to see the exact amount of cards you have.", true, "UNObot 0.2")]

        public async Task Start(string mode)
        {
            if (await UNOdb.IsPlayerInGame(Context.User.Id))
            {
                await UNOdb.AddGame(Context.Guild.Id);
                await UNOdb.AddUser(Context.User.Id, Context.User.Username);
                if (await UNOdb.IsServerInGame(Context.Guild.Id))
                    await ReplyAsync("The game has already started!");
                else
                {
                    string Response = "";
                    switch (mode.ToLower().Trim())
                    {
                        case "private":
                            Response += "Playing in privacy!";
                            await UNOdb.AddGuild(Context.Guild.Id, 1, 2);
                            break;
                        case "fast":
                            Response += "Playing in fast mode!";
                            await UNOdb.AddGuild(Context.Guild.Id, 1, 3);
                            break;
                        case "normal":
                            Response += "Playing in normal mode!";
                            await UNOdb.AddGuild(Context.Guild.Id, 1, 1);
                            break;
                        default:
                            await ReplyAsync("That's not a valid mode!");
                            return;
                    }
                    await UNOdb.GetUsersAndAdd(Context.Guild.Id);
                    foreach (ulong player in await UNOdb.GetPlayers(Context.Guild.Id))
                    {
                        await UNOdb.UpdateStats(player, 1);
                    }
                    //randomize start
                    for (int i = 0; i < ThreadSafeRandom.ThisThreadsRandom.Next(0, await QueueHandler.PlayerCount(Context.Guild.Id)); i++)
                        await QueueHandler.NextPlayer(Context.Guild.Id);

                    Response += "\n\nGame has started. All information about your cards will be PMed.\n" +
                            "You have been given 7 cards; PM \"deck\" to view them.\n" +
                            "Remember; you have 1 minute and 30 seconds to place a card.\n" +
                            $"The first player is <@{await QueueHandler.GetCurrentPlayer(Context.Guild.Id)}>.\n";
                    Card currentCard = UNOcore.RandomCard();
                    while (currentCard.Color == "Wild")
                        currentCard = UNOcore.RandomCard();
                    switch (currentCard.Value)
                    {
                        case "+2":
                            var curuser = await QueueHandler.GetCurrentPlayer(Context.Guild.Id);
                            await UNOdb.AddCard(curuser, UNOcore.RandomCard());
                            await UNOdb.AddCard(curuser, UNOcore.RandomCard());
                            Response += $"\nToo bad <@{curuser}>, you just got two cards!";
                            break;
                        case "Reverse":
                            await QueueHandler.ReversePlayers(Context.Guild.Id);
                            Response += $"\nWhat? The order has been reversed! Now, it's <@{await QueueHandler.GetCurrentPlayer(Context.Guild.Id)}>'s turn.";
                            break;
                        case "Skip":
                            await QueueHandler.NextPlayer(Context.Guild.Id);
                            Response += $"What's this? A skip? Oh well, now it's <@{await QueueHandler.GetCurrentPlayer(Context.Guild.Id)}>'s turn.";
                            break;
                        default:
                            _ = 1;
                            break;
                    }
                    await UNOdb.SetCurrentCard(Context.Guild.Id, currentCard);
                    Response += $"\nCurrent card: {currentCard.ToString()}\n";
                    await UNOdb.UpdateDescription(Context.Guild.Id, "The game has just started!");
                    await ReplyAsync(Response);
                    await UNOdb.StarterCard(Context.Guild.Id);
                    AFKtimer.StartTimer(Context.Guild.Id);
                }
            }
            else
                await ReplyAsync("You have not joined a game!");
        }
        [Command("play", RunMode = RunMode.Async), Priority(2), Alias("put", "place", "p")]
        [Help(new string[] { ".play (color) (value)" }, "Play a card that is of the same color or value. Exceptions include all Wild cards, which you can play on any card.", true, "UNObot 0.2")]
        public async Task Play(string color, string value)
        {
            if (await UNOdb.IsPlayerInGame(Context.User.Id))
            {
                if (await UNOdb.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNOdb.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await QueueHandler.GetCurrentPlayer(Context.Guild.Id))
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
        [Command("play", RunMode = RunMode.Async), Priority(1), Alias("put", "place", "p")]
        [Help(new string[] { ".play (color) (value) (new color)" }, "Play a card that is of the same color or value. Exceptions include all Wild cards, which you can play on any card.", true, "UNObot 0.2")]
        public async Task PlayWild(string color, string value, string wild)
        {
            if (await UNOdb.IsPlayerInGame(Context.User.Id))
            {
                if (await UNOdb.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNOdb.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await QueueHandler.GetCurrentPlayer(Context.Guild.Id))
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
        [Command("setdefaultchannel", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageChannels), Alias("adddefaultchannel")]
        [Help(new string[] { ".setdefaultchannel" }, "Set the default channel for UNObot to chat in. Managers only.", true, "UNObot 2.0")]
        public async Task SetDefaultChannel()
        {
            await ReplyAsync($":white_check_mark: Set default UNO channel to #{Context.Channel.Name}.");
            await UNOdb.SetDefaultChannel(Context.Guild.Id, Context.Channel.Id);
            await UNOdb.SetHasDefaultChannel(Context.Guild.Id, true);

            //default channel should be allowed, by default
            var currentChannels = await UNOdb.GetAllowedChannels(Context.Guild.Id);
            currentChannels.Add(Context.Channel.Id);
            await UNOdb.SetAllowedChannels(Context.Guild.Id, currentChannels);
        }
        [Command("removedefaultchannel", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageChannels), Alias("deletedefaultchannel")]
        [Help(new string[] { ".removedefaultchannel" }, "Remove the default channel for UNObot to chat in. Managers only.", true, "UNObot 2.0")]
        public async Task RemoveDefaultChannel()
        {
            await ReplyAsync($":white_check_mark: Removed default UNO channel, assuming there was one.");
            if (!await UNOdb.HasDefaultChannel(Context.Guild.Id))
            {
                ulong channel = await UNOdb.GetDefaultChannel(Context.Guild.Id);
                //remove default channel
                var currentChannels = await UNOdb.GetAllowedChannels(Context.Guild.Id);
                currentChannels.Remove(channel);
                await UNOdb.SetAllowedChannels(Context.Guild.Id, currentChannels);
            }
            //ok tbh, it should be null, but doesn't really matter imo
            await UNOdb.SetDefaultChannel(Context.Guild.Id, Context.Guild.DefaultChannel.Id);
            await UNOdb.SetHasDefaultChannel(Context.Guild.Id, false);
        }
        [Command("enforcechannels", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageChannels), Alias("forcechannels")]
        [Help(new string[] { ".enforcechannels" }, "Only allow UNObot to recieve commands from enforced channels. Managers only.", true, "UNObot 2.0")]
        public async Task EnforceChannel()
        {
            //start check (make sure all channels exist at time of enforcing)
            var allowedChannels = await UNOdb.GetAllowedChannels(Context.Guild.Id);
            var currentChannels = Context.Guild.TextChannels.ToList();
            var currentChannelsIDs = new List<ulong>();
            foreach (var channel in currentChannels)
                currentChannelsIDs.Add(channel.Id);
            if (allowedChannels.Except(currentChannelsIDs).Any())
            {
                foreach (var toRemove in allowedChannels.Except(currentChannelsIDs))
                    allowedChannels.Remove(toRemove);
                await UNOdb.SetAllowedChannels(Context.Guild.Id, allowedChannels);
            }
            //end check
            if (allowedChannels.Count == 0)
            {
                await Context.Channel.SendMessageAsync("Error: Cannot enable enforcechannels if there are no allowed channels!");
                return;
            }
            if (!await UNOdb.HasDefaultChannel(Context.Guild.Id))
            {
                await Context.Channel.SendMessageAsync("Error: Cannot enable enforcechannels if there is no default channel!");
                return;
            }
            bool enforce = await UNOdb.EnforceChannel(Context.Guild.Id);
            await UNOdb.SetEnforceChannel(Context.Guild.Id, !enforce);
            if (!enforce)
                await ReplyAsync($":white_check_mark: Currently enforcing UNObot to only respond to messages in the filter.");
            else
                await ReplyAsync($":white_check_mark: Currently allowing UNObot to respond to messages from anywhere.");
        }
        [Command("addallowedchannel", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageChannels)]
        [Help(new string[] { ".addallowedchannel" }, "Allow the current channel to accept commands. Managers only.", true, "UNObot 2.0")]

        public async Task AddAllowedChannel()
        {
            if (!await UNOdb.HasDefaultChannel(Context.Guild.Id))
                await ReplyAsync("Error: You need to set a default channel first.");
            else if (await UNOdb.GetDefaultChannel(Context.Guild.Id) == Context.Channel.Id)
                await ReplyAsync("The default UNO channel has been set to this already; there is no need to add this as a default channel.");
            else if ((await UNOdb.GetAllowedChannels(Context.Guild.Id)).Contains(Context.Channel.Id))
                await ReplyAsync("This channel is already allowed! To see all channels, use .listallowedchannels.");
            else
            {
                var currentChannels = await UNOdb.GetAllowedChannels(Context.Guild.Id);
                currentChannels.Add(Context.Channel.Id);
                await UNOdb.SetAllowedChannels(Context.Guild.Id, currentChannels);
                await ReplyAsync($"Added #{Context.Channel.Name} to the list of allowed channels. Make sure you .enforcechannels for this to work.");
            }
        }
        [Command("listallowedchannels", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageChannels)]
        [Help(new string[] { ".listallowedchannels" }, "See all channels that UNObot can accept commands if enforced mode was on.", true, "UNObot 2.0")]
        public async Task ListAllowedChannels()
        {
            var allowedChannels = await UNOdb.GetAllowedChannels(Context.Guild.Id);
            //start check
            var currentChannels = Context.Guild.TextChannels.ToList();
            var currentChannelsIDs = new List<ulong>();
            foreach (var channel in currentChannels)
                currentChannelsIDs.Add(channel.Id);
            if (allowedChannels.Except(currentChannelsIDs).Any())
            {
                foreach (var toRemove in allowedChannels.Except(currentChannelsIDs))
                    allowedChannels.Remove(toRemove);
                await UNOdb.SetAllowedChannels(Context.Guild.Id, allowedChannels);
            }
            //end check
            bool enforced = await UNOdb.EnforceChannel(Context.Guild.Id);
            string yesno = enforced ? "Currently enforcing channels." : "Not enforcing channels.";
            string response = $"{yesno}\nCurrent channels allowed: \n";
            if (allowedChannels.Count == 0)
            {
                await ReplyAsync("There are no channels that are currently allowed. Add them with .addallowedchannel and .enforcechannels.");
                return;
            }
            foreach (ulong id in allowedChannels)
                response += $"- <#{id}>\n";
            await ReplyAsync(response);
        }
        [Command("removeallowedchannel", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageChannels)]
        [Help(new string[] { ".removeallowedchannel" }, "Remove a channel that UNObot previously was allowed to accept commands from.", true, "UNObot 2.0")]
        public async Task RemoveAllowedChannel()
        {
            //start check
            var allowedChannels = await UNOdb.GetAllowedChannels(Context.Guild.Id);
            var currentChannels = Context.Guild.TextChannels.ToList();
            var currentChannelsIDs = new List<ulong>();
            foreach (var channel in currentChannels)
                currentChannelsIDs.Add(channel.Id);
            if (allowedChannels.Except(currentChannelsIDs).Any())
            {
                foreach (var toRemove in allowedChannels.Except(currentChannelsIDs))
                    allowedChannels.Remove(toRemove);
                await UNOdb.SetAllowedChannels(Context.Guild.Id, allowedChannels);
            }
            //end check
            if (allowedChannels.Contains(Context.Channel.Id))
            {
                allowedChannels.Remove(Context.Channel.Id);
                await UNOdb.SetAllowedChannels(Context.Guild.Id, allowedChannels);
                await ReplyAsync($"Removed <#{Context.Channel.Id}> from the allowed channels!");
            }
            else
                await ReplyAsync("This channel was never an allowed channel.");
        }
        [Command("emergency", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageMessages), Alias("em", "leaveserver")]
        [Help(new string[] { ".emergency" }, "Kick the bot from the server.", false, "UNObot 2.0")]
        public async Task Emergency()
        {
            await ReplyAsync("If a rogue bot has taken over this account, it will be disabled with the use of this command.\n" +
                             $"Currently on **{Context.Guild.Name}**, goodbye world!\n" +
                             "To reinvite the bot, please use this link: https://discordapp.com/api/oauth2/authorize?client_id=477616287997231105&permissions=8192&scope=bot");
            await Context.Guild.LeaveAsync();
        }

        [Command("exit", RunMode = RunMode.Async)]
        public async Task Exit()
        {
            if (Context.User.Id == 278524552462598145)
                await ReplyAsync("ERROR: detected idiot");
            else if (Context.User.Id == 191397590946807809)
            {
                await ReplyAsync("Resetting!");
                Environment.Exit(0);
            }
            else
                await ReplyAsync("You do not have access to this command!");
        }
    }
}