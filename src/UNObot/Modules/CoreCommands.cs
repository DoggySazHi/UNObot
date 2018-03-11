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
                $"{Context.Client.CurrentUser.Username} - Created by DoggySazHi\n Version {Program.version}");
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

        [Command("join")]
        public Task Join()
        {
            db.AddUser(Context.User.Id, Context.User.Username);
            return ReplyAsync($"{Context.User.Username} has been added to the queue.\n");
        }
        [Command("leave")]
        public Task Leave()
        {
            db.RemoveUser(Context.User.Id, Context.User.Username);
            return ReplyAsync($"{Context.User.Username} has been removed from the queue.\n");
            /*
            if (Program.players.Contains(Context.User.Id))
            {
                Program.players.Remove(Context.User.Id);
                ReplyAsync($"{Context.User.Username} has been removed from the queue.\n");
                if (Program.order == 1)
                {
                    Program.currentPlayer++;
                    if (Program.currentPlayer >= Program.players.Count)
                        Program.currentPlayer = Program.currentPlayer - Program.players.Count;
                }
                else
                {
                    Program.currentPlayer--;
                    if (Program.currentPlayer < 0)
                        Program.currentPlayer = Program.players.Count - Program.currentPlayer;
                }
                if (Program.players.Count == 0)
                {
                    Program.currentPlayer = 0;
                    Program.gameStarted = false;
                    Program.order = 1;
                    Program.currentcard = null;
                    ReplyAsync("Game has been reset, due to nobody in-game.");
                    playTimer.Dispose();
                }
                return null;
            }
            else
            {
                return ReplyAsync($"<@{Context.User.Id}>, you are already out of the queue!\n");
            }
            */
        }
        [Command("upupdowndownleftrightleftrightbastart")]
        public Task Easteregg1()
        {
            return ReplyAsync($"<@419374055792050176> claims that <@{Context.User.Id}> is stupid.");
        }
        [Command("upupdowndownleftrightleftrightbastart")]
        public Task Easteregg2(string response)
        {
            return ReplyAsync(response);
        }
        [Command("draw")]
        public Task Draw()
        {
            if (Program.players.Contains(Context.User.Id))
            {
                if (Program.gameStarted)
                {
                    Card card = UNOcore.RandomCard();
                    Discord.UserExtensions.SendMessageAsync(Context.Message.Author, "You have recieved: " + card.Color + " " + card.Value + ".");
                    List<Card> playercards = (List<Card>)Program.players[Context.User.Id];
                    playercards.Add(card);
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
            if (Program.players.Contains(Context.User.Id))
            {
                if (Program.gameStarted)
                {
                    List<Card> list = (List<Card>)Program.players[Context.User.Id];
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
                               "See who is playing and who's turn is it.\n");
        }

        [Command("asdf")]
        public Task Credits()
        {
            return ReplyAsync("UNObot: Programmed by DoggySazHi\n" +
                "Tested by Aragami and Fm\n" +
                "Created for the UBOWS server\n\n" +
                "Stickerz was here.");
        }

        [Command("players")]
        public Task Players()
        {
            if (Program.gameStarted)
            {
                if (Program.order == 1)
                {
                    Program.currentPlayer++;
                    if (Program.currentPlayer >= Program.players.Count)
                        Program.currentPlayer = Program.currentPlayer - Program.players.Count;
                }
                else
                {
                    Program.currentPlayer--;
                    if (Program.currentPlayer < 0)
                        Program.currentPlayer = Program.players.Count - Program.currentPlayer;
                }
                UInt64.TryParse(Program.players.Cast<DictionaryEntry>().ElementAt(Program.currentPlayer).Key.ToString(), out ulong id);
                string response = $"Current player: <@{id}>";
                foreach (ulong player in Program.players.Keys)
                {
                    List<Card> loserlist = (List<Card>)Program.players[player];
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
            if (Program.players.Contains(Context.User.Id))
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
                        List<Card> usercards = (List<Card>) Program.players[Context.User.Id];
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
            if (Program.players.Contains(Context.User.Id))
            {
                if (Program.gameStarted)
                    return ReplyAsync($"<@{Context.User.Id}>, the game has already started!\n");
                else
                {
                    Program.currentcard = UNOcore.RandomCard();
                    Discord.WebSocket.DiscordSocketClient dsc = new Discord.WebSocket.DiscordSocketClient();
                    if (Program.order == 1)
                    {
                        Program.currentPlayer++;
                        if (Program.currentPlayer >= Program.players.Count)
                            Program.currentPlayer = Program.currentPlayer - Program.players.Count;
                    }
                    else
                    {
                        Program.currentPlayer--;
                        if (Program.currentPlayer < 0)
                            Program.currentPlayer = Program.players.Count - Program.currentPlayer;
                    }
                    UInt64.TryParse(Program.players.Cast<DictionaryEntry>().ElementAt(0).Key.ToString(), out ulong result);
                    ReplyAsync("Game has started. Please remember; PM the bot to avoid bleeding!\n" +
                               "You have been given 7 cards; PM \"deck\" to view them.\n" +
                               "Remember; you have 1 minute and 30 seconds to place a card.\n" +
                               $"The first player is <@{result}>.\n");
                    SetTimer();
                    Program.gameStarted = true;
                    foreach (ulong player in Program.players.Keys)
                    {
                        List<Card> list = (List<Card>)Program.players[player];
                        for (int i = 1; i <= 7; i++)
                        {
                            list.Add(UNOcore.RandomCard());
                        }
                    }
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
            //TODO Case sensitive value? Check reset, and +4 not working
            if (Program.players.Contains(Context.User.Id))
            {
                if (Program.gameStarted)
                {
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
                    if (Program.currentPlayer > Program.players.Count || Program.currentPlayer < 0)
                    {
                        if (Program.order == 1)
                        {
                            if (Program.currentPlayer >= Program.players.Count)
                                Program.currentPlayer = Program.currentPlayer - Program.players.Count;
                        }
                        else
                        {
                            if (Program.currentPlayer < 0)
                                Program.currentPlayer = Program.players.Count - Program.currentPlayer;
                        }
                    }
                    bool shouldwork = UInt64.TryParse(Program.players.Cast<DictionaryEntry>().ElementAt(Program.currentPlayer).Key.ToString(), out ulong result);
                    if (!shouldwork)
                    {
                        ReplyAsync($"<@{Context.User.Id}>, you apparently don't exist.");
                        return;
                    }
                    if (result == Context.User.Id)
                    {
                        if(Program.onecardleft != 0)
                        {
                            ReplyAsync($"<@{Program.onecardleft}> has forgotten to say UNO! They have been given 2 cards.");
                            List <Card> forgotuno = (List<Card>)Program.players[Program.onecardleft];
                            forgotuno.Add(UNOcore.RandomCard());
                            forgotuno.Add(UNOcore.RandomCard());
                            Program.onecardleft = 0;
                        }
                        Card card = new Card
                        {
                            Color = color,
                            Value = value
                        };
                        List<Card> list = (List<Card>)Program.players[Context.User.Id];
                        bool exists = false;
                        int cardindex = 0;
                        foreach (Card c in list)
                        {
                            if (c.Equals(card))
                            {
                                exists = true;
                                cardindex = list.IndexOf(c);
                            }
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
                                list.RemoveAt(cardindex);
                                Program.players[Context.User.Id] = list;
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
                                SendAll("The order has been reversed!");
                                if (Program.order == 1)
                                    Program.order = 2;
                                else
                                    Program.order = 1;
                            }
                            if (Program.order == 1)
                            {
                                Program.currentPlayer++;
                                if (Program.currentcard.Value == "Skip")
                                {
                                    if (Program.currentPlayer >= Program.players.Count)
                                        Program.currentPlayer = Program.currentPlayer - Program.players.Count;
                                    UInt64.TryParse(Program.players.Cast<DictionaryEntry>().ElementAt(Program.currentPlayer).Key.ToString(), out ulong skipped);
                                    SendAll($"<@{skipped}> has been skipped!");
                                    Program.currentPlayer++;
                                }
                                if (Program.currentPlayer >= Program.players.Count)
                                    Program.currentPlayer = Program.currentPlayer - Program.players.Count;
                            }
                            else
                            {
                                Program.currentPlayer--;
                                if (Program.currentcard.Value == "Skip")
                                {
                                    if (Program.currentPlayer < 0)
                                        Program.currentPlayer = Program.players.Count - Program.currentPlayer;
                                    UInt64.TryParse(Program.players.Cast<DictionaryEntry>().ElementAt(Program.currentPlayer).Key.ToString(), out ulong skipped);
                                    SendAll($"<@{skipped}> has been skipped!");
                                    Program.currentPlayer--;
                                }
                                if (Program.currentPlayer < 0)
                                    Program.currentPlayer = Program.players.Count - Program.currentPlayer;
                            }
                            if (Program.currentcard.Value == "+2" || Program.currentcard.Value == "+4")
                            {
                                if (Program.order == 1)
                                {
                                    Program.currentPlayer++;
                                    if (Program.currentPlayer >= Program.players.Count)
                                        Program.currentPlayer = Program.currentPlayer - Program.players.Count;
                                }
                                else
                                {
                                    Program.currentPlayer--;
                                    if (Program.currentPlayer < 0)
                                        Program.currentPlayer = Program.players.Count - Program.currentPlayer;
                                }
                                UInt64.TryParse(Program.players.Cast<DictionaryEntry>().ElementAt(Program.currentPlayer).Key.ToString(), out ulong skipped);
                                List<Card> skiplist = (List<Card>)Program.players[skipped];

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
                                ReplyAsync($"<@{skipped}> has been skipped! They have also recieved a prize of {Program.currentcard.Value} cards.");
                                if (Program.order == 1)
                                {
                                    Program.currentPlayer++;
                                    if (Program.currentPlayer >= Program.players.Count)
                                        Program.currentPlayer = Program.currentPlayer - Program.players.Count;
                                }
                                else
                                {
                                    Program.currentPlayer--;
                                    if (Program.currentPlayer < 0)
                                        Program.currentPlayer = Program.players.Count - Program.currentPlayer;
                                }
                            }
                            List< Card > userlist = (List<Card>)Program.players[Context.User.Id];
                            if (userlist.Count == 1)
                            {
                                Program.onecardleft = Context.User.Id;
                            }
                            if (userlist.Count == 0)
                            {
                                ReplyAsync($"<@{Context.User.Id}> has won!");
                                string response = "";
                                foreach(ulong player in Program.players.Keys)
                                {
                                    List<Card> loserlist = (List<Card>)Program.players[player];
                                    response += $"- <@{player}> had {loserlist.Count} cards left.\n";
                                }
                                ReplyAsync(response);
                                Program.currentPlayer = 0;
                                Program.gameStarted = false;
                                Program.order = 1;
                                Program.currentcard = null;
                                Program.players = new System.Collections.Specialized.OrderedDictionary();
                                playTimer.Dispose();
                                ReplyAsync("Game is over. You may rejoin now.");
                            }
                            if (Program.order == 1)
                            {
                                Program.currentPlayer++;
                                if (Program.currentPlayer >= Program.players.Count)
                                    Program.currentPlayer = Program.currentPlayer - Program.players.Count;
                            }
                            else
                            {
                                Program.currentPlayer--;
                                if (Program.currentPlayer < 0)
                                    Program.currentPlayer = Program.players.Count - Program.currentPlayer;
                            }
                            UInt64.TryParse(Program.players.Cast<DictionaryEntry>().ElementAt(Program.currentPlayer).Key.ToString(), out ulong id);
                            ReplyAsync($"It is now <@{id}>'s turn.");
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

        public void SendAll(string message)
        {
            foreach (ulong player in Program.players.Keys)
            {
                Discord.WebSocket.DiscordSocketClient dsg = new Discord.WebSocket.DiscordSocketClient();
                Discord.UserExtensions.SendMessageAsync(dsg.GetUser(player), message);

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

        void AutoKick(Object source, ElapsedEventArgs e){
            UInt64.TryParse(Program.players.Cast<DictionaryEntry>().ElementAt(Program.currentPlayer).Key.ToString(), out ulong id);
            Program.players.Remove(id);
            ReplyAsync($"<@{id}>, you have been AFK removed.\n");
            if (Program.order == 1)
            {
                Program.currentPlayer++;
                if (Program.currentPlayer >= Program.players.Count)
                    Program.currentPlayer = Program.currentPlayer - Program.players.Count;
            }
            else
            {
                Program.currentPlayer--;
                if (Program.currentPlayer < 0)
                    Program.currentPlayer = Program.players.Count - Program.currentPlayer;
            }
            ResetTimer();
            UInt64.TryParse(Program.players.Cast<DictionaryEntry>().ElementAt(Program.currentPlayer).Key.ToString(), out ulong id2);
            Program.players.Remove(id);
            ReplyAsync($"It is now <@{id2}> turn.\n");
            if (Program.players.Count == 0)
            {
                Program.currentPlayer = 0;
                Program.gameStarted = false;
                Program.order = 1;
                Program.currentcard = null;
                Program.players = new System.Collections.Specialized.OrderedDictionary();
                ReplyAsync("Game has been reset, due to nobody in-game.");
                playTimer.Dispose();
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