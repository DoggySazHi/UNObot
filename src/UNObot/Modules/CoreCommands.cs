using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using System.Linq;
using System.Collections;
using System.Timers;
using Discord;

namespace DiscordBot.Modules
{
    public class Card
    {
        public string Color;
        public string Value;

        public override String ToString() => $"{Color} {Value}";

        public bool Equals(Card other) => Value == other.Value && Color == other.Color;
    }

    public class CoreCommands : ModuleBase<SocketCommandContext>
    {
        UNObot.Modules.UNOdb db = new UNObot.Modules.UNOdb();
        static System.Timers.Timer playTimer;

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
        [Command("purge"),RequireUserPermission(GuildPermission.Administrator)]
        public async Task Purge(int length)
        {
            var messages = await Context.Channel.GetMessagesAsync(length + 1).Flatten();

            await Context.Channel.DeleteMessagesAsync(messages);
            const int delay = 5000;
            var m = await ReplyAsync($"Purge completed. _This message will be deleted in {delay / 1000} seconds._");
            await Task.Delay(delay);
            await m.DeleteAsync();
        }
        [Command("join")]
        public async Task Join()
        {
            if (Program.gameStarted == true)
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
                await db.AddUser(Context.User.Id, Context.User.Username);
            await ReplyAsync($"{Context.User.Username} has been added to the queue.\n");
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
        [Command("pfpsteal")]
        public Task Pfpsteal(string user)
        {
            user = user.Trim(new Char[] { ' ', '<', '>', '!', '@' });
            if (!UInt64.TryParse(user, out ulong userid))
                return ReplyAsync("Mention the player with this command to see their stats.");
            Discord.WebSocket.DiscordSocketClient discordSocketClient = new Discord.WebSocket.DiscordSocketClient();
            Discord.WebSocket.SocketUser newuser = discordSocketClient.GetUser(userid);
            if (newuser == null)
                return ReplyAsync($"The user does not exist; did you type it wrong?");
            else
                return ReplyAsync($"<@{userid}>'s Profile Picture Link: {newuser.GetAvatarUrl()}");
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
            List<ulong> players = await db.GetPlayers();
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
        [Command("upupdowndownleftrightleftrightbastart")]
        public async Task Easteregg1()
        {
            await ReplyAsync($"<@419374055792050176> claims that <@{Context.User.Id}> is stupid.");
        }
        [Command("upupdowndownleftrightleftrightbastart")]
        public async Task Easteregg2(string response)
        {
            var messages = await this.Context.Channel.GetMessagesAsync(1).Flatten();

            await this.Context.Channel.DeleteMessagesAsync(messages);
            await ReplyAsync(response);
        }
        [Command("draw")]
        public async Task Draw()
        {
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                if (Program.gameStarted)
                {
                    Card card = UNOcore.RandomCard();
                    await UserExtensions.SendMessageAsync(Context.Message.Author, "You have recieved: " + card.Color + " " + card.Value + ".");
                    await db.AddCard(Context.User.Id, card);
                    return;
                }
                else
                {
                    await ReplyAsync($"<@{Context.User.Id}>, the game has not started!\n");
                    return;
                }
            }
            else
            {
                await ReplyAsync($"<@{Context.User.Id}>, you are not in game.\n");
                return;
            }
        }

        [Command("deck")]
        public async Task Deck()
        {
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                if (Program.gameStarted)
                {
                    List<Card> list = await db.GetCards(Context.User.Id);
                    string response = "Cards available:\n";
                    foreach (Card card in list)
                    {
                        response += card.Color + " " + card.Value + "\n";
                    }
                    await UserExtensions.SendMessageAsync(Context.Message.Author, response);
                    return;
                }
                else
                {
                    await ReplyAsync($"<@{Context.User.Id}>, the game has not started!\n");
                    return;
                }
            }
            else
            {
                await ReplyAsync($"<@{Context.User.Id}>, you are not in game.\n");
                return;
            }
        }

