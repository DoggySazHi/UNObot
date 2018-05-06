using System.Threading.Tasks;
using System;
using Discord;
using Discord.Commands;
using System.Collections.Generic;

namespace UNObot.Modules
{
    class PlayCard : ModuleBase<SocketCommandContext>
    {
        UNOdb db = new UNOdb();
        QueueHandler queueHandler = new QueueHandler();
        public async Task Play(string color, string value, string wild, ulong player, ulong server)
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
                    await ReplyAsync($"<@{player}>, that's not a color.");
                    return;
            }
            switch (value.ToLower())
            {
                case "reverse":
                    value = "Reverse";
                    break;
                case "color":
                    value = "Color";
                    break;
                case "+4":
                    value = "+4";
                    break;
                default:
                    await ReplyAsync($"<@{player}>, that's not a value.");
                    return;
            }
            if(wild != null)
            {
                switch(wild)
                {
                    case "red":
                        wild = "Red";
                        break;
                    case "blue":
                        wild = "Blue";
                        break;
                    case "green":
                        wild = "Green";
                        break;
                    case "yellow":
                        wild = "Yellow";
                        break;
                    case "wild":
                        wild = "Wild";
                        break;
                    default:
                        await ReplyAsync($"<@{player}>, that's not a color for your wild card!");
                        return;
                }
            }
            //Great, filtering active.
            if(await db.GetUNOPlayer(server) != 0)
            {
                await ReplyAsync($"<@{await db.GetUNOPlayer(server)}> has forgotten to say UNO! They have been given 2 cards.");
                await db.AddCard(await db.GetUNOPlayer(server), UNOcore.RandomCard());
                await db.AddCard(await db.GetUNOPlayer(server), UNOcore.RandomCard());
                await db.SetUNOPlayer(server, 0);
            }
            Card playCard = new Card
            {
                Color = color,
                Value = value
            };
            bool existing = false;
            foreach(Card card in await db.GetCards(player))
            {
                existing |= card.Equals(playCard);
            }
            if(!existing)
            {
                await ReplyAsync("You do not have this card!");
                return;
            }
            Card currentCard = await db.GetCurrentCard(server);
            if(!(playCard.Color == currentCard.Color || playCard.Value == currentCard.Value || playCard.Color == "Wild"))
            {
                await ReplyAsync("This is illegal you know. Your card must match in color/value, or be a wild card.");
                return;
            }
            await ReplyAsync($"<@{player}> has placed an {playCard.ToString()}.");
            await db.RemoveCard(player, playCard);
            await db.SetCurrentCard(server, playCard);
            //time to check if someone won or set uno player
            List<Card> checkCards = await db.GetCards(player);
            if(checkCards.Count == 0)
            {
                //woah person won
                await ReplyAsync($"<@{Context.User.Id}> has won!");
                await db.UpdateStats(Context.User.Id, 3);
                
                string response = "";
                foreach(ulong getplayer in await db.GetPlayers(server))
                {
                    List<Card> loserlist = await db.GetCards(getplayer);
                    response += $"- <@{player}> had {loserlist.Count} cards left.\n";
                    await db.UpdateStats(player, 2);
                }
                await ReplyAsync(response);
                await db.ResetGame(server);
                await ReplyAsync("Game is over. You may rejoin now.");
                return;
            } else if (checkCards.Count == 1)
                await db.SetUNOPlayer(server, player);
            //keeps on going if nobody won
            await queueHandler.NextPlayer(server);
            if(playCard.Color == "Wild")
            {
                if(playCard.Value == "+4")
                {
                    await ReplyAsync($"<@{await queueHandler.GetCurrentPlayer(server)}> has recieved four cards from the action.");
                    // no 4 loop 4 u
                    await db.AddCard(await queueHandler.GetCurrentPlayer(server), UNOcore.RandomCard());
                    await db.AddCard(await queueHandler.GetCurrentPlayer(server), UNOcore.RandomCard());
                    await db.AddCard(await queueHandler.GetCurrentPlayer(server), UNOcore.RandomCard());
                    await db.AddCard(await queueHandler.GetCurrentPlayer(server), UNOcore.RandomCard());
                }
                await ReplyAsync($"Due to the wild card, the current card is now {wild}.");
                Card newCard = new Card
                {
                    Color = wild,
                    Value = "Any"
                };
                await db.SetCurrentCard(server, newCard);
            }
            if(playCard.Value == "+2")
            {
                await ReplyAsync($"<@{await queueHandler.GetCurrentPlayer(server)}> has recieved two cards from the action.");
                await db.AddCard(await queueHandler.GetCurrentPlayer(server), UNOcore.RandomCard());
                await db.AddCard(await queueHandler.GetCurrentPlayer(server), UNOcore.RandomCard());
            } else if (playCard.Value == "Skip")
            {
                await ReplyAsync($"<@{await queueHandler.GetCurrentPlayer(server)}> has been skipped! Feelsbadm8.");
                await queueHandler.NextPlayer(server);
            } else if (playCard.Value == "Reverse")
            {
                await ReplyAsync("The order has been reversed!");
                await queueHandler.ReversePlayers(server);
                if(await queueHandler.PlayerCount(server) != 2)
                    await queueHandler.NextPlayer(server);
            }
            await ReplyAsync($"It is now <@{queueHandler.GetCurrentPlayer(server)}>'s turn.");
        }

        public static async Task ReplyAsync(string reply)
            => await ReplyAsync(reply);
    }
}