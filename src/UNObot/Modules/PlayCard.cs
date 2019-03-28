using System.Threading.Tasks;
using System;
using System.Collections.Generic;

#pragma warning disable CS1701 // Assuming assembly reference matches identity
#pragma warning disable CS1702 // Assuming assembly reference matches identity

namespace UNObot.Modules
{
    class PlayCard
    {
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
            if (wild != null)
            {
                switch (wild.ToLower())
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
            foreach (Card card in await UNOdb.GetCards(player))
            {
                existing |= card.Equals(playCard);
            }
            if (!existing)
            {
                return "You do not have this card!";
            }
            Card currentCard = await UNOdb.GetCurrentCard(server);
            if (!(playCard.Color == currentCard.Color || playCard.Value == currentCard.Value || playCard.Color == "Wild"))
            {
                return "This is illegal you know. Your card must match in color/value, or be a wild card.";
            }
            string Response = "";
            string UsernamePlayer = Program._client.GetUser(player).Username;
            Response += $"{UsernamePlayer} has placed an {playCard.ToString()}.\n";
            if (await UNOdb.GetUNOPlayer(server) != 0)
            {
                Response += $"<@{await UNOdb.GetUNOPlayer(server)}> has forgotten to say UNO! They have been given 2 cards.\n";
                await UNOdb.AddCard(await UNOdb.GetUNOPlayer(server), UNOcore.RandomCard());
                await UNOdb.AddCard(await UNOdb.GetUNOPlayer(server), UNOcore.RandomCard());
                await UNOdb.SetUNOPlayer(server, 0);
            }
            await UNOdb.RemoveCard(player, playCard);
            await UNOdb.SetCurrentCard(server, playCard);
            //time to check if someone won or set uno player
            List<Card> checkCards = await UNOdb.GetCards(player);
            if (checkCards.Count == 0)
            {
                //woah person won
                Response += $"<@{player}> has won!\n";
                await UNOdb.UpdateStats(player, 3);

                string response = "";
                foreach (ulong getplayer in await UNOdb.GetPlayers(server))
                {
                    List<Card> loserlist = await UNOdb.GetCards(getplayer);
                    response += $"- <@{getplayer}> had {loserlist.Count} cards left.\n";
                    await UNOdb.UpdateStats(getplayer, 2);
                    await UNOdb.RemoveUser(getplayer);
                }
                Response += response;
                await UNOdb.ResetGame(server);
                Response += "Game is over. You may rejoin now.";
                return Response;
            }
            //keeps on going if nobody won
            await QueueHandler.NextPlayer(server);
            if (playCard.Color == "Wild")
            {
                if (playCard.Value == "+4")
                {
                    Response += $"<@{await QueueHandler.GetCurrentPlayer(server)}> has recieved four cards from the action.\n";
                    for (int i = 0; i < 4; i++)
                        await UNOdb.AddCard(await QueueHandler.GetCurrentPlayer(server), UNOcore.RandomCard());
                }
                Response += $"Due to the wild card, the current card is now {wild}.\n";
                Card newCard = new Card
                {
                    Color = wild,
                    Value = "Any"
                };
                await UNOdb.SetCurrentCard(server, newCard);
            }

            switch (playCard.Value)
            {
                case "+2":
                    Response += $"<@{await QueueHandler.GetCurrentPlayer(server)}> has recieved two cards from the action.\n";
                    await UNOdb.AddCard(await QueueHandler.GetCurrentPlayer(server), UNOcore.RandomCard());
                    await UNOdb.AddCard(await QueueHandler.GetCurrentPlayer(server), UNOcore.RandomCard());
                    break;
                case "Skip":
                    Response += $"<@{await QueueHandler.GetCurrentPlayer(server)}> has been skipped!\n";
                    await QueueHandler.NextPlayer(server);
                    break;
                case "Reverse":
                    Response += "The order has been reversed!\n";
                    await QueueHandler.ReversePlayers(server);
                    if (await QueueHandler.PlayerCount(server) != 2)
                        await QueueHandler.NextPlayer(server);
                    break;
                default:
                    _ = 1;
                    break;
            }
            checkCards = await UNOdb.GetCards(player);
            if (checkCards.Count == 1)
                await UNOdb.SetUNOPlayer(server, player);

            await UNOdb.UpdateDescription(server, Response);
            Response += $"It is now <@{await QueueHandler.GetCurrentPlayer(server)}>'s turn.";
            return Response;
        }
    }
}