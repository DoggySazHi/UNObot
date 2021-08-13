using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Core.Services;
using UNObot.Core.UNOCore;
using UNObot.Plugins;
using UNObot.Plugins.Attributes;
using UNObot.Plugins.TerminalCore;

namespace UNObot.Core.Modules
{
    public class UNOCommands : UNObotModule<UNObotCommandContext>
    {
        private readonly UNOPlayCardService _playCard;
        private readonly DatabaseService _db;
        private readonly QueueHandlerService _queue;
        private readonly AFKTimerService _afk;
        private readonly EmbedService _embed;

        public UNOCommands(UNOPlayCardService playCard, DatabaseService db, QueueHandlerService queue, AFKTimerService afk, EmbedService embed)
        {
            _playCard = playCard;
            _db = db;
            _queue = queue;
            _afk = afk;
            _embed = embed;
        }

        [Command("join", RunMode = RunMode.Async)]
        [Help(new[] {".join"}, "Join the queue in the current server.", true, "UNObot 0.1")]
        [DisableDMs]
        public async Task Join()
        {
            await _db.AddGame(Context.Guild.Id);
            await _db.AddUser(Context.User.Id, Context.User.Username);
            if (await _db.IsServerInGame(Context.Guild.Id))
            {
                await ReplyAsync("The game has already started in this server!\n");
                return;
            }

            if (await _db.IsPlayerInGame(Context.User.Id))
            {
                await ReplyAsync($"{Context.User.Username}, you are already in a game!\n");
                return;
            }

            await _db.AddUser(Context.User.Id, Context.User.Username, Context.Guild.Id);
            await ReplyAsync($"{Context.User.Username} has been added to the queue.\n");
        }

        [Command("leave", RunMode = RunMode.Async)]
        [DisableDMs]
        [Help(new[] {".leave"}, "Leave the queue (or game) in the current server.", true, "UNObot 0.2")]
        public async Task Leave()
        {
            await _db.AddGame(Context.Guild.Id);
            await _db.AddUser(Context.User.Id, Context.User.Username);
            if (await _db.IsPlayerInGame(Context.User.Id) &&
                Context.Guild.Id == await _db.GetUserServer(Context.User.Id))
            {
                await _db.RemoveUser(Context.User.Id);
                await _queue.RemovePlayer(Context.User.Id, Context.Guild.Id);
            }
            else
            {
                await ReplyAsync(
                    $"{Context.User.Username}, you are already out of game! Note that you must run this command in the server you are playing in.\n");
                return;
            }

            if (await _db.IsServerInGame(Context.Guild.Id) &&
                await _queue.PlayerCount(Context.Guild.Id) == 0)
            {
                await _db.ResetGame(Context.Guild.Id);
                await ReplyAsync($"Due to {Context.User.Username}'s departure, the game has been reset.");
                return;
            }

            await ReplyAsync($"{Context.User.Username} has been removed from the queue.\n");
            if (await _queue.PlayerCount(Context.Guild.Id) > 1 &&
                await _db.IsServerInGame(Context.Guild.Id))
                await ReplyAsync(
                    $"It is now <@{await _queue.GetCurrentPlayer(Context.Guild.Id)}>'s turn.");
        }

        [Command("stats", RunMode = RunMode.Async)]
        [Help(new[] {".stats"},
            "Get the statistics of you or another player to see if they are a noob, pro, or cheater.", true,
            "UNObot 1.4")]
        public async Task Stats()
        {
            var stats = await _db.GetStats(Context.User.Id);
            var note = await _db.GetNote(Context.User.Id);
            if (!await _db.UserExists(Context.User.Id))
                await ReplyAsync("You do not currently exist in the database. Maybe you should play a game.");
            else
                await ReplyAsync((note != null ? $"NOTE: {note}\n" : "") 
                    + $"{Context.User.Username}'s stats:\n"
                    + $"Games joined: {stats.GamesJoined}\n"
                    + $"Games fully played: {stats.GamesPlayed}\n"
                    + $"Games won: {stats.GamesWon}");
        }

