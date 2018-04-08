﻿using System;
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
        public async Task purge(int length)
        {
            var messages = await this.Context.Channel.GetMessagesAsync((int)length + 1).Flatten();

            await this.Context.Channel.DeleteMessagesAsync(messages);
            const int delay = 5000;
            var m = await this.ReplyAsync($"Purge completed. _This message will be deleted in {delay / 1000} seconds._");
            await Task.Delay(delay);
            await m.DeleteAsync();
        }
        [Command("join")]
        public Task Join()
        {
            if(Program.gameStarted == true)
                return ReplyAsync($"The game has already started!\n");
            else if(db.IsPlayerInGame(Context.User.Id))
                return ReplyAsync($"{Context.User.Username}, you are already in game!\n");
            else
                db.AddUser(Context.User.Id, Context.User.Username);
            return ReplyAsync($"{Context.User.Username} has been added to the queue.\n");
        }
        [Command("stats")]
        public Task Stats()
        {
            int[] stats = db.GetStats(Context.User.Id);
            return ReplyAsync($"{Context.User.Username}'s stats:\n"
                                + $"Games joined: {stats[0]}\n"
                                + $"Games fully played: {stats[1]}\n"
                                + $"Games won: {stats[2]}");
        }
        [Command("stats")]
        public Task Stats2(string user)
        {
            user = user.Trim(new Char[] { ' ', '<', '>', '!', '@' });
            if (!UInt64.TryParse(user, out ulong userid))
                return ReplyAsync("Mention the player with this command to see their stats.");
            if (!db.UserExists(userid))
                return ReplyAsync($"<@{userid}> The user does not exist; did you type it wrong?");
            int[] stats = db.GetStats(userid);
            return ReplyAsync($"<@{userid}>'s stats:\n"
                                + $"Games joined: {stats[0]}\n"
                                + $"Games fully played: {stats[1]}\n"
                                + $"Games won: {stats[2]}");
        }
        [Command("leave")]
        public Task Leave()
        {
            if(db.IsPlayerInGame(Context.User.Id))
                db.RemoveUser(Context.User.Id);
            else
                return ReplyAsync($"{Context.User.Username}, you are already out of game!\n");
            List<ulong> players = db.Players;
            NextPlayer();
            if (players.Count == 0)
            {
                Program.currentPlayer = 0;
                Program.gameStarted = false;
                Program.order = 1;
                Program.currentcard = null;
                ReplyAsync("Game has been reset, due to nobody in-game.");
                playTimer.Dispose();
            }
            return ReplyAsync($"{Context.User.Username} has been removed from the queue.\n");
        }
        [Command("upupdowndownleftrightleftrightbastart")]
        public Task Easteregg1()
        {
            return ReplyAsync($"<@419374055792050176> claims that <@{Context.User.Id}> is stupid.");
        }
        [Command("upupdowndownleftrightleftrightbastart")]
        public async Task Easteregg2(string response)
        {
            var messages = await this.Context.Channel.GetMessagesAsync(1).Flatten();

            await this.Context.Channel.DeleteMessagesAsync(messages);
            await ReplyAsync(response);
        }
        [Command("draw")]
        public Task Draw()
        {
            if (db.IsPlayerInGame(Context.User.Id))
            {
                if (Program.gameStarted)
                {
                    Card card = UNOcore.RandomCard();
                    Discord.UserExtensions.SendMessageAsync(Context.Message.Author, "You have recieved: " + card.Color + " " + card.Value + ".");
                    db.AddCard(Context.User.Id, card);
                    return null;
                }
                else
                    return ReplyAsync($"<@{Context.User.Id}>, the game has not started!\n");
            }
            else
            {
                return ReplyAsync($"<@{Context.User.Id}>, you are not in game.\n");
            }
        }

        [Command("deck")]
        public Task Deck()
        {
            if (db.IsPlayerInGame(Context.User.Id))
            {
                if (Program.gameStarted)
                {
                    List<Card> list = db.GetCards(Context.User.Id);
                    string response = "Cards available:\n";
                    foreach (Card card in list)
                    {
                        response += card.Color + " " + card.Value + "\n";
                    }
                    Discord.UserExtensions.SendMessageAsync(Context.Message.Author, response);
                    return null;
                }
                else
                    return ReplyAsync($"<@{Context.User.Id}>, the game has not started!\n");
            }
            else
            {
                return ReplyAsync($"<@{Context.User.Id}>, you are not in game.\n");
            }
        }

        [Command("card")]
        public Task Card()
        {
            if (Program.gameStarted)
            {
                ReplyAsync("Current card: " + Program.currentcard.Color + " " + Program.currentcard.Value);
                return null;
            }
            else
                return ReplyAsync($"<@{Context.User.Id}>, the game has not started!\n");
        }
        [Command("help")]
        public Task Help()
        {
            ReplyAsync("Help has been sent. Or, I think it has.");
            return Discord.UserExtensions.SendMessageAsync(Context.Message.Author, "Commands: @UNOBot#4308 (Required) {Required in certain conditions} [Optional] " +
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
                              "See the current version and crap about UNObot.");
        }

        [Command("asdf")]
        public Task Credits()
        {
            return ReplyAsync("UNObot: Programmed by DoggySazHi\n" +
                "Tested by Aragami and Fm\n" +
                "Created for the UBOWS server\n\n" +
                "Stickerz was here.\n" +
                "Blame LocalDisk and Harvest for any bugs.");
        }

        [Command("players")]
        public Task Players()
        {
            List<ulong> players = db.Players;
            if (Program.gameStarted)
            {
                FixOrder();
                ulong id = players.ElementAt(Program.currentPlayer);
                string response = $"Current player: <@{id}>\n";
                foreach (ulong player in players)
                {
                    List<Card> loserlist = db.GetCards(player);
                    response += $"- <@{player}> has {loserlist.Count} cards left.\n";
                }
                return ReplyAsync(response);
            }
            else
                return ReplyAsync($"<@{Context.User.Id}>, the game has not started!\n");
        }
        [Command("seed")]
        public Task Seed(string seed)
        {
            UNOcore.r = new Random(seed.GetHashCode());
            return ReplyAsync("Seed has been updated. I do not guarantee 100% Wild cards.");
        }
        [Command("uno")]
        public Task Uno()
        {
            if (db.IsPlayerInGame(Context.User.Id))
            {
                if (Program.gameStarted)
                {
                    if(Context.User.Id == Program.onecardleft)
                    {
                        ReplyAsync($"Good job, <@{Context.User.Id}> has exactly one card left!");
                        Program.onecardleft = 0;
                    } else
                    {
                        ReplyAsync($"<@{Context.User.Id}>, you still have more than one card! As a result, you are forced to draw two cards.");
                        List<Card> usercards = db.GetCards(Context.User.Id);
                        usercards.Add(UNOcore.RandomCard());
                        usercards.Add(UNOcore.RandomCard());
                    }
                    return null;
                }
                else
                    return ReplyAsync($"<@{Context.User.Id}>, the game has not started!\n");
            }
            else
            {
                return ReplyAsync($"<@{Context.User.Id}>, you are not in game.\n");
            }
        }
        [Command("start")]
        public Task Start()
        {
            if (db.IsPlayerInGame(Context.User.Id))
            {
                if (Program.gameStarted)
                    return ReplyAsync($"<@{Context.User.Id}>, the game has already started!\n");
                else
                {
                    List<ulong> players = db.Players;
                    Program.currentcard = UNOcore.RandomCard();
                    Discord.WebSocket.DiscordSocketClient dsc = new Discord.WebSocket.DiscordSocketClient();
                    NextPlayer();
                    foreach(ulong player in db.Players)
                    {
                        db.UpdateStats(player, 1);
                    }
                    ReplyAsync("Game has started. All information about your cards will be PMed.\n" +
                               "You have been given 7 cards; PM \"deck\" to view them.\n" +
                               "Remember; you have 1 minute and 30 seconds to place a card.\n" +
                               $"The first player is <@{players.ElementAt(Program.currentPlayer)}>.\n");
                    SetTimer();
                    Program.gameStarted = true;
                    db.StarterCard();
                    return ReplyAsync("Current card: " +
                                                            Program.currentcard.Color + " " + Program.currentcard.Value + "\n");
                }
            }
            return ReplyAsync($"<@{Context.User.Id}>, you are not in game.\n");
        }
        [Command("play"), Priority(2)]
        public Task Play(string color, string value)
        {
            if (color.ToLower() == "wild")
            {
                ReplyAsync("You need to rerun the command, but also add what color should it represent.\nEx. play Wild Color Green");
                return null;
            }
            else
            {
                PlayCommon(color, value, null);
                return null;
            }
        }
        [Command("play"), Priority(1)]
        public Task PlayWild(string color, string value, string wild)
        {
            PlayCommon(color, value, wild);
            return null;
        }

        public void PlayCommon(string color, string value, string wild)
        {
            if (db.IsPlayerInGame(Context.User.Id))
            {
                if (Program.gameStarted)
                {
                    List<ulong> players = db.Players;
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
                            ReplyAsync($"<@{Context.User.Id}>, that's not a color.");
                            return;
                    }
                    if (value.ToLower() == "reverse")
                        value = "Reverse";
                    if (value.ToLower() == "color")
                        value = "Color";
                    FixOrder();
                    if (players.ElementAt(Program.currentPlayer) == Context.User.Id)
                    {
                        if(Program.onecardleft != 0)
                        {
                            ReplyAsync($"<@{Program.onecardleft}> has forgotten to say UNO! They have been given 2 cards.");
                            db.AddCard(Program.onecardleft, UNOcore.RandomCard());
                            db.AddCard(Program.onecardleft, UNOcore.RandomCard());
                            Program.onecardleft = 0;
                        }
                        Card card = new Card
                        {
                            Color = color,
                            Value = value
                        };
                        List<Card> list = db.GetCards(Context.User.Id);
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
                                Discord.UserExtensions.SendMessageAsync(Context.Message.Author, $"You have placed a {card.Color} {card.Value}.");
                                db.RemoveCard(Context.User.Id, card);
                                ReplyAsync($"<@{Context.User.Id}> has placed an " + card.Color + " " + card.Value + ".");
                                if (card.Color == "Wild")
                                    ReplyAsync($"<@{Context.User.Id}> has decided that the new color is {Program.currentcard.Color}.");
                            }
                            else
                            {
                                Discord.UserExtensions.SendMessageAsync(Context.Message.Author, "This is an illegal choice. Make sure your color or value matches.");
                                return;
                            }
                            Discord.UserExtensions.SendMessageAsync(Context.Message.Author, $"Current card: {Program.currentcard.Color} {Program.currentcard.Value}");
                            if (Program.currentcard.Value == "Reverse")
                            {
                                ReplyAsync($"The order has been reversed! Also, {players.ElementAt(Program.currentPlayer)} has been skipped!");
                                if (Program.order == 1)
                                    Program.order = 2;
                                else
                                    Program.order = 1;
                                NextPlayer();
                            }
                            //TODO somehow simplify this crap? Note: uses next player()
                            if (Program.order == 1)
                            {
                                Program.currentPlayer++;
                                if (Program.currentcard.Value == "Skip")
                                {
                                    if (Program.currentPlayer >= players.Count)
                                        Program.currentPlayer = Program.currentPlayer - players.Count;
                                    ReplyAsync($"<@{players.ElementAt(Program.currentPlayer)}> has been skipped!");
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
                                    ReplyAsync($"<@{players.ElementAt(Program.currentPlayer)}> has been skipped!");
                                    Program.currentPlayer--;
                                }
                                if (Program.currentPlayer < 0)
                                    Program.currentPlayer = players.Count - Program.currentPlayer;
                            }
                            if (Program.currentcard.Value == "+2" || Program.currentcard.Value == "+4")
                            {
                                ReplyAsync($"<@{players.ElementAt(Program.currentPlayer)}> has been skipped! They have also recieved a prize of {Program.currentcard.Value} cards.");
                                NextPlayer();
                                List<Card> skiplist = db.GetCards(players.ElementAt(Program.currentPlayer));

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
                            List<Card> userlist = db.GetCards(Context.User.Id);
                            if (userlist.Count == 1)
                            {
                                Program.onecardleft = Context.User.Id;
                            }
                            if (userlist.Count == 0)
                            {
                                ReplyAsync($"<@{Context.User.Id}> has won!");
                                db.UpdateStats(Context.User.Id, 3);
                                string response = "";
                                foreach(ulong player in db.Players)
                                {
                                    List<Card> loserlist = db.GetCards(player);
                                    response += $"- <@{player}> had {loserlist.Count} cards left.\n";
                                    db.UpdateStats(player, 2);
                                }
                                ReplyAsync(response);
                                Program.currentPlayer = 0;
                                Program.gameStarted = false;
                                Program.order = 1;
                                Program.currentcard = null;
                                playTimer.Dispose();
                                ReplyAsync("Game is over. You may rejoin now.");
                                return;
                            }
                            FixOrder();
                            ReplyAsync($"It is now <@{db.Players.ElementAt(Program.currentPlayer)}>'s turn.");
                            return;
                        }
                        else
                        {
                            Discord.UserExtensions.SendMessageAsync(Context.Message.Author, "You do not have this card!");
                        }
                        return;
                    }
                    else
                        ReplyAsync($"<@{Context.User.Id}>, it is not your turn!\n");
                }
                else
                    ReplyAsync($"<@{Context.User.Id}>, the game has not started!\n");
            }
            else
            {
                ReplyAsync($"<@{Context.User.Id}>, you are not in game.\n");
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

        void NextPlayer()
        {
            FixOrder();
            if (Program.order == 1)
            {
                Program.currentPlayer++;
                if (Program.currentPlayer >= db.Players.Count)
                    Program.currentPlayer = 0;
            }
            else
            {
                Program.currentPlayer--;
                if (Program.currentPlayer < 0)
                    Program.currentPlayer = db.Players.Count - 1;
            }
            FixOrder();
        }

        void FixOrder()
        {
            if (Program.order == 1 && Program.currentPlayer >= db.Players.Count)
                    Program.currentPlayer = 0;
            else if (Program.currentPlayer < 0)
                Program.currentPlayer = db.Players.Count - 1;
        }
        void AutoKick(Object source, ElapsedEventArgs e){
            ulong id = db.Players.ElementAt(Program.currentPlayer);
            db.RemoveUser(id);
            ReplyAsync($"<@{id}>, you have been AFK removed.\n");
            List<ulong> players = db.Players;
            NextPlayer();
            ResetTimer();
            //reupdate
            players = db.Players;
            if (players.Count == 0)
            {
                Program.currentPlayer = 0;
                Program.gameStarted = false;
                Program.order = 1;
                Program.currentcard = null;
                ReplyAsync("Game has been reset, due to nobody in-game.");
                playTimer.Dispose();
            }
            else
            {
                FixOrder();
                ulong id2 = players.ElementAt(Program.currentPlayer);
                ReplyAsync($"It is now <@{id2}> turn.\n");
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