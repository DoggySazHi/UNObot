using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UNObot.Services;
using static UNObot.Services.UNOCoreServices;

namespace UNObot.Modules
{
    public class UNOCommands : ModuleBase<SocketCommandContext>
    {
        readonly UNOPlayCardService playCard = new UNOPlayCardService();

        [Command("join", RunMode = RunMode.Async), Help(new[] { ".join" }, "Join the queue in the current server.", true, "UNObot 0.1")]
        [DisableDMs]
        public async Task Join()
        {
            await UNODatabaseService.AddGame(Context.Guild.Id);
            await UNODatabaseService.AddUser(Context.User.Id, Context.User.Username);
            if (await UNODatabaseService.IsServerInGame(Context.Guild.Id))
            {
                await ReplyAsync($"The game has already started in this server!\n");
                return;
            }
            if (await UNODatabaseService.IsPlayerInGame(Context.User.Id))
            {
                await ReplyAsync($"{Context.User.Username}, you are already in a game!\n");
                return;
            }
            await UNODatabaseService.AddUser(Context.User.Id, Context.User.Username, Context.Guild.Id);
            await ReplyAsync($"{Context.User.Username} has been added to the queue.\n");
        }

        [Command("leave", RunMode = RunMode.Async)]
        [DisableDMs]
        [Help(new[] { ".leave" }, "Leave the queue (or game) in the current server.", true, "UNObot 0.2")]
        public async Task Leave()
        {
            await UNODatabaseService.AddGame(Context.Guild.Id);
            await UNODatabaseService.AddUser(Context.User.Id, Context.User.Username);
            if (await UNODatabaseService.IsPlayerInGame(Context.User.Id) && Context.Guild.Id == await UNODatabaseService.GetUserServer(Context.User.Id))
            {
                await UNODatabaseService.RemoveUser(Context.User.Id);
                await QueueHandlerService.RemovePlayer(Context.User.Id, Context.Guild.Id);
            }
            else
            {
                await ReplyAsync($"{Context.User.Username}, you are already out of game! Note that you must run this command in the server you are playing in.\n");
                return;
            }
            if (await UNODatabaseService.IsServerInGame(Context.Guild.Id) && (await QueueHandlerService.PlayerCount(Context.Guild.Id) == 0))
            {
                await UNODatabaseService.ResetGame(Context.Guild.Id);
                await ReplyAsync($"Due to {Context.User.Username}'s departure, the game has been reset.");
                return;
            }
            await ReplyAsync($"{Context.User.Username} has been removed from the queue.\n");
            if (await QueueHandlerService.PlayerCount(Context.Guild.Id) > 1 && await UNODatabaseService.IsServerInGame(Context.Guild.Id))
            {
                await ReplyAsync($"It is now <@{await QueueHandlerService.GetCurrentPlayer(Context.Guild.Id)}>'s turn.");
            }
        }