        [Command("stats", RunMode = RunMode.Async)]
        [Help(new[] {".stats (ping another player, or their ID)"},
            "Get the statistics of you or another player to see if they are a noob, pro, or cheater.", true,
            "UNObot 1.4")]
        public async Task Stats2([Remainder] string user)
        {
            user = user.Trim();
            //Style of Username#XXXX or Username XXXX
            if ((user.Contains('#') || user.Contains(' ')) && user.Length >= 6 &&
                int.TryParse(user.Substring(user.Length - 4), out var discriminator))
            {
                var userObj = Context.Client.GetUser(user[..^5], discriminator.ToString());
                //Negative one is only passed in because it cannot convert to ulong; it will fail the TryParse and give a "Mention the player..." error.
                user = userObj != null ? userObj.Id.ToString() : (-1).ToString();
            }

            user = user.Trim(' ', '<', '>', '!', '@');
            if (!ulong.TryParse(user, out var userid))
            {
                await ReplyAsync(
                    "Mention the player with this command to see their stats. Or if you want to be polite, try using their ID.");
                return;
            }

            if (!await _db.UserExists(userid))
            {
                await ReplyAsync(
                    "The user does not exist; either you have typed it wrong, or that user doesn't exist in the UNObot database.");
                return;
            }

            var stats = await _db.GetStats(userid);
            var note = await _db.GetNote(userid);
            await ReplyAsync((note != null ? $"NOTE: {note}\n" : "") 
                + $"{Context.Client.GetUser(userid).Username}'s stats:\n"
                + $"Games joined: {stats.GamesJoined}\n"
                + $"Games fully played: {stats.GamesPlayed}\n"
                + $"Games won: {stats.GamesWon}");
        }

        [Command("setnote", RunMode = RunMode.Async)]
        [Help(new[] {".setnote"}, "Set a note about yourself. Write nothing to delete your message", true,
            "UNObot 2.1")]
        public async Task SetNote()
        {
            await _db.RemoveNote(Context.User.Id);
            await ReplyAsync("Successfully removed note!");
        }

        [Command("setnote", RunMode = RunMode.Async)]
        [Help(new[] {".setnote"}, "Set a note about yourself. Write nothing to delete your message", true,
            "UNObot 2.1")]
        public async Task SetNote([Remainder] string text)
        {
            text = text.Trim().Normalize();
            if (text == "")
            {
                text = "???";
            }
            else if (text.ToLower().Contains("discord") && text.ToLower().Contains("gg"))
            {
                await ReplyAsync("You are not allowed to put invites!");
                return;
            }

            await _db.SetNote(Context.User.Id, text);
            await ReplyAsync("Successfully set note!");
        }

        [Command("setusernote", RunMode = RunMode.Async)]
        [RequireOwner]
        [Help(new[] {".setusernote"}, "Set a note about others. This command can only be run by DoggySazHi.", false,
            "UNObot 2.1")]
        public async Task SetNote(string user, [Remainder] string text)
        {
            user = user.Trim(' ', '<', '>', '!', '@');
            if (!ulong.TryParse(user, out var userid))
            {
                await ReplyAsync("Mention the player with this command to see their stats.");
                return;
            }

            if (!await _db.UserExists(userid))
            {
                await ReplyAsync(
                    "The user does not exist; either you have typed it wrong, or that user doesn't exist in the UNObot database.");
                return;
            }

            if (text.Trim().Normalize() == "")
                text = "???";
            await _db.SetNote(userid, text);
            await ReplyAsync("Successfully set note!");
        }

        [Command("removenote", RunMode = RunMode.Async)]
        [Help(new[] {".removenote"}, "Remove your current note.", true, "UNObot 2.1")]
        public async Task RemoveNote()
        {
            await _db.RemoveNote(Context.User.Id);
            await ReplyAsync("Successfully removed note!");
        }