        [Command("card")]
        public async Task Card()
        {
            if (Program.gameStarted)
            {
                await ReplyAsync("Current card: " + Program.currentcard.Color + " " + Program.currentcard.Value);
                return;
            }
            else
                await ReplyAsync($"<@{Context.User.Id}>, the game has not started!\n");
        }
        [Command("help")]
        public async Task Help()
        {
            await ReplyAsync("Help has been sent. Or, I think it has.");
            await UserExtensions.SendMessageAsync(Context.Message.Author, "Commands: @UNOBot#4308 (Required) {Required in certain conditions} [Optional] " +
                               "- Join\n" +
                               "Join the queue.\n" +
                               "- Leave" +
                               "Leave the queue.\n" +
                               "- Start\n" +
                               "Start the game. Game only starts when 2 or more players are available.\n" +
                               "- Draw\n" +
                               "Get a card. This is randomized. Does not follow the 108 deck, but uses the probablity instead.\n" +
                               "- Play (Color/Wild) (#/Reverse/Skip/+2/+4/Color) {Wild color change}\n" +
                               "Play a card. You must have the card in your deck. Also, if you are placing a wildcard, type in the color as the next parameter.\n" +
                               "- Card\n" +
                               "See the last placed card.\n" +
                               "- Deck See the cards you have currently.\n" +
                               "- Uno\n" +
                               "Don't forget to say this when you end up with one card left!\n" +
                               "- Help\n" +
                               "Get a help list. But you probably knew this.\n" +
                              "- Seed (seed)\n" +
                              "Possibly increases your chance of winning.\n" +
                              "- Players\n" +
                              "See who is playing and who's turn is it.\n" +
                              "- Stats [player by mention]\n" +
                              "See if you or somebody else is a pro or a noob at UNO. It's probably the former.\n" +
                              "- Info\n" +
                              "See the current version and other stuff about UNObot.");
        }

        [Command("asdf")]
        public async Task Credits()
        {
            await ReplyAsync("UNObot: Programmed by DoggySazHi\n" +
                "Tested by Aragami and Fm\n" +
                "Created for the UBOWS server\n\n" +
                "Stickerz was here.\n" +
                "Blame LocalDisk and Harvest for any bugs.");
        }

