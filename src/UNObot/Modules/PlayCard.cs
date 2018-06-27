using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace UNObot.Modules
{
    class PlayCard
    {
        UNOdb db = new UNOdb();
        QueueHandler queueHandler = new QueueHandler();
        public async Task<string> Play(string color, string value, string wild, ulong player, ulong server)
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
                    return $"<@{player}>, that's not a color.";
            }
            if (!Int32.TryParse(value, out int output) || value == "+4" || value == "+2")
            {
                switch (value.ToLower())
                {
                    case "reverse":
                        value = "Reverse";
                        break;
                    case "skip":
                        value = "Skip";
                        break;
                    case "color":
                        value = "Color";
                        break;
                    case "+4":
                        value = "+4";
                        break;
                    case "+2":
                        value = "+2";
                        break;
                    default:
                        return $"<@{player}>, that's not a value.";
                }
            }
            else
                value = output.ToString();
            if(wild != null)
            {
                switch(wild.ToLower())
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
                        return $"<@{player}>, that's not a color for your wild card!";
                }
            }
            //Great, filtering active.
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
                return "You do not have this card!";
            }
            Card currentCard = await db.GetCurrentCard(server);
            if(!(playCard.Color == currentCard.Color || playCard.Value == currentCard.Value || playCard.Color == "Wild"))
            {
                return "This is illegal you know. Your card must match in color/value, or be a wild card.";
            }
            string Response = "";
            Response += $"<@{player}> has placed an {playCard.ToString()}.\n";
            if (await db.GetUNOPlayer(server) != 0)
            {
                Response += $"<@{await db.GetUNOPlayer(server)}> has forgotten to say UNO! They have been given 2 cards.\n";
                await db.AddCard(await db.GetUNOPlayer(server), UNOcore.RandomCard());
                await db.AddCard(await db.GetUNOPlayer(server), UNOcore.RandomCard());
                await db.SetUNOPlayer(server, 0);
            }
            await db.RemoveCard(player, playCard);
            await db.SetCurrentCard(server, playCard);
            //time to check if someone won or set uno player
            List<Card> checkCards = await db.GetCards(player);
            if(checkCards.Count == 0)
            {
                //woah person won
                Response += $"<@{player}> has won!\n";
                await db.UpdateStats(player, 3);
                
                string response = "";
                foreach(ulong getplayer in await db.GetPlayers(server))
                {
                    List<Card> loserlist = await db.GetCards(getplayer);
                    response += $"- <@{getplayer}> had {loserlist.Count} cards left.\n";
                    await db.UpdateStats(getplayer, 2);
                    await db.RemoveUser(getplayer);
                }
                Response += response;
                await db.ResetGame(server);
                Response += "Game is over. You may rejoin now.";
                return Response;
            } else if (checkCards.Count == 1)
                await db.SetUNOPlayer(server, player);
            //keeps on going if nobody won
            await queueHandler.NextPlayer(server);
            if(playCard.Color == "Wild")
            {
                if(playCard.Value == "+4")
                {
                    Response += $"<@{await queueHandler.GetCurrentPlayer(server)}> has recieved four cards from the action.\n";
                    for(int i = 0; i < 4; i++)
                        await db.AddCard(await queueHandler.GetCurrentPlayer(server), UNOcore.RandomCard());
                }
                Response += $"Due to the wild card, the current card is now {wild}.\n";
                Card newCard = new Card
                {
                    Color = wild,
                    Value = "Any"
                };
                await db.SetCurrentCard(server, newCard);
            }

            switch (playCard.Value)
            {
                case "+2":
                    Response += $"<@{await queueHandler.GetCurrentPlayer(server)}> has recieved two cards from the action.\n";
                    await db.AddCard(await queueHandler.GetCurrentPlayer(server), UNOcore.RandomCard());
                    await db.AddCard(await queueHandler.GetCurrentPlayer(server), UNOcore.RandomCard());
                    break;
                case "Skip":
                    Response += $"<@{await queueHandler.GetCurrentPlayer(server)}> has been skipped!\n";
                    await queueHandler.NextPlayer(server);
                    break;
                case "Reverse":
                    Response += "The order has been reversed!\n";
                    await queueHandler.ReversePlayers(server);
                    if (await queueHandler.PlayerCount(server) != 2)
                        await queueHandler.NextPlayer(server);
                    break;
            }

            Response += $"It is now <@{await queueHandler.GetCurrentPlayer(server)}>'s turn.";
            return Response;
        }
    }
}