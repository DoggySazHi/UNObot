using System.Threading.Tasks;
using Discord.Commands;
using UNObot.Core.UNOCore;

namespace UNObot.Core.Services
{
    public class UNOPlayCardService
    {
        private readonly DatabaseService _db;
        private readonly QueueHandlerService _queue;
        
        public UNOPlayCardService(DatabaseService db, QueueHandlerService queue)
        {
            _db = db;
            _queue = queue;
        }
        
        public async Task<string> Play(string color, string value, string wild, SocketCommandContext context)
        {
            var player = context.User.Id;
            var server = context.Guild.Id;
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
            var playCard = new Card(color, value);
            var existing = false;
            foreach (var card in await _db.GetCards(player)) existing |= card.Equals(playCard);
            if (!existing) return "You do not have this card!";
            var currentCard = await _db.GetCurrentCard(server);
            if (!(playCard.Color == currentCard.Color || playCard.Value == currentCard.Value ||
                  playCard.Color == "Wild"))
                return "This is illegal you know. Your card must match in color/value, or be a wild card.";
            var response = "";
            var usernamePlayer = context.User.Username;
            response += $"{usernamePlayer} has placed an {playCard}.\n";
            
            var unoPlayer = await _db.GetUNOPlayer(server);
            if (unoPlayer != 0)
            {
                response +=
                    $"<@{unoPlayer}> forgot to say UNO! They have been given 2 cards.\n";
                await _db.AddCard(unoPlayer, Card.RandomCard(2));
                await _db.SetUNOPlayer(server, 0);
            }

            await _db.RemoveCard(player, playCard);
            await _db.SetCurrentCard(server, playCard);
            //time to check if someone won or set uno player
            var checkCards = await _db.GetCards(player);
            if (checkCards.Count == 0)
            {
                //wow, person won
                response += $"<@{player}> has won!\n";
                await _db.UpdateStats(player, 3);

                var winResponse = "";
                foreach (var getPlayer in await _db.GetPlayers(server))
                {
                    var loserList = await _db.GetCards(getPlayer);
                    winResponse += $"- <@{getPlayer}> had {loserList.Count} cards left.\n";
                    await _db.UpdateStats(getPlayer, 2);
                    await _db.RemoveUser(getPlayer);
                }

                response += winResponse;
                await _db.ResetGame(server);
                response += "Game is over. You may rejoin now.";
                return response;
            }

            //keeps on going if nobody won
            await _queue.NextPlayer(server);
            if (playCard.Color == "Wild")
            {
                if (playCard.Value == "+4")
                {
                    response +=
                        $"<@{await _queue.GetCurrentPlayer(server)}> has received four cards from the action.\n";
                    await _db.AddCard(await _queue.GetCurrentPlayer(server), 
                        Card.RandomCard(4));
                }

                response += $"Due to the wild card, the current card is now {wild}.\n";
                var newCard = new Card(wild, "Any");
                await _db.SetCurrentCard(server, newCard);
            }

            switch (playCard.Value)
            {
                case "+2":
                    response +=
                        $"<@{await _queue.GetCurrentPlayer(server)}> has received two cards from the action.\n";
                    await _db.AddCard(await _queue.GetCurrentPlayer(server),
                        Card.RandomCard(2));
                    break;
                case "Skip":
                    response += $"<@{await _queue.GetCurrentPlayer(server)}> has been skipped!\n";
                    await _queue.NextPlayer(server);
                    break;
                case "Reverse":
                    response += "The order has been reversed!\n";
                    await _queue.ReversePlayers(server);
                    if (await _queue.PlayerCount(server) != 2)
                        await _queue.NextPlayer(server);
                    break;
            }

            checkCards = await _db.GetCards(player);
            if (checkCards.Count == 1)
                await _db.SetUNOPlayer(server, player);

            await _db.UpdateDescription(server, response);
            response += $"It is now <@{await _queue.GetCurrentPlayer(server)}>'s turn.";
            return response;
        }
    }
}