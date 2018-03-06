using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using System.Linq;
using System.Collections;

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
        [Command("info")]
        public Task Info()
            => ReplyAsync(
                $"{Context.Client.CurrentUser.Username} - Created by DoggySazHi\n");

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
            if (Program.players.Contains(Context.User.Id))
            {
                return ReplyAsync($"<@{Context.User.Id}>, you're already in the queue!\n");
            }
            if (Program.gameStarted)
            {
                return ReplyAsync($"The game has already started, so you cannot join.\n");
            }
            Program.players.Add(Context.User.Id, new List<Card>());
            return ReplyAsync($"<@{Context.User.Id}>, you have been added to the queue.\n");
        }
        [Command("leave")]
        public Task Leave()
        {
            if (Program.players.Contains(Context.User.Id))
            {
                Program.players.Remove(Context.User.Id);
                ReplyAsync($"<@{Context.User.Id}>, you have been removed from the queue.\n");
                if (Program.order == 1)
                {
                    Program.currentPlayer++;
                    if (Program.currentPlayer == Program.players.Count)
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
                    Program.players = new System.Collections.Specialized.OrderedDictionary();
                    ReplyAsync("Game has been reset, due to nobody in-game.");
                }
                return null;
            }
            else
            {
                return ReplyAsync($"<@{Context.User.Id}>, you are already out of the queue!\n");
            }
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
            if (Program.players.Contains(Context.User.Id))
            {
                if (Program.gameStarted)
                {
                    ReplyAsync("Current card: " + Program.currentcard.Color + " " + Program.currentcard.Value);
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
                    UInt64.TryParse(Program.players.Cast<DictionaryEntry>().ElementAt(0).Key.ToString(), out ulong result);
                    ReplyAsync("Game has started. Please remember; PM the bot to avoid bleeding!\n" +
                               "You have been given 7 cards; PM \"deck\" to view them. " +
                               $"The first player is <@{result}>.)");
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
                        default:
                            ReplyAsync($"<@{Context.User.Id}>, that's not a color.");
                            return;
                    }
                    bool shouldwork = UInt64.TryParse(Program.players.Cast<DictionaryEntry>().ElementAt(Program.currentPlayer).Key.ToString(), out ulong result);
                    if (!shouldwork)
                    {
                        ReplyAsync($"<@{Context.User.Id}>, you apparently don't exist.");
                        return;
                    }
                    if (result == Context.User.Id)
                    {
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
                                    if (Program.currentPlayer == Program.players.Count)
                                        Program.currentPlayer = Program.currentPlayer - Program.players.Count;
                                    UInt64.TryParse(Program.players.Cast<DictionaryEntry>().ElementAt(Program.currentPlayer).Key.ToString(), out ulong skipped);
                                    SendAll($"<@{skipped}> has been skipped!");
                                    Program.currentPlayer++;
                                }
                                if (Program.currentPlayer == Program.players.Count)
                                    Program.currentPlayer = Program.currentPlayer - Program.players.Count;
                            }
                            else
                            {
                                Program.currentPlayer--;
                                if (Program.currentcard.Value == "Skip")
                                {
                                    if (Program.currentPlayer == Program.players.Count)
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
                                    if (Program.currentPlayer == Program.players.Count)
                                        Program.currentPlayer = Program.currentPlayer - Program.players.Count;
                                }
                                else
                                {
                                    Program.currentPlayer--;
                                    if (Program.currentPlayer < 0)
                                        Program.currentPlayer = Program.players.Count - Program.currentPlayer;
                                }
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

        public void SendAll (string message)
        {
            foreach (ulong player in Program.players.Keys)
            {
                Discord.WebSocket.DiscordSocketClient dsg = new Discord.WebSocket.DiscordSocketClient();
                Discord.UserExtensions.SendMessageAsync(dsg.GetUser(player), message);
            }
        }
    }

    public static class UNOcore {
        static readonly Random r = new Random();

        public static Card RandomCard(){
            Card card = new Card();

            //0-9 is number, 10 is actioncard
            int myCard = r.Next(0, 11);
            // see switch
            int myColor = r.Next(1, 5);

            switch(myColor){
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

            if(myCard < 10){
                card.Value = myCard.ToString();
            } else {
                //4 is wild, 1-3 is action
                int action = r.Next(1, 5);
                switch(action){
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