        [Command("stats", RunMode = RunMode.Async)]
        [Help(new[] { ".stats" }, "Get the statistics of you or another player to see if they are a noob, pro, or hacker.", true, "UNObot 1.4")]
        public async Task Stats()
        {
            int[] stats = await UNODatabaseService.GetStats(Context.User.Id);
            string note = await UNODatabaseService.GetNote(Context.User.Id);
            if (!await UNODatabaseService.UserExists(Context.User.Id))
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
        [Help(new[] { ".stats (ping another player, or their ID)" }, "Get the statistics of you or another player to see if they are a noob, pro, or hacker.", true, "UNObot 1.4")]
        public async Task Stats2([Remainder] string user)
        {
            user = user.Trim();
            //Style of Username#XXXX or Username XXXX
            if ((user.Contains('#') || user.Contains(' ')) && user.Length >= 6 && int.TryParse(user.Substring(user.Length - 4), out int discriminator))
            {
                var userObj = Context.Client.GetUser(user[0..^5], discriminator.ToString());
                //Negative one is only passed in because it cannot convert to ulong; it will fail the TryParse and give a "Mention the player..." error.
                user = userObj != null ? userObj.Id.ToString() : (-1).ToString();
            }
            user = user.Trim(new char[] { ' ', '<', '>', '!', '@' });
            if (!ulong.TryParse(user, out ulong userid))
            {
                await ReplyAsync("Mention the player with this command to see their stats. Or if you want to be polite, try using their ID.");
                return;
            }
            if (!await UNODatabaseService.UserExists(userid))
            {
                await ReplyAsync($"The user does not exist; either you have typed it wrong, or that user doesn't exist in the UNObot database.");
                return;
            }
            int[] stats = await UNODatabaseService.GetStats(userid);
            string note = await UNODatabaseService.GetNote(userid);
            if (note != null)
            {
                await ReplyAsync($"NOTE: {note}");
            }
            await ReplyAsync($"{Context.Client.GetUser(userid).Username}'s stats:\n"
                                + $"Games joined: {stats[0]}\n"
                                + $"Games fully played: {stats[1]}\n"
                                + $"Games won: {stats[2]}");
        }

        [Command("setnote", RunMode = RunMode.Async)]
        [Help(new[] { ".setnote" }, "Set a note about yourself. Write nothing to delete your message", true, "UNObot 2.1")]
        public async Task SetNote()
        {
            await UNODatabaseService.RemoveNote(Context.User.Id);
            await ReplyAsync("Successfully removed note!");
        }

        [Command("setnote", RunMode = RunMode.Async)]
        [Help(new[] { ".setnote" }, "Set a note about yourself. Write nothing to delete your message", true, "UNObot 2.1")]
        public async Task SetNote([Remainder] string text)
        {
            text = text.Trim().Normalize();
            if (text == "")
                text = "???";
            else if (text.ToLower().Contains("discord") && text.ToLower().Contains("gg"))
            {
                await ReplyAsync("You are not allowed to put invites!");
                return;
            }
            await UNODatabaseService.SetNote(Context.User.Id, text);
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
            LoggerService.Log(LogSeverity.Debug, response);
            await ReplyAsync("UNObot was already succcessfully initialized in this server. But thank you.");
        }

        [Command("setusernote", RunMode = RunMode.Async), RequireOwner]
        [Help(new[] { ".setusernote" }, "Set a note about others. This command can only be ran by DoggySazHi.", false, "UNObot 2.1")]
        public async Task SetNote(string user, [Remainder] string text)
        {
            user = user.Trim(new Char[] { ' ', '<', '>', '!', '@' });
            if (!UInt64.TryParse(user, out ulong userid))
            {
                await ReplyAsync("Mention the player with this command to see their stats.");
                return;
            }
            if (!await UNODatabaseService.UserExists(userid))
            {
                await ReplyAsync($"The user does not exist; either you have typed it wrong, or that user doesn't exist in the UNObot database.");
                return;
            }
            if (text.Trim().Normalize() == "")
                text = "???";
            await UNODatabaseService.SetNote(userid, text);
            await ReplyAsync("Successfully set note!");
        }
        [Command("removenote", RunMode = RunMode.Async)]
        [Help(new[] { ".removenote" }, "Remove your current note.", true, "UNObot 2.1")]
        public async Task RemoveNote()
        {
            await UNODatabaseService.RemoveNote(Context.User.Id);
            await ReplyAsync("Successfully removed note!");
        }
        [Command("draw", RunMode = RunMode.Async), Alias("take", "dr", "tk")]
        [DisableDMs]
        [Help(new[] { ".draw" }, "Draw a randomized card, which is based off probabilities instead of the real deck.", true, "UNObot 0.2")]
        public async Task Draw()
        {
            await UNODatabaseService.AddGame(Context.Guild.Id);
            await UNODatabaseService.AddUser(Context.User.Id, Context.User.Username);
            if (await UNODatabaseService.IsPlayerInGame(Context.User.Id))
            {
                if (await UNODatabaseService.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNODatabaseService.IsServerInGame(Context.Guild.Id))
                    {
                        if (await QueueHandlerService.GetCurrentPlayer(Context.Guild.Id) == Context.User.Id)
                        {
                            if ((await UNODatabaseService.GetGamemode(Context.Guild.Id)).HasFlag(Gamemodes.Retro))
                            {
                                if (await UNODatabaseService.GetCardsDrawn(Context.Guild.Id) > 0)
                                {
                                    await ReplyAsync("You cannot draw again, as you have already drawn a card previously.");
                                    return;
                                }
                            }
                            Card card = RandomCard();
                            await UserExtensions.SendMessageAsync(Context.Message.Author, "You have recieved: " + card.Color + " " + card.Value + ".");
                            await UNODatabaseService.AddCard(Context.User.Id, card);
                            await UNODatabaseService.SetCardsDrawn(Context.Guild.Id, 1);
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
        [DisableDMs]
        [Help(new[] { ".deck" }, "View all of the cards you possess.", true, "UNObot 0.2")]
        public async Task Deck()
        {
            await UNODatabaseService.AddGame(Context.Guild.Id);
            await UNODatabaseService.AddUser(Context.User.Id, Context.User.Username);
            if (await UNODatabaseService.IsPlayerInGame(Context.User.Id))
            {
                if (await UNODatabaseService.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNODatabaseService.IsServerInGame(Context.Guild.Id))
                    {
                        int num = (await UNODatabaseService.GetCards(Context.User.Id)).Count;
                        await UserExtensions.SendMessageAsync(Context.Message.Author,
                            $"You have {num} {(num == 1 ? "card" : "cards")} left.", false,
                            await EmbedDisplayService.DisplayCards(Context.User.Id, Context.Guild.Id));
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
        [DisableDMs]
        [Help(new[] { ".skip" }, "Skip your turn if the game is in fast mode. However, you are forced to draw two cards.", true, "UNObot 2.7")]
        public async Task Skip()
        {
            if (await UNODatabaseService.IsPlayerInGame(Context.User.Id))
            {
                if (await UNODatabaseService.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNODatabaseService.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await QueueHandlerService.GetCurrentPlayer(Context.Guild.Id))
                        {
                            var Gamemode = await UNODatabaseService.GetGamemode(Context.Guild.Id);
                            if (Gamemode.HasFlag(Gamemodes.Fast))
                            {
                                var playerCards = await UNODatabaseService.GetCards(Context.User.Id);
                                var currentCard = await UNODatabaseService.GetCurrentCard(Context.Guild.Id);
                                var found = false;
                                foreach (var c in playerCards)
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
                                await QueueHandlerService.NextPlayer(Context.Guild.Id);
                                await UNODatabaseService.AddCard(Context.User.Id, RandomCard());
                                await UNODatabaseService.AddCard(Context.User.Id, RandomCard());
                                await ReplyAsync($"You have drawn two cards. It is now <@{await QueueHandlerService.GetCurrentPlayer(Context.Guild.Id)}>'s turn.");
                                AFKtimer.ResetTimer(Context.Guild.Id);
                            }
                            else if (Gamemode.HasFlag(Gamemodes.Retro))
                            {
                                var playerCards = await UNODatabaseService.GetCards(Context.User.Id);
                                var currentCard = await UNODatabaseService.GetCurrentCard(Context.Guild.Id);
                                var found = false;
                                foreach (var c in playerCards)
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

                                var CardsDrawn = await UNODatabaseService.GetCardsDrawn(Context.Guild.Id);
                                if (CardsDrawn == 0)
                                {
                                    await ReplyAsync("You cannot skip without drawing at least one card! HINT: You can try using .quickplay/.qp instead of .draw and .skip.");
                                    return;
                                }

                                // Useless, it will be cleared.
                                //await UNODatabaseService.SetCardsDrawn(Context.Guild.Id, CardsDrawn + 1);

                                await QueueHandlerService.NextPlayer(Context.Guild.Id);
                                await ReplyAsync($"You have skipped, and it is now <@{await QueueHandlerService.GetCurrentPlayer(Context.Guild.Id)}>'s turn.");
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
        [DisableDMs]
        [Help(new[] { ".card" }, "See the most recently placed card.", true, "UNObot 0.2")]
        public async Task Card()
        {
            await UNODatabaseService.AddGame(Context.Guild.Id);
            await UNODatabaseService.AddUser(Context.User.Id, Context.User.Username);
            if (await UNODatabaseService.IsPlayerInGame(Context.User.Id))
            {
                if (await UNODatabaseService.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNODatabaseService.IsServerInGame(Context.Guild.Id))
                    {
                        Card currentCard = await UNODatabaseService.GetCurrentCard(Context.Guild.Id);
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
        [DisableDMs]
        [Help(new[] { ".quickplay" }, "Autodraw/play the first card possible. This is very inefficient, and should only be used if you are saving a wild card, or you don't have usable cards left.", true, "UNObot 2.4")]
        public async Task QuickPlay()
        {
            async Task Skip()
            {
                await QueueHandlerService.NextPlayer(Context.Guild.Id);
                await ReplyAsync($"It is now <@{await QueueHandlerService.GetCurrentPlayer(Context.Guild.Id)}>'s turn.");
                AFKtimer.ResetTimer(Context.Guild.Id);
            }

            if (await UNODatabaseService.IsPlayerInGame(Context.User.Id))
            {
                if (await UNODatabaseService.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNODatabaseService.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await QueueHandlerService.GetCurrentPlayer(Context.Guild.Id))
                        {
                            var Gamemode = await UNODatabaseService.GetGamemode(Context.Guild.Id);
                            var PlayerCards = await UNODatabaseService.GetCards(Context.User.Id);
                            var CurrentCard = await UNODatabaseService.GetCurrentCard(Context.Guild.Id);

                            foreach (var c in PlayerCards)
                            {
                                if (c.Color == CurrentCard.Color || c.Value == CurrentCard.Value)
                                {
                                    await UserExtensions.SendMessageAsync(Context.Message.Author, "Played the first card that matched the criteria!");
                                    await ReplyAsync(await playCard.Play(c.Color, c.Value, null, Context.User.Id, Context.Guild.Id));
                                    return;
                                }
                            }

                            if (Gamemode.HasFlag(Gamemodes.Retro))
                            {
                                var CardsDrawn = await UNODatabaseService.GetCardsDrawn(Context.Guild.Id);
                                if (CardsDrawn > 0)
                                {
                                    await Skip();
                                    return;
                                }
                            }

                            var cardsDrawn = 0;
                            var cardsTaken = new List<Card>();
                            var response = "Cards drawn:\n";
                            while (true)
                            {
                                var rngcard = RandomCard();
                                await UNODatabaseService.AddCard(Context.User.Id, rngcard);
                                cardsTaken.Add(rngcard);
                                cardsDrawn++;

                                if (rngcard.Color == CurrentCard.Color || rngcard.Value == CurrentCard.Value)
                                {
                                    foreach (var cardTake in cardsTaken)
                                    {
                                        response += cardTake + "\n";
                                    }
                                    await ReplyAsync($"You have drawn {cardsDrawn} card{(cardsDrawn == 1 ? "" : "s")}.");
                                    await Context.Message.Author.SendMessageAsync(response);
                                    await ReplyAsync(await playCard.Play(rngcard.Color, rngcard.Value, null, Context.User.Id, Context.Guild.Id));
                                    break;
                                }

                                if (rngcard.Color == "Wild")
                                {
                                    foreach (var cardTake in cardsTaken)
                                    {
                                        response += cardTake + "\n";
                                    }
                                    response += ($"\n\nYou have drawn {cardsDrawn} cards, however the autodrawer has stopped at a Wild card." +
                                                     $"{(Gamemode.HasFlag(Gamemodes.Retro) ? "If you want to skip, use .skip or .quickplay." : "\nIf you want to draw for a regular card, run the command again.")}");
                                    await Context.Message.Author.SendMessageAsync(response);
                                    await UNODatabaseService.SetCardsDrawn(Context.Guild.Id, cardsDrawn);
                                    break;
                                }

                                if (Gamemode.HasFlag(Gamemodes.Retro))
                                {
                                    await ReplyAsync($"You have drawn a card and skipped.");
                                    await Context.Message.Author.SendMessageAsync($"You have drawn a {rngcard}.");
                                    await Skip();
                                    break;
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
        [DisableDMs]
        [Help(new[] { ".players" }, "See all players in the game, as well as the amount of cards they have. Note however that if the server is running in private mode, it will not show the exact amount of cards that they have.", false, "UNObot 1.0")]
        public async Task Players()
        {
            await ReplyAsync(".players has been deprecated and has been replaced with .game.");
            await Game();
        }

        [Command("game", RunMode = RunMode.Async), Help(new[] { ".game" }, "Display all information about the current game.", true, "UNObot 3.0")]
        [DisableDMs]
        public async Task Game()
        {
            await UNODatabaseService.AddGame(Context.Guild.Id);
            await UNODatabaseService.AddUser(Context.User.Id, Context.User.Username);
            if (await UNODatabaseService.IsServerInGame(Context.Guild.Id))
                await ReplyAsync($"It is now <@{await QueueHandlerService.GetCurrentPlayer(Context.Guild.Id)}>'s turn.", false, await EmbedDisplayService.DisplayGame(Context.Guild.Id));
            else
                await ReplyAsync("The game has not started!");
        }

        [Command("queue", RunMode = RunMode.Async), Alias("q")]
        [DisableDMs]
        [Help(new[] { ".queue" }, "See which players are currently waiting to play a game.", true, "UNObot 2.4")]
        public async Task Queue()
        {
            await UNODatabaseService.AddGame(Context.Guild.Id);
            await UNODatabaseService.AddUser(Context.User.Id, Context.User.Username);
            Queue<ulong> currqueue = await UNODatabaseService.GetUsersWithServer(Context.Guild.Id);
            if (await UNODatabaseService.IsServerInGame(Context.Guild.Id))
            {
                await ReplyAsync("Since the server is already in a game, you can also use .game!");
                await Game();
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
        [DisableDMs]
        [Help(new[] { ".uno" }, "Quickly use this when you have one card left.", true, "UNObot 0.2")]
        public async Task UNOcmd()
        {
            await UNODatabaseService.AddGame(Context.Guild.Id);
            await UNODatabaseService.AddUser(Context.User.Id, Context.User.Username);
            if (await UNODatabaseService.IsPlayerInGame(Context.User.Id))
            {
                if (await UNODatabaseService.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNODatabaseService.IsServerInGame(Context.Guild.Id))
                    {
                        if (await UNODatabaseService.GetUNOPlayer(Context.Guild.Id) == Context.User.Id)
                        {
                            await ReplyAsync("Great, you have one card left! Everyone still has a chance however, so keep going!");
                            await UNODatabaseService.SetUNOPlayer(Context.Guild.Id, 0);
                        }
                        else
                        {
                            var Description = await UNODatabaseService.GetDescription(Context.Guild.Id);
                            if (Description.Contains("forgot") && Description.Contains(Context.User.Id.ToString()))
                            {
                                await ReplyAsync("You have already been penalized; no extra cards will be drawn.");
                                return;
                            }
                            await ReplyAsync("Uh oh, you still have more than one card! Two cards have been added to your hand.");
                            await UNODatabaseService.AddCard(Context.User.Id, RandomCard());
                            await UNODatabaseService.AddCard(Context.User.Id, RandomCard());
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
        [DisableDMs]
        [Help(new[] { ".start" }, "Start the game you have joined in the current server. Now, you can also add an option to it, which currently include \"fast\", which allows the skip command, and \"private\", preventing others to see the exact amount of cards you have.", true, "UNObot 0.2")]
        public async Task Start()
        {
            await Start("normal");
        }
        [Command("start", RunMode = RunMode.Async)]
        [DisableDMs]
        [Help(new[] { ".start (gamemode)" }, "Start the game you have joined in the current server. Now, you can also add an option to it, which currently include \"fast\", which allows the skip command, and \"private\", preventing others to see the exact amount of cards you have.", true, "UNObot 0.2")]

        public async Task Start(params string[] Modes)
        {
            if (await UNODatabaseService.IsPlayerInGame(Context.User.Id))
            {
                await UNODatabaseService.AddGame(Context.Guild.Id);
                await UNODatabaseService.AddUser(Context.User.Id, Context.User.Username);
                if (await UNODatabaseService.IsServerInGame(Context.Guild.Id))
                    await ReplyAsync("The game has already started!");
                else
                {
                    var Response = "";
                    var FlagMode = Gamemodes.Normal;
                    foreach (var Mode in Modes)
                        switch (Mode.ToLower().Trim())
                        {
                            case "private":
                                FlagMode |= Gamemodes.Private;
                                break;
                            case "fast":
                                FlagMode |= Gamemodes.Fast;
                                break;
                            case "retro":
                                FlagMode |= Gamemodes.Retro;
                                break;
                            case "normal":
                                FlagMode |= Gamemodes.Normal;
                                break;
                            default:
                                await ReplyAsync($"\"{Mode}\" is not a valid mode!");
                                return;
                        }

                    // For interfering modes, cancel actions
                    if (FlagMode.HasFlag(Gamemodes.Fast | Gamemodes.Retro))
                    {
                        await ReplyAsync($"You cannot play both fast modes simultaneously.");
                        return;
                    }

                    await UNODatabaseService.AddGuild(Context.Guild.Id, 1, (ushort)FlagMode);
                    Response += $"Playing in modes: {FlagMode}!";
                    await UNODatabaseService.GetUsersAndAdd(Context.Guild.Id);
                    foreach (ulong player in await UNODatabaseService.GetPlayers(Context.Guild.Id))
                    {
                        await UNODatabaseService.UpdateStats(player, 1);
                    }
                    //randomize start
                    for (int i = 0; i < ThreadSafeRandom.ThisThreadsRandom.Next(0, await QueueHandlerService.PlayerCount(Context.Guild.Id)); i++)
                        await QueueHandlerService.NextPlayer(Context.Guild.Id);

                    Response += "\n\nGame has started. All information about your cards will be PMed.\n" +
                            "You have been given 7 cards; run \".deck\" to view them.\n" +
                            "Remember; you have 1 minute and 30 seconds to place a card.\n" +
                            $"The first player is <@{await QueueHandlerService.GetCurrentPlayer(Context.Guild.Id)}>.\n";
                    Card currentCard = RandomCard();
                    while (currentCard.Color == "Wild")
                        currentCard = RandomCard();
                    switch (currentCard.Value)
                    {
                        case "+2":
                            var curuser = await QueueHandlerService.GetCurrentPlayer(Context.Guild.Id);
                            await UNODatabaseService.AddCard(curuser, RandomCard());
                            await UNODatabaseService.AddCard(curuser, RandomCard());
                            Response += $"\nToo bad <@{curuser}>, you just got two cards!";
                            break;
                        case "Reverse":
                            await QueueHandlerService.ReversePlayers(Context.Guild.Id);
                            Response += $"\nWhat? The order has been reversed! Now, it's <@{await QueueHandlerService.GetCurrentPlayer(Context.Guild.Id)}>'s turn.";
                            break;
                        case "Skip":
                            await QueueHandlerService.NextPlayer(Context.Guild.Id);
                            Response += $"What's this? A skip? Oh well, now it's <@{await QueueHandlerService.GetCurrentPlayer(Context.Guild.Id)}>'s turn.";
                            break;
                    }
                    await UNODatabaseService.SetCurrentCard(Context.Guild.Id, currentCard);
                    Response += $"\nCurrent card: {currentCard}\n";
                    await UNODatabaseService.UpdateDescription(Context.Guild.Id, "The game has just started!");
                    await ReplyAsync(Response);
                    await UNODatabaseService.StarterCard(Context.Guild.Id);
                    AFKtimer.StartTimer(Context.Guild.Id);
                }
            }
            else
                await ReplyAsync("You have not joined a game!");
        }
        [Command("play", RunMode = RunMode.Async), Priority(2), Alias("put", "place", "p")]
        [DisableDMs]
        [Help(new[] { ".play (color) (value)" }, "Play a card that is of the same color or value. Exceptions include all Wild cards, which you can play on any card.", true, "UNObot 0.2")]
        public async Task Play(string color, string value)
        {
            if (await UNODatabaseService.IsPlayerInGame(Context.User.Id))
            {
                if (await UNODatabaseService.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNODatabaseService.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await QueueHandlerService.GetCurrentPlayer(Context.Guild.Id))
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
        [DisableDMs]
        [Help(new[] { ".play (color) (value) (new color)" }, "Play a card that is of the same color or value. Exceptions include all Wild cards, which you can play on any card.", true, "UNObot 0.2")]
        public async Task PlayWild(string color, string value, string wild)
        {
            if (await UNODatabaseService.IsPlayerInGame(Context.User.Id))
            {
                if (await UNODatabaseService.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await UNODatabaseService.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await QueueHandlerService.GetCurrentPlayer(Context.Guild.Id))
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
        [DisableDMs]
        [Help(new[] { ".setdefaultchannel" }, "Set the default channel for UNObot to chat in. Managers only.", true, "UNObot 2.0")]
        public async Task SetDefaultChannel()
        {
            await ReplyAsync($":white_check_mark: Set default UNO channel to #{Context.Channel.Name}.");
            await UNODatabaseService.SetDefaultChannel(Context.Guild.Id, Context.Channel.Id);
            await UNODatabaseService.SetHasDefaultChannel(Context.Guild.Id, true);

            //default channel should be allowed, by default
            var currentChannels = await UNODatabaseService.GetAllowedChannels(Context.Guild.Id);
            currentChannels.Add(Context.Channel.Id);
            await UNODatabaseService.SetAllowedChannels(Context.Guild.Id, currentChannels);
        }
        [Command("removedefaultchannel", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageChannels), Alias("deletedefaultchannel")]
        [DisableDMs]
        [Help(new[] { ".removedefaultchannel" }, "Remove the default channel for UNObot to chat in. Managers only.", true, "UNObot 2.0")]
        public async Task RemoveDefaultChannel()
        {
            await ReplyAsync($":white_check_mark: Removed default UNO channel, assuming there was one.");
            if (!await UNODatabaseService.HasDefaultChannel(Context.Guild.Id))
            {
                ulong channel = await UNODatabaseService.GetDefaultChannel(Context.Guild.Id);
                //remove default channel
                var currentChannels = await UNODatabaseService.GetAllowedChannels(Context.Guild.Id);
                currentChannels.Remove(channel);
                await UNODatabaseService.SetAllowedChannels(Context.Guild.Id, currentChannels);
            }
            //ok tbh, it should be null, but doesn't really matter imo
            await UNODatabaseService.SetDefaultChannel(Context.Guild.Id, Context.Guild.DefaultChannel.Id);
            await UNODatabaseService.SetHasDefaultChannel(Context.Guild.Id, false);
        }
        [Command("enforcechannels", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageChannels), Alias("forcechannels")]
        [DisableDMs]
        [Help(new[] { ".enforcechannels" }, "Only allow UNObot to recieve commands from enforced channels. Managers only.", true, "UNObot 2.0")]
        public async Task EnforceChannel()
        {
            //start check (make sure all channels exist at time of enforcing)
            var allowedChannels = await UNODatabaseService.GetAllowedChannels(Context.Guild.Id);
            var currentChannels = Context.Guild.TextChannels.ToList();
            var currentChannelsIDs = new List<ulong>();
            foreach (var channel in currentChannels)
                currentChannelsIDs.Add(channel.Id);
            if (allowedChannels.Except(currentChannelsIDs).Any())
            {
                foreach (var toRemove in allowedChannels.Except(currentChannelsIDs))
                    allowedChannels.Remove(toRemove);
                await UNODatabaseService.SetAllowedChannels(Context.Guild.Id, allowedChannels);
            }
            //end check
            if (allowedChannels.Count == 0)
            {
                await Context.Channel.SendMessageAsync("Error: Cannot enable enforcechannels if there are no allowed channels!");
                return;
            }
            if (!await UNODatabaseService.HasDefaultChannel(Context.Guild.Id))
            {
                await Context.Channel.SendMessageAsync("Error: Cannot enable enforcechannels if there is no default channel!");
                return;
            }
            bool enforce = await UNODatabaseService.EnforceChannel(Context.Guild.Id);
            await UNODatabaseService.SetEnforceChannel(Context.Guild.Id, !enforce);
            if (!enforce)
                await ReplyAsync($":white_check_mark: Currently enforcing UNObot to only respond to messages in the filter.");
            else
                await ReplyAsync($":white_check_mark: Currently allowing UNObot to respond to messages from anywhere.");
        }
        [Command("addallowedchannel", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageChannels)]
        [DisableDMs]
        [Help(new[] { ".addallowedchannel" }, "Allow the current channel to accept commands. Managers only.", true, "UNObot 2.0")]

        public async Task AddAllowedChannel()
        {
            if (!await UNODatabaseService.HasDefaultChannel(Context.Guild.Id))
                await ReplyAsync("Error: You need to set a default channel first.");
            else if (await UNODatabaseService.GetDefaultChannel(Context.Guild.Id) == Context.Channel.Id)
                await ReplyAsync("The default UNO channel has been set to this already; there is no need to add this as a default channel.");
            else if ((await UNODatabaseService.GetAllowedChannels(Context.Guild.Id)).Contains(Context.Channel.Id))
                await ReplyAsync("This channel is already allowed! To see all channels, use .listallowedchannels.");
            else
            {
                var currentChannels = await UNODatabaseService.GetAllowedChannels(Context.Guild.Id);
                currentChannels.Add(Context.Channel.Id);
                await UNODatabaseService.SetAllowedChannels(Context.Guild.Id, currentChannels);
                await ReplyAsync($"Added #{Context.Channel.Name} to the list of allowed channels. Make sure you .enforcechannels for this to work.");
            }
        }
        [Command("listallowedchannels", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageChannels)]
        [DisableDMs]
        [Help(new[] { ".listallowedchannels" }, "See all channels that UNObot can accept commands if enforced mode was on.", true, "UNObot 2.0")]
        public async Task ListAllowedChannels()
        {
            var allowedChannels = await UNODatabaseService.GetAllowedChannels(Context.Guild.Id);
            //start check
            var currentChannels = Context.Guild.TextChannels.ToList();
            var currentChannelsIDs = new List<ulong>();
            foreach (var channel in currentChannels)
                currentChannelsIDs.Add(channel.Id);
            if (allowedChannels.Except(currentChannelsIDs).Any())
            {
                foreach (var toRemove in allowedChannels.Except(currentChannelsIDs))
                    allowedChannels.Remove(toRemove);
                await UNODatabaseService.SetAllowedChannels(Context.Guild.Id, allowedChannels);
            }
            //end check
            bool enforced = await UNODatabaseService.EnforceChannel(Context.Guild.Id);
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
        [DisableDMs]
        [Help(new[] { ".removeallowedchannel" }, "Remove a channel that UNObot previously was allowed to accept commands from.", true, "UNObot 2.0")]
        public async Task RemoveAllowedChannel()
        {
            //start check
            var allowedChannels = await UNODatabaseService.GetAllowedChannels(Context.Guild.Id);
            var currentChannels = Context.Guild.TextChannels.ToList();
            var currentChannelsIDs = new List<ulong>();
            foreach (var channel in currentChannels)
                currentChannelsIDs.Add(channel.Id);
            if (allowedChannels.Except(currentChannelsIDs).Any())
            {
                foreach (var toRemove in allowedChannels.Except(currentChannelsIDs))
                    allowedChannels.Remove(toRemove);
                await UNODatabaseService.SetAllowedChannels(Context.Guild.Id, allowedChannels);
            }
            //end check
            if (allowedChannels.Contains(Context.Channel.Id))
            {
                allowedChannels.Remove(Context.Channel.Id);
                await UNODatabaseService.SetAllowedChannels(Context.Guild.Id, allowedChannels);
                await ReplyAsync($"Removed <#{Context.Channel.Id}> from the allowed channels!");
            }
            else
                await ReplyAsync("This channel was never an allowed channel.");
        }
        [Command("emergency", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.ManageMessages), Alias("em", "leaveserver")]
        [DisableDMs]
        [Help(new[] { ".emergency" }, "Kick the bot from the server.", false, "UNObot 2.0")]
        public async Task Emergency()
        {
            await ReplyAsync("If a rogue bot has taken over this account, it will be disabled with the use of this command.\n" +
                             $"Currently on **{Context.Guild.Name}**, goodbye world!\n" +
                             "To reinvite the bot, please use this link: https://discordapp.com/api/oauth2/authorize?client_id=477616287997231105&permissions=8192&scope=bot");
            await Context.Guild.LeaveAsync();
        }
    }
}