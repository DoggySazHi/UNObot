﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using System.Linq;
using System.Collections;
using System.Timers;
using Discord;

namespace UNObot.Modules
{

    public class CoreCommands : ModuleBase<SocketCommandContext>
    {
        UNObot.Modules.UNOdb db = new UNObot.Modules.UNOdb();

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
                    Queue<ulong> players = await db.GetPlayers();
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



        async Task NextPlayer()
        {
            Queue<ulong> players = await db.GetPlayers();
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
            Queue<ulong> players = await db.GetPlayers();
            if (Program.order == 1 && Program.currentPlayer >= players.Count)
                    Program.currentPlayer = 0;
            else if (Program.currentPlayer < 0)
                Program.currentPlayer = players.Count - 1;
        }
        async void AutoKick(Object source, ElapsedEventArgs e){
            Queue<ulong> players = await db.GetPlayers();
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
    }
}