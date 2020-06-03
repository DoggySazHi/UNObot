using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;

namespace UNObot.Services
{
    class UNOPlayCardService
    {
        public async Task<string> Play(string color, string value, string wild, ulong player, ulong server)
        {
            switch (color.ToLower()[0])
            {
                case 'r':
                    color = "Red";
                    break;
                case 'b':
                    color = "Blue";
                    break;
                case 'g':
                    color = "Green";
                    break;
                case 'y':
                    color = "Yellow";
                    break;
                case 'w':
                    color = "Wild";
                    break;
                default:
                    return $"<@{player}>, that's not a color.";
            }
            // Since +2 and +4 are treated as positive numbers, .TryParse turns them into 2 and 4, respectively.
            if (!int.TryParse(value, out var output) || value == "+2" || value == "+4")
            {
                switch (value.ToLower())
                {
                    case "skip":
                        value = "Skip";
                        break;
                    case "reverse":
                        value = "Reverse";
                        break;
                    case "color":
                        value = "Color";
                        break;
                    case "+2":
                        value = "+2";
                        break;
                    case "+4":
                        value = "+4";
                        break;
                    default:
                        return $"<@{player}>, that's not a value.";
                }
            }
            else
                value = output.ToString();
            if (wild != null)
            {
                switch (wild.ToLower()[0])
                {
                    case 'r':
                        wild = "Red";
                        break;
                    case 'b':
                        wild = "Blue";
                        break;
                    case 'g':
                        wild = "Green";
                        break;
                    case 'y':
                        wild = "Yellow";
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
            foreach (var card in await UNODatabaseService.GetCards(player))
            {
                LoggerService.Log(LogSeverity.Debug, $"Compared {card} to the user's {playCard}");
                existing |= card.Equals(playCard);
            }
            if (!existing)
            {
                return "You do not have this card!";
            }
            Card currentCard = await UNODatabaseService.GetCurrentCard(server);
            if (!(playCard.Color == currentCard.Color || playCard.Value == currentCard.Value || playCard.Color == "Wild"))
            {
                return "This is illegal you know. Your card must match in color/value, or be a wild card.";
            }
            string Response = "";
            string UsernamePlayer = Program._client.GetUser(player).Username;
            Response += $"{UsernamePlayer} has placed an {playCard}.\n";
            if (await UNODatabaseService.GetUNOPlayer(server) != 0)
            {
                Response += $"<@{await UNODatabaseService.GetUNOPlayer(server)}> has forgot to say UNO! They have been given 2 cards.\n";
                await UNODatabaseService.AddCard(await UNODatabaseService.GetUNOPlayer(server), UNOCoreServices.RandomCard());
                await UNODatabaseService.AddCard(await UNODatabaseService.GetUNOPlayer(server), UNOCoreServices.RandomCard());
                await UNODatabaseService.SetUNOPlayer(server, 0);
            }
            await UNODatabaseService.RemoveCard(player, playCard);
            await UNODatabaseService.SetCurrentCard(server, playCard);
            //time to check if someone won or set uno player
            List<Card> checkCards = await UNODatabaseService.GetCards(player);
            if (checkCards.Count == 0)
            {
                //woah person won
                Response += $"<@{player}> has won!\n";
                await UNODatabaseService.UpdateStats(player, 3);

                string response = "";
                foreach (ulong getplayer in await UNODatabaseService.GetPlayers(server))
                {
                    List<Card> loserlist = await UNODatabaseService.GetCards(getplayer);
                    response += $"- <@{getplayer}> had {loserlist.Count} cards left.\n";
                    await UNODatabaseService.UpdateStats(getplayer, 2);
                    await UNODatabaseService.RemoveUser(getplayer);
                }
                Response += response;
                await UNODatabaseService.ResetGame(server);
                Response += "Game is over. You may rejoin now.";
                return Response;
            }
            //keeps on going if nobody won
            await QueueHandlerService.NextPlayer(server);
            if (playCard.Color == "Wild")
            {
                if (playCard.Value == "+4")
                {
                    Response += $"<@{await QueueHandlerService.GetCurrentPlayer(server)}> has recieved four cards from the action.\n";
                    for (int i = 0; i < 4; i++)
                        await UNODatabaseService.AddCard(await QueueHandlerService.GetCurrentPlayer(server), UNOCoreServices.RandomCard());
                }
                Response += $"Due to the wild card, the current card is now {wild}.\n";
                Card newCard = new Card
                {
                    Color = wild,
                    Value = "Any"
                };
                await UNODatabaseService.SetCurrentCard(server, newCard);
            }

            switch (playCard.Value)
            {
                case "+2":
                    Response += $"<@{await QueueHandlerService.GetCurrentPlayer(server)}> has recieved two cards from the action.\n";
                    await UNODatabaseService.AddCard(await QueueHandlerService.GetCurrentPlayer(server), UNOCoreServices.RandomCard());
                    await UNODatabaseService.AddCard(await QueueHandlerService.GetCurrentPlayer(server), UNOCoreServices.RandomCard());
                    break;
                case "Skip":
                    Response += $"<@{await QueueHandlerService.GetCurrentPlayer(server)}> has been skipped!\n";
                    await QueueHandlerService.NextPlayer(server);
                    break;
                case "Reverse":
                    Response += "The order has been reversed!\n";
                    await QueueHandlerService.ReversePlayers(server);
                    if (await QueueHandlerService.PlayerCount(server) != 2)
                        await QueueHandlerService.NextPlayer(server);
                    break;
            }
            checkCards = await UNODatabaseService.GetCards(player);
            if (checkCards.Count == 1)
                await UNODatabaseService.SetUNOPlayer(server, player);

            await UNODatabaseService.UpdateDescription(server, Response);
            Response += $"It is now <@{await QueueHandlerService.GetCurrentPlayer(server)}>'s turn.";
            return Response;
        }
    }
}