        [Command("players")]
        public async Task Players()
        {
            List<ulong> players = await db.GetPlayers();
            if (Program.gameStarted)
            {
                await FixOrder();
                ulong id = players.ElementAt(Program.currentPlayer);
                string response = $"Current player: <@{id}>\n";
                foreach (ulong player in players)
                {
                    List<Card> loserlist = await db.GetCards(player);
                    response += $"- <@{player}> has {loserlist.Count} cards left.\n";
                }
                await ReplyAsync(response);
            }
            else
                await ReplyAsync($"<@{Context.User.Id}>, the game has not started!\n");
        }
        [Command("seed")]
        public async Task Seed(string seed)
        {
            UNOcore.r = new Random(seed.GetHashCode());
            await ReplyAsync("Seed has been updated. I do not guarantee 100% Wild cards.");
        }
        [Command("uno")]
        public async Task Uno()
        {
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                if (Program.gameStarted)
                {
                    if(Context.User.Id == Program.onecardleft)
                    {
                        await ReplyAsync($"Good job, <@{Context.User.Id}> has exactly one card left!");
                        Program.onecardleft = 0;
                    } else
                    {
                        await ReplyAsync($"<@{Context.User.Id}>, you still have more than one card! As a result, you are forced to draw two cards.");
                        List<Card> usercards = await db.GetCards(Context.User.Id);
                        usercards.Add(UNOcore.RandomCard());
                        usercards.Add(UNOcore.RandomCard());
                    }
                    return;
                }
                else
                    await ReplyAsync($"<@{Context.User.Id}>, the game has not started!\n");
            }
            else
                await ReplyAsync($"<@{Context.User.Id}>, you are not in game.\n");
        }
        [Command("start")]
        public async Task Start()
        {
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                if (Program.gameStarted)
                    await ReplyAsync($"<@{Context.User.Id}>, the game has already started!\n");
                else
                {
                    List<ulong> players = await db.GetPlayers();
                    Program.currentcard = UNOcore.RandomCard();
                    await NextPlayer();
                    foreach(ulong player in players)
                    {
                        await db.UpdateStats(player, 1);
                    }
                    await ReplyAsync("Game has started. All information about your cards will be PMed.\n" +
                               "You have been given 7 cards; PM \"deck\" to view them.\n" +
                               "Remember; you have 1 minute and 30 seconds to place a card.\n" +
                               $"The first player is <@{players.ElementAt(Program.currentPlayer)}>.\n");
                    SetTimer();
                    Program.gameStarted = true;
                    await db.StarterCard();
                    await ReplyAsync($"Current card: {Program.currentcard.Color } {Program.currentcard.Value}\n");
                }
            }
            await ReplyAsync($"<@{Context.User.Id}>, you are not in game.\n");
        }
        [Command("play"), Priority(2)]
        public async Task Play(string color, string value)
        {
            if (color.ToLower() == "wild")
            {
                await ReplyAsync("You need to rerun the command, but also add what color should it represent.\nEx. play Wild Color Green");
            }
            else
            {
                await PlayCommon(color, value, null);
            }
        }
        [Command("play"), Priority(1)]
        public async Task PlayWild(string color, string value, string wild)
        {
            await PlayCommon(color, value, wild);
        }

        public async Task PlayCommon(string color, string value, string wild)
        {
            if (await db.IsPlayerInGame(Context.User.Id))
            {
                if (Program.gameStarted)
                {
                    List<ulong> players = await db.GetPlayers();
                    switch (color.ToLower())
                    {
                        case "red":
                            color = "Red";
                            break;
                        case "blue":
                            color = "Blue";
                            break;
                        case "green":
                            color = "Green";
                            break;
                        case "yellow":
                            color = "Yellow";
                            break;
                        case "wild":
                            color = "Wild";
                            break;
                        default:
                            await ReplyAsync($"<@{Context.User.Id}>, that's not a color.");
                            return;
                    }
                    if (value.ToLower() == "reverse")
                        value = "Reverse";
                    if (value.ToLower() == "color")
                        value = "Color";
                    await FixOrder();
                    if (players.ElementAt(Program.currentPlayer) == Context.User.Id)
                    {
                        if(Program.onecardleft != 0)
                        {
                            await ReplyAsync($"<@{Program.onecardleft}> has forgotten to say UNO! They have been given 2 cards.");
                            await db.AddCard(Program.onecardleft, UNOcore.RandomCard());
                            await db.AddCard(Program.onecardleft, UNOcore.RandomCard());
                            Program.onecardleft = 0;
                        }
                        Card card = new Card
                        {
                            Color = color,
                            Value = value
                        };
                        List<Card> list = await db.GetCards(Context.User.Id);
                        bool exists = false;
                        foreach (Card c in list)
                        {
                            exists |= c.Equals(card);
                        }
                        if (exists)
                        {
                            if (card.Color == "Wild")
                            {
                                switch (wild.ToLower())
                                {
                                    case "red":
                                        Program.currentcard.Color = "Red";
                                        Program.currentcard.Value = "Any";
                                        break;
                                    case "blue":
                                        Program.currentcard.Color = "Blue";
                                        Program.currentcard.Value = "Any";
                                        break;
                                    case "yellow":
                                        Program.currentcard.Color = "Yellow";
                                        Program.currentcard.Value = "Any";
                                        break;
                                    case "green":
                                        Program.currentcard.Color = "Green";
                                        Program.currentcard.Value = "Any";
                                        break;
                                }
                            }
                            if (card.Color == Program.currentcard.Color || card.Value == Program.currentcard.Value || card.Color == "Wild")
                            {
                                ResetTimer();
                                if (card.Color != "Wild")
                                {
                                    Program.currentcard.Color = card.Color;
                                    Program.currentcard.Value = card.Value;
                                }
                                await UserExtensions.SendMessageAsync(Context.Message.Author, $"You have placed a {card.Color} {card.Value}.");
                                await db.RemoveCard(Context.User.Id, card);
                                await ReplyAsync($"<@{Context.User.Id}> has placed an " + card.Color + " " + card.Value + ".");
                                if (card.Color == "Wild")
                                    await ReplyAsync($"<@{Context.User.Id}> has decided that the new color is {Program.currentcard.Color}.");
                            }
                            else
                            {
                                await UserExtensions.SendMessageAsync(Context.Message.Author, "This is an illegal choice. Make sure your color or value matches.");
                                return;
                            }
                            await UserExtensions.SendMessageAsync(Context.Message.Author, $"Current card: {Program.currentcard.Color} {Program.currentcard.Value}");
                            if (Program.currentcard.Value == "Reverse")
                            {
                                await ReplyAsync($"The order has been reversed! Also, {players.ElementAt(Program.currentPlayer)} has been skipped!");
                                if (Program.order == 1)
                                    Program.order = 2;
                                else
                                    Program.order = 1;
                                await NextPlayer();
                            }
                            await NextPlayer();
                            if(Program.currentcard.Value == "Skip")
                            {
                                await ReplyAsync($"<@{players.ElementAt(Program.currentPlayer)}> has been skipped!");
                                await NextPlayer();
                            }
                            /*
                            if (Program.order == 1)
                            {
                                Program.currentPlayer++;
                                if (Program.currentcard.Value == "Skip")
                                {
                                    if (Program.currentPlayer >= players.Count)
                                        Program.currentPlayer = Program.currentPlayer - players.Count;
                                    await ReplyAsync($"<@{players.ElementAt(Program.currentPlayer)}> has been skipped!");
                                    Program.currentPlayer++;
                                }
                                if (Program.currentPlayer >= players.Count)
                                    Program.currentPlayer = Program.currentPlayer - players.Count;
                            }
                            else
                            {
                                Program.currentPlayer--;
                                if (Program.currentcard.Value == "Skip")
                                {
                                    if (Program.currentPlayer < 0)
                                        Program.currentPlayer = players.Count - Program.currentPlayer;
                                    await ReplyAsync($"<@{players.ElementAt(Program.currentPlayer)}> has been skipped!");
                                    Program.currentPlayer--;
                                }
                                if (Program.currentPlayer < 0)
                                    Program.currentPlayer = players.Count - Program.currentPlayer;
                            }*/
                            if (Program.currentcard.Value == "+2" || Program.currentcard.Value == "+4")
                            {
                                await ReplyAsync($"<@{players.ElementAt(Program.currentPlayer)}> has been skipped! They have also recieved a prize of {Program.currentcard.Value} cards.");
                                await NextPlayer();
                                List<Card> skiplist = await db.GetCards(players.ElementAt(Program.currentPlayer));

                                if (Program.currentcard.Value == "+2")
                                {
                                    skiplist.Add(UNOcore.RandomCard());
                                    skiplist.Add(UNOcore.RandomCard());
                                }
                                if (Program.currentcard.Value == "+4")
                                {
                                    skiplist.Add(UNOcore.RandomCard());
                                    skiplist.Add(UNOcore.RandomCard());
                                    skiplist.Add(UNOcore.RandomCard());
                                    skiplist.Add(UNOcore.RandomCard());
                                }
                            }
                            List<Card> userlist = await db.GetCards(Context.User.Id);
                            if (userlist.Count == 1)
                            {
                                Program.onecardleft = Context.User.Id;
                            }
                            if (userlist.Count == 0)
                            {
                                await ReplyAsync($"<@{Context.User.Id}> has won!");
                                await db.UpdateStats(Context.User.Id, 3);
                                string response = "";
                                foreach(ulong player in await db.GetPlayers())
                                {
                                    List<Card> loserlist = await db.GetCards(player);
                                    response += $"- <@{player}> had {loserlist.Count} cards left.\n";
                                    await db.UpdateStats(player, 2);
                                }
                                await ReplyAsync(response);
                                Program.currentPlayer = 0;
                                Program.gameStarted = false;
                                Program.order = 1;
                                Program.currentcard = null;
                                playTimer.Dispose();
                                await ReplyAsync("Game is over. You may rejoin now.");
                                return;
                            }
                            await FixOrder();
                            //HACK if something goes wrong, probably replace players with GetPlayers
                            await ReplyAsync($"It is now <@{players.ElementAt(Program.currentPlayer)}>'s turn.");
                            return;
                        }
                        else
                        {
                            await UserExtensions.SendMessageAsync(Context.Message.Author, "You do not have this card!");
                        }
                        return;
                    }
                    else
                        await ReplyAsync($"<@{Context.User.Id}>, it is not your turn!\n");
                }
                else
                    await ReplyAsync($"<@{Context.User.Id}>, the game has not started!\n");
            }
            else
            {
                await ReplyAsync($"<@{Context.User.Id}>, you are not in game.\n");
            }
        }

        void SetTimer() {
            playTimer = new Timer(90000);
            playTimer.Elapsed += AutoKick;
            playTimer.AutoReset = false;
            playTimer.Start();
        }

        void ResetTimer() {
            playTimer.Stop();
            playTimer.Start();
        }

        async Task NextPlayer()
        {
            List<ulong> players = await db.GetPlayers();
            await FixOrder();
            if (Program.order == 1)
            {
                Program.currentPlayer++;
                if (Program.currentPlayer >= players.Count)
                    Program.currentPlayer = 0;
            }
            else
            {
                Program.currentPlayer--;
                if (Program.currentPlayer < 0)
                    Program.currentPlayer = players.Count - 1;
            }
            await FixOrder();
        }

        async Task FixOrder()
        {
            List<ulong> players = await db.GetPlayers();
            if (Program.order == 1 && Program.currentPlayer >= players.Count)
                    Program.currentPlayer = 0;
            else if (Program.currentPlayer < 0)
                Program.currentPlayer = players.Count - 1;
        }
        async void AutoKick(Object source, ElapsedEventArgs e){
            List<ulong> players = await db.GetPlayers();
            ulong id = players.ElementAt(Program.currentPlayer);
            await db.RemoveUser(id);
            await ReplyAsync($"<@{id}>, you have been AFK removed.\n");
            await NextPlayer();
            ResetTimer();
            //reupdate
            players = await db.GetPlayers();
            if (players.Count == 0)
            {
                Program.currentPlayer = 0;
                Program.gameStarted = false;
                Program.order = 1;
                Program.currentcard = null;
                await ReplyAsync("Game has been reset, due to nobody in-game.");
                playTimer.Dispose();
            }
            else
            {
                await FixOrder();
                ulong id2 = players.ElementAt(Program.currentPlayer);
                await ReplyAsync($"It is now <@{id2}> turn.\n");
            }

        }

        public static class UNOcore
        {
            public static Random r = new Random();

            public static Card RandomCard()
            {
                Card card = new Card();

                //0-9 is number, 10 is actioncard
                int myCard = r.Next(0, 11);
                // see switch
                int myColor = r.Next(1, 5);

                switch (myColor)
                {
                    case 1:
                        card.Color = "Red";
                        break;
                    case 2:
                        card.Color = "Yellow";
                        break;
                    case 3:
                        card.Color = "Green";
                        break;
                    case 4:
                        card.Color = "Blue";
                        break;
                }

                if (myCard < 10)
                {
                    card.Value = myCard.ToString();
                }
                else
                {
                    //4 is wild, 1-3 is action
                    int action = r.Next(1, 5);
                    switch (action)
                    {
                        case 1:
                            card.Value = "Skip";
                            break;
                        case 2:
                            card.Value = "Reverse";
                            break;
                        case 3:
                            card.Value = "+2";
                            break;
                        case 4:
                            int wild = r.Next(1, 3);
                            card.Color = "Wild";
                            if (wild == 1)
                                card.Value = "Color";
                            else
                                card.Value = "+4";
                            break;
                    }
                }
                return card;
            }
        }
    }
}