        [Command("draw", RunMode = RunMode.Async)]
        [Alias("take", "dr", "tk")]
        [DisableDMs]
        [Help(new[] {".draw"}, "Draw a randomized card, which is based off probabilities instead of the real deck.",
            true, "UNObot 0.2")]
        public async Task Draw()
        {
            await _db.AddGame(Context.Guild.Id);
            await _db.AddUser(Context.User.Id, Context.User.Username);
            if (await _db.IsPlayerInGame(Context.User.Id))
            {
                if (await _db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await _db.IsServerInGame(Context.Guild.Id))
                    {
                        if (await _queue.GetCurrentPlayer(Context.Guild.Id) == Context.User.Id)
                        {
                            if ((await _db.GetGameMode(Context.Guild.Id)).HasFlag(GameMode.Retro))
                                if (await _db.GetCardsDrawn(Context.Guild.Id) > 0)
                                {
                                    await ReplyAsync(
                                        "You cannot draw again, as you have already drawn a card previously.");
                                    return;
                                }

                            var card = Card.RandomCard();
                            await Context.User.SendMessageAsync(
                                "You have recieved: " + card.Color + " " + card.Value + ".");
                            await _db.AddCard(Context.User.Id, card);
                            await _db.SetCardsDrawn(Context.Guild.Id, 1);
                            _afk.ResetTimer(Context.Guild.Id);

                            return;
                        }

                        await ReplyAsync("Why draw now? Draw when it's your turn!");
                    }
                    else
                    {
                        await ReplyAsync("The game has not started!");
                    }
                }
                else
                {
                    await ReplyAsync("You are in a game, however you are not in the right server!");
                }
            }
            else
            {
                await ReplyAsync("You are not in any game!");
            }
        }

        [Command("deck", RunMode = RunMode.Async)]
        [Alias("hand", "cards", "d", "h")]
        [DisableDMs]
        [Help(new[] {".deck"}, "View all of the cards you possess.", true, "UNObot 0.2")]
        public async Task Deck()
        {
            await _db.AddGame(Context.Guild.Id);
            await _db.AddUser(Context.User.Id, Context.User.Username);
            if (await _db.IsPlayerInGame(Context.User.Id))
            {
                if (await _db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await _db.IsServerInGame(Context.Guild.Id))
                    {
                        var num = (await _db.GetCards(Context.User.Id)).Count;
                        await Context.User.SendMessageAsync(
                            $"You have {num} {(num == 1 ? "card" : "cards")} left.", false,
                            await _embed.DisplayCards(Context.User.Id, Context.Guild.Id));
                        _afk.ResetTimer(Context.Guild.Id);
                    }
                    else
                    {
                        await ReplyAsync("The game has not started!");
                    }
                }
                else
                {
                    await ReplyAsync("The game has not started, or you are not in the right server!");
                }
            }
            else
            {
                await ReplyAsync("You are not in any game!");
            }
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Alias("s")]
        [DisableDMs]
        [Help(new[] {".skip"}, "Skip your turn if the game is in fast mode. However, you are forced to draw two cards.",
            true, "UNObot 2.7")]
        public async Task Skip()
        {
            if (await _db.IsPlayerInGame(Context.User.Id))
            {
                if (await _db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await _db.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await _queue.GetCurrentPlayer(Context.Guild.Id))
                        {
                            var gamemode = await _db.GetGameMode(Context.Guild.Id);
                            if (gamemode.HasFlag(GameMode.Fast))
                            {
                                var playerCards = await _db.GetCards(Context.User.Id);
                                var currentCard = await _db.GetCurrentCard(Context.Guild.Id);
                                var found = false;
                                foreach (var c in playerCards)
                                    if (c.Color == currentCard.Color || c.Value == currentCard.Value)
                                    {
                                        found = true;
                                        break;
                                    }

                                if (found)
                                {
                                    await ReplyAsync(
                                        "You cannot skip because you have a card that matches the criteria!");
                                    return;
                                }

                                await _queue.NextPlayer(Context.Guild.Id);
                                await _db.AddCard(Context.User.Id, Card.RandomCard(2));
                                await ReplyAsync(
                                    $"You have drawn two cards. It is now <@{await _queue.GetCurrentPlayer(Context.Guild.Id)}>'s turn.");
                                _afk.ResetTimer(Context.Guild.Id);
                            }
                            else if (gamemode.HasFlag(GameMode.Retro))
                            {
                                var playerCards = await _db.GetCards(Context.User.Id);
                                var currentCard = await _db.GetCurrentCard(Context.Guild.Id);
                                var found = false;
                                foreach (var c in playerCards)
                                    if (c.Color == currentCard.Color || c.Value == currentCard.Value)
                                    {
                                        found = true;
                                        break;
                                    }

                                if (found)
                                {
                                    await ReplyAsync(
                                        "You cannot skip because you have a card that matches the criteria!");
                                    return;
                                }

                                var cardsDrawn = await _db.GetCardsDrawn(Context.Guild.Id);
                                if (cardsDrawn == 0)
                                {
                                    await ReplyAsync(
                                        "You cannot skip without drawing at least one card! HINT: You can try using .quickplay/.qp instead of .draw and .skip.");
                                    return;
                                }

                                // Useless, it will be cleared.
                                //await _db.SetCardsDrawn(Context.Guild.Id, CardsDrawn + 1);

                                await _queue.NextPlayer(Context.Guild.Id);
                                await ReplyAsync(
                                    $"You have skipped, and it is now <@{await _queue.GetCurrentPlayer(Context.Guild.Id)}>'s turn.");
                                _afk.ResetTimer(Context.Guild.Id);
                            }
                            else
                            {
                                await ReplyAsync("The current game doesn't allow skipping!");
                            }
                        }
                        else
                        {
                            await ReplyAsync("It is not your turn!");
                        }
                    }
                    else
                    {
                        await ReplyAsync("The game has not started!");
                    }
                }
                else
                {
                    await ReplyAsync("You are in a game, however you are not in the right server!");
                }
            }
            else
            {
                await ReplyAsync("You are not in any game!");
            }
        }

        [Command("card", RunMode = RunMode.Async)]
        [Alias("top", "c")]
        [DisableDMs]
        [Help(new[] {".card"}, "See the most recently placed card.", true, "UNObot 0.2")]
        public async Task CardCmd()
        {
            await _db.AddGame(Context.Guild.Id);
            await _db.AddUser(Context.User.Id, Context.User.Username);
            if (await _db.IsPlayerInGame(Context.User.Id))
            {
                if (await _db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await _db.IsServerInGame(Context.Guild.Id))
                    {
                        var currentCard = await _db.GetCurrentCard(Context.Guild.Id);
                        await ReplyAsync("Current card: " + currentCard);
                    }
                    else
                    {
                        await ReplyAsync("The game has not started!");
                    }
                }
                else
                {
                    await ReplyAsync("You are in a game, however you are not in the right server!");
                }
            }
            else
            {
                await ReplyAsync("You are not in any game!");
            }
        }

        [Command("quickplay", RunMode = RunMode.Async)]
        [Alias("quickdraw", "autoplay", "autodraw", "qp", "qd", "ap", "ad")]
        [DisableDMs]
        [Help(new[] {".quickplay"},
            "Autodraw/play the first card possible. This is very inefficient, and should only be used if you are saving a wild card, or you don't have usable cards left.",
            true, "UNObot 2.4")]
        public async Task QuickPlay()
        {
            async Task SkipQP()
            {
                await _queue.NextPlayer(Context.Guild.Id);
                await ReplyAsync(
                    $"It is now <@{await _queue.GetCurrentPlayer(Context.Guild.Id)}>'s turn.");
                _afk.ResetTimer(Context.Guild.Id);
            }

            if (await _db.IsPlayerInGame(Context.User.Id))
            {
                if (await _db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await _db.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await _queue.GetCurrentPlayer(Context.Guild.Id))
                        {
                            var gamemode = await _db.GetGameMode(Context.Guild.Id);
                            var playerCards = await _db.GetCards(Context.User.Id);
                            var currentCard = await _db.GetCurrentCard(Context.Guild.Id);

                            foreach (var c in playerCards)
                                if (c.Color == currentCard.Color || c.Value == currentCard.Value)
                                {
                                    await Context.User.SendMessageAsync(
                                        "Played the first card that matched the criteria!");
                                    await ReplyAsync(await _playCard.Play(c.Color, c.Value, null, Context));
                                    return;
                                }

                            if (gamemode.HasFlag(GameMode.Retro))
                            {
                                var cardsAlreadyDrawn = await _db.GetCardsDrawn(Context.Guild.Id);
                                if (cardsAlreadyDrawn > 0)
                                {
                                    await SkipQP();
                                    return;
                                }
                            }

                            var cardsDrawn = 0;
                            var cardsTaken = new List<Card>();
                            var response = "Cards drawn:\n";
                            while (true)
                            {
                                var rngCard = Card.RandomCard();
                                await _db.AddCard(Context.User.Id, rngCard);
                                cardsTaken.Add(rngCard);
                                cardsDrawn++;

                                if (rngCard.Color == currentCard.Color || rngCard.Value == currentCard.Value)
                                {
                                    foreach (var cardTake in cardsTaken) response += cardTake + "\n";
                                    await ReplyAsync(
                                        $"You have drawn {cardsDrawn} card{(cardsDrawn == 1 ? "" : "s")}.");
                                    await Context.User.SendMessageAsync(response);
                                    await ReplyAsync(await _playCard.Play(rngCard.Color, rngCard.Value, null,
                                        Context));
                                    break;
                                }

                                if (rngCard.Color == "Wild")
                                {
                                    foreach (var cardTake in cardsTaken) response += cardTake + "\n";
                                    response +=
                                        $"\n\nYou have drawn {cardsDrawn} cards, however the autodrawer has stopped at a Wild card." +
                                        $"{(gamemode.HasFlag(GameMode.Retro) ? "If you want to skip, use .skip or .quickplay." : "\nIf you want to draw for a regular card, run the command again.")}";
                                    await Context.User.SendMessageAsync(response);
                                    await _db.SetCardsDrawn(Context.Guild.Id, cardsDrawn);
                                    break;
                                }

                                if (gamemode.HasFlag(GameMode.Retro))
                                {
                                    await ReplyAsync("You have drawn a card and skipped.");
                                    await Context.User.SendMessageAsync($"You have drawn a {rngCard}.");
                                    await SkipQP();
                                    break;
                                }
                            }

                            _afk.ResetTimer(Context.Guild.Id);
                        }
                        else
                        {
                            await ReplyAsync("It is not your turn!");
                        }
                    }
                    else
                    {
                        await ReplyAsync("The game has not started!");
                    }
                }
                else
                {
                    await ReplyAsync("You are in a game, however you are not in the right server!");
                }
            }
            else
            {
                await ReplyAsync("You are not in any game!");
            }
        }

        [Command("players", RunMode = RunMode.Async)]
        [Alias("users", "pl")]
        [DisableDMs]
        [Help(new[] {".players"},
            "See all players in the game, as well as the amount of cards they have. Note however that if the server is running in private mode, it will not show the exact amount of cards that they have.",
            false, "UNObot 1.0")]
        public async Task Players()
        {
            await ReplyAsync(".players has been deprecated and has been replaced with .game.");
            await Game();
        }

        [Command("game", RunMode = RunMode.Async)]
        [Help(new[] {".game"}, "Display all information about the current game.", true, "UNObot 3.0")]
        [DisableDMs]
        public async Task Game()
        {
            await _db.AddGame(Context.Guild.Id);
            await _db.AddUser(Context.User.Id, Context.User.Username);
            if (await _db.IsServerInGame(Context.Guild.Id))
                await ReplyAsync($"It is now <@{await _queue.GetCurrentPlayer(Context.Guild.Id)}>'s turn.",
                    false, await _embed.DisplayGame(Context.Guild.Id));
            else
                await ReplyAsync("The game has not started!");
        }

        [Command("queue", RunMode = RunMode.Async)]
        [Alias("q")]
        [DisableDMs]
        [Help(new[] {".queue"}, "See which players are currently waiting to play a game.", true, "UNObot 2.4")]
        public async Task Queue()
        {
            await _db.AddGame(Context.Guild.Id);
            await _db.AddUser(Context.User.Id, Context.User.Username);
            var currqueue = await _db.GetUsersWithServer(Context.Guild.Id);
            if (await _db.IsServerInGame(Context.Guild.Id))
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

            var response = "Current queue players:\n";
            foreach (var player in currqueue)
                response += $"- <@{player}>\n";
            await ReplyAsync(response);
        }

        [Command("uno", RunMode = RunMode.Async)]
        [Alias("u")]
        [DisableDMs]
        [Help(new[] {".uno"}, "Quickly use this when you have one card left.", true, "UNObot 0.2")]
        public async Task CallUNO()
        {
            await _db.AddGame(Context.Guild.Id);
            await _db.AddUser(Context.User.Id, Context.User.Username);
            if (await _db.IsPlayerInGame(Context.User.Id))
            {
                if (await _db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await _db.IsServerInGame(Context.Guild.Id))
                    {
                        var unoPlayer = await _db.GetUNOPlayer(Context.Guild.Id);
                        var gamemode = await _db.GetGameMode(Context.Guild.Id);
                        if (unoPlayer == Context.User.Id)
                        {
                            await ReplyAsync(
                                "Great, you have one card left! Everyone still has a chance however, so keep going!");
                            await _db.SetUNOPlayer(Context.Guild.Id, 0);
                        }
                        else if (unoPlayer != 0 && gamemode.HasFlag(GameMode.UNOCallout))
                        {
                            await ReplyAsync(
                                $"<@{unoPlayer}> was too slow to call out their UNO by {Context.User.Username}! They have been given two cards.");
                            await _db.AddCard(unoPlayer, Card.RandomCard(2));
                            await _db.SetUNOPlayer(Context.Guild.Id, 0);
                        }
                        else
                        {
                            var description = await _db.GetDescription(Context.Guild.Id);
                            if (description.Contains("forgot") && description.Contains(Context.User.Id.ToString()))
                            {
                                await ReplyAsync("You have already been penalized; no extra cards will be drawn.");
                                return;
                            }

                            await ReplyAsync(
                                "Uh oh, you still have more than one card! Two cards have been added to your hand.");
                            await _db.AddCard(Context.User.Id, Card.RandomCard(2));
                        }
                    }
                    else
                    {
                        await ReplyAsync("The game has not started!");
                    }
                }
                else
                {
                    await ReplyAsync("You are in a game, however you are not in the right server!");
                }
            }
            else
            {
                await ReplyAsync("You are not in any game!");
            }
        }

        [Command("start", RunMode = RunMode.Async)]
        [DisableDMs]
        [Help(new[] {".start"},
            "Start the game you have joined in the current server. Now, you can also add an option to it, which currently include \"fast\", which allows the skip command, \"retro\", which like fast, allows skipping but limits draws, \"unocallout\", allowing .uno to be used to penalize a person who forgot to call out UNO, and \"private\", preventing others to see the exact amount of cards you have.",
            true, "UNObot 0.2")]
        public async Task Start()
        {
            await Start("normal");
        }

        [Command("start", RunMode = RunMode.Async)]
        [DisableDMs]
        [Help(new[] {".start (gamemode)"},
            "Start the game you have joined in the current server. Now, you can also add an option to it, which currently include \"fast\", which allows the skip command, \"retro\", which like fast, allows skipping but limits draws, \"unocallout\", allowing .uno to be used to penalize a person who forgot to call out UNO, and \"private\", preventing others to see the exact amount of cards you have.",
            true, "UNObot 0.2")]
        public async Task Start(params string[] modes)
        {
            if (await _db.IsPlayerInGame(Context.User.Id))
            {
                await _db.AddGame(Context.Guild.Id);
                await _db.AddUser(Context.User.Id, Context.User.Username);
                if (await _db.IsServerInGame(Context.Guild.Id))
                {
                    await ReplyAsync("The game has already started!");
                }
                else
                {
                    var response = "";
                    var flagMode = GameMode.Normal;
                    foreach (var mode in modes)
                        switch (mode.ToLower().Trim())
                        {
                            case "private":
                                flagMode |= GameMode.Private;
                                break;
                            case "fast":
                                flagMode |= GameMode.Fast;
                                break;
                            case "retro":
                                flagMode |= GameMode.Retro;
                                break;
                            case "unocallout":
                                flagMode |= GameMode.UNOCallout;
                                break;
                            case "normal":
                                flagMode |= GameMode.Normal;
                                break;
                            default:
                                await ReplyAsync($"\"{mode}\" is not a valid mode!");
                                return;
                        }

                    // For interfering modes, cancel actions
                    if (flagMode.HasFlag(GameMode.Fast | GameMode.Retro))
                    {
                        await ReplyAsync("You cannot play both fast modes simultaneously.");
                        return;
                    }

                    await _db.AddGuild(Context.Guild.Id, true, (byte) flagMode);
                    response += $"Playing in modes: {flagMode}!";
                    await _db.GetUsersAndAdd(Context.Guild.Id);
                    foreach (var player in await _db.GetPlayers(Context.Guild.Id))
                        await _db.UpdateStats(player, 1);
                    //randomize start
                    for (var i = 0;
                        i < ThreadSafeRandom.ThisThreadsRandom.Next(0,
                            await _queue.PlayerCount(Context.Guild.Id));
                        i++)
                        await _queue.NextPlayer(Context.Guild.Id);

                    response += "\n\nThe game has started. All information about your cards will be DMed.\n" +
                                "You have been given 7 cards; run \".hand\" to view them.\n" +
                                "Remember, you have 1 minute and 30 seconds to place a card.\n" +
                                $"The first player is <@{await _queue.GetCurrentPlayer(Context.Guild.Id)}>.\n";
                    var currentCard = Card.RandomCard();
                    while (currentCard.Color == "Wild")
                        currentCard = Card.RandomCard();
                    switch (currentCard.Value)
                    {
                        case "+2":
                            var curuser = await _queue.GetCurrentPlayer(Context.Guild.Id);
                            await _db.AddCard(curuser, Card.RandomCard(2));
                            response += $"\nToo bad <@{curuser}>, you just got two cards!";
                            break;
                        case "Reverse":
                            await _queue.ReversePlayers(Context.Guild.Id);
                            response +=
                                $"\nWhat? The order has been reversed! Now, it's <@{await _queue.GetCurrentPlayer(Context.Guild.Id)}>'s turn.";
                            break;
                        case "Skip":
                            await _queue.NextPlayer(Context.Guild.Id);
                            response +=
                                $"What's this? A skip? Oh well, now it's <@{await _queue.GetCurrentPlayer(Context.Guild.Id)}>'s turn.";
                            break;
                    }

                    await _db.SetCurrentCard(Context.Guild.Id, currentCard);
                    response += $"\nCurrent card: {currentCard}\n";
                    await _db.UpdateDescription(Context.Guild.Id, "The game has just started!");
                    await ReplyAsync(response);
                    await _db.StarterCard(Context.Guild.Id);
                    _afk.StartTimer(Context.Guild.Id);
                }
            }
            else
            {
                await ReplyAsync("You have not joined a game!");
            }
        }

        [Command("play", RunMode = RunMode.Async)]
        [Priority(3)]
        [Alias("put", "place", "p")]
        [DisableDMs]
        [Help(new[] {".play (color) (value)"},
            "Play a card that is of the same color or value. Exceptions include all Wild cards, which you can play on any card.",
            true, "UNObot 0.2")]
        public async Task Play(string color, string value)
        {
            if (await _db.IsPlayerInGame(Context.User.Id))
            {
                if (await _db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await _db.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await _queue.GetCurrentPlayer(Context.Guild.Id))
                        {
                            if (color.ToLower()[0] == 'w')
                            {
                                await ReplyAsync(
                                    "You need to rerun the command, but also add what color should it represent.\nEx. .play Wild Color Green or .play Wild +4 Green");
                            }
                            else
                            {
                                _afk.ResetTimer(Context.Guild.Id);
                                await ReplyAsync(await _playCard.Play(color, value, null, Context));
                            }
                        }
                        else
                        {
                            await ReplyAsync("It is not your turn!");
                        }
                    }
                    else
                    {
                        await ReplyAsync("The game has not started!");
                    }
                }
                else
                {
                    await ReplyAsync("You are in a game, however you are not in the right server!");
                }
            }
            else
            {
                await ReplyAsync("You are not in any game!");
            }
        }

        [Command("play", RunMode = RunMode.Async)]
        [Priority(2)]
        [Alias("put", "place", "p")]
        [DisableDMs]
        [Help(new[] {".play (color) (value) (new color/uno)"},
            "Play a card that is of the same color or value. Exceptions include all Wild cards, which you can play on any card.",
            true, "UNObot 0.2")]
        public async Task Play(string color, string value, string option)
        {
            if (color.ToLower()[0] == 'w')
                await PlayWild(color, value, option);
            else if (option.ToLower()[0] == 'u')
            {
                await Play(color, value);
                await CallUNO();
            }
        }
        
        [Command("play", RunMode = RunMode.Async)]
        [Priority(1)]
        [Alias("put", "place", "p")]
        [DisableDMs]
        [Help(new[] {".play (color) (value) (new color) (uno)"},
            "Play a card that is of the same color or value. Exceptions include all Wild cards, which you can play on any card.",
            true, "UNObot 0.2")]
        public async Task Play(string color, string value, string wild, string option)
        {
            await PlayWild(color, value, wild);
            if (option.ToLower()[0] == 'u')
                await CallUNO();
        }
        
        private async Task PlayWild(string color, string value, string wild)
        {
            if (await _db.IsPlayerInGame(Context.User.Id))
            {
                if (await _db.IsPlayerInServerGame(Context.User.Id, Context.Guild.Id))
                {
                    if (await _db.IsServerInGame(Context.Guild.Id))
                    {
                        if (Context.User.Id == await _queue.GetCurrentPlayer(Context.Guild.Id))
                        {
                            _afk.ResetTimer(Context.Guild.Id);
                            await ReplyAsync(await _playCard.Play(color, value, wild, Context));
                        }
                        else
                        {
                            await ReplyAsync("It is not your turn!");
                        }
                    }
                    else
                    {
                        await ReplyAsync("The game has not started!");
                    }
                }
                else
                {
                    await ReplyAsync("You are in a game, however you are not in the right server!");
                }
            }
            else
            {
                await ReplyAsync("You are not in any game!");
            }
        }
    }
}