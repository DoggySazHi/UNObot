using System.Threading.Tasks;

namespace UNObot.Services
{
    internal class UNOPlayCardService
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
            else
                value = output.ToString();

            if (wild != null)
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

            //Great, filtering active.
            var playCard = new Card
            {
                Color = color,
                Value = value
            };
            var existing = false;
            foreach (var card in await UNODatabaseService.GetCards(player)) existing |= card.Equals(playCard);
            if (!existing) return "You do not have this card!";
            var currentCard = await UNODatabaseService.GetCurrentCard(server);
            if (!(playCard.Color == currentCard.Color || playCard.Value == currentCard.Value ||
                  playCard.Color == "Wild"))
                return "This is illegal you know. Your card must match in color/value, or be a wild card.";
            var response = "";
            var usernamePlayer = Program.Client.GetUser(player).Username;
            response += $"{usernamePlayer} has placed an {playCard}.\n";
            if (await UNODatabaseService.GetUNOPlayer(server) != 0)
            {
                response +=
                    $"<@{await UNODatabaseService.GetUNOPlayer(server)}> forgot to say UNO! They have been given 2 cards.\n";
                var unoPlayer = await UNODatabaseService.GetUNOPlayer(server);
                await UNODatabaseService.AddCard(unoPlayer, UNOCoreServices.RandomCard(2));
                await UNODatabaseService.SetUNOPlayer(server, 0);
            }

            await UNODatabaseService.RemoveCard(player, playCard);
            await UNODatabaseService.SetCurrentCard(server, playCard);
            //time to check if someone won or set uno player
            var checkCards = await UNODatabaseService.GetCards(player);
            if (checkCards.Count == 0)
            {
                //woah person won
                response += $"<@{player}> has won!\n";
                await UNODatabaseService.UpdateStats(player, 3);

                var winResponse = "";
                foreach (var getplayer in await UNODatabaseService.GetPlayers(server))
                {
                    var loserlist = await UNODatabaseService.GetCards(getplayer);
                    winResponse += $"- <@{getplayer}> had {loserlist.Count} cards left.\n";
                    await UNODatabaseService.UpdateStats(getplayer, 2);
                    await UNODatabaseService.RemoveUser(getplayer);
                }

                response += winResponse;
                await UNODatabaseService.ResetGame(server);
                response += "Game is over. You may rejoin now.";
                return response;
            }

            //keeps on going if nobody won
            await QueueHandlerService.NextPlayer(server);
            if (playCard.Color == "Wild")
            {
                if (playCard.Value == "+4")
                {
                    response +=
                        $"<@{await QueueHandlerService.GetCurrentPlayer(server)}> has recieved four cards from the action.\n";
                    await UNODatabaseService.AddCard(await QueueHandlerService.GetCurrentPlayer(server), 
                        UNOCoreServices.RandomCard(4));
                }

                response += $"Due to the wild card, the current card is now {wild}.\n";
                var newCard = new Card
                {
                    Color = wild,
                    Value = "Any"
                };
                await UNODatabaseService.SetCurrentCard(server, newCard);
            }

            switch (playCard.Value)
            {
                case "+2":
                    response +=
                        $"<@{await QueueHandlerService.GetCurrentPlayer(server)}> has recieved two cards from the action.\n";
                    await UNODatabaseService.AddCard(await QueueHandlerService.GetCurrentPlayer(server),
                        UNOCoreServices.RandomCard(2));
                    break;
                case "Skip":
                    response += $"<@{await QueueHandlerService.GetCurrentPlayer(server)}> has been skipped!\n";
                    await QueueHandlerService.NextPlayer(server);
                    break;
                case "Reverse":
                    response += "The order has been reversed!\n";
                    await QueueHandlerService.ReversePlayers(server);
                    if (await QueueHandlerService.PlayerCount(server) != 2)
                        await QueueHandlerService.NextPlayer(server);
                    break;
            }

            checkCards = await UNODatabaseService.GetCards(player);
            if (checkCards.Count == 1)
                await UNODatabaseService.SetUNOPlayer(server, player);

            await UNODatabaseService.UpdateDescription(server, response);
            response += $"It is now <@{await QueueHandlerService.GetCurrentPlayer(server)}>'s turn.";
            return response;
        }
    }
}