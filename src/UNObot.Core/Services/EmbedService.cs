using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using UNObot.Core.UNOCore;
using UNObot.Plugins.TerminalCore;

namespace UNObot.Core.Services
{
    public static class ImageHandler
    {
        public static string GetImage(Card c)
        {
            return $"https://williamle.com/unobot/{c.Color}_{c.Value}.png";
        }
    }
    
    public class EmbedService
    {
        private readonly DatabaseService _db;
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _config;
        
        public EmbedService(DatabaseService db, DiscordSocketClient client, IConfiguration config)
        {
            _db = db;
            _client = client;
            _config = config;
        }
        
        public async Task<Embed> DisplayGame(ulong serverId)
        {
            var card = await _db.GetCurrentCard(serverId);

            uint cardColor = card.Color switch
            {
                "Red" => 0xFF0000,
                "Blue" => 0x0000FF,
                "Yellow" => 0xFFFF00,
                _ => 0x00FF00
            };
            var response = "";
            var gamemode = await _db.GetGameMode(serverId);
            var server = _client.GetGuild(serverId).Name;
            foreach (var id in await _db.GetPlayers(serverId))
            {
                var user = _client.GetUser(id);
                var cardCount = (await _db.GetCards(id)).Count;
                if (!gamemode.HasFlag(GameMode.Private))
                {
                    if (id == (await _db.GetPlayers(serverId)).Peek())
                        response += $"**{user.Username}** - {cardCount} card";
                    else
                        response += $"{user.Username} - {cardCount} card";
                    if (cardCount > 1)
                        response += "s\n";
                    else
                        response += "\n";
                }
                else
                {
                    if (id == (await _db.GetPlayers(serverId)).Peek())
                        response += $"**{user.Username}** - ??? cards\n";
                    else
                        response += $"{user.Username} - ??? cards\n";
                }
            }

            var builder = new EmbedBuilder()
                .WithTitle("Current Game")
                .WithDescription(await _db.GetDescription(serverId))
                .WithColor(new Color(cardColor))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {_config["version"]} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithThumbnailUrl(ImageHandler.GetImage(card))
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Playing in {server}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Current Players", response, true)
                .AddField("Current Card", card, true);
            var embed = builder.Build();
            return embed;
            //Remember to ReplyAsync("It is now <@832983482589>'s turn.", Embed);
        }

        public async Task<Embed> DisplayCards(ulong userid, ulong serverId)
        {
            var server = _client.GetGuild(serverId).Name;
            var currentCard = await _db.GetCurrentCard(serverId);
            var cards = await _db.GetCards(userid);
            cards = cards.OrderBy(o => o.Color).ThenBy(o => o.Value).ToList();

            var redCards = "";
            var greenCards = "";
            var blueCards = "";
            var yellowCards = "";
            var wildCards = "";

            foreach (var c in cards)
                switch (c.Color)
                {
                    case "Red":
                        if (c.Color == currentCard.Color || c.Value == currentCard.Value)
                            redCards += $"**{c}**\n";
                        else
                            redCards += $"{c}\n";
                        break;
                    case "Green":
                        if (c.Color == currentCard.Color || c.Value == currentCard.Value)
                            greenCards += $"**{c}**\n";
                        else
                            greenCards += $"{c}\n";
                        break;
                    case "Blue":
                        if (c.Color == currentCard.Color || c.Value == currentCard.Value)
                            blueCards += $"**{c}**\n";
                        else
                            blueCards += $"{c}\n";
                        break;
                    case "Yellow":
                        if (c.Color == currentCard.Color || c.Value == currentCard.Value)
                            yellowCards += $"**{c}**\n";
                        else
                            yellowCards += $"{c}\n";
                        break;
                    default:
                        wildCards += $"{c}\n";
                        break;
                }

            redCards += redCards == "" ? "There are no cards available." : "";
            greenCards += greenCards == "" ? "There are no cards available." : "";
            blueCards += blueCards == "" ? "There are no cards available." : "";
            yellowCards += yellowCards == "" ? "There are no cards available." : "";
            wildCards += wildCards == "" ? "There are no cards available." : "";

            var r = ThreadSafeRandom.ThisThreadsRandom;

            var builder = new EmbedBuilder()
                .WithTitle("Cards in Hand")
                .WithDescription($"Current Card: {currentCard}")
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {_config["version"]} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithThumbnailUrl(ImageHandler.GetImage(currentCard))
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Playing in {server}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Red Cards", redCards, true)
                .AddField("Green Cards", greenCards, true)
                .AddField("Blue Cards", blueCards, true)
                .AddField("Yellow Cards", yellowCards, true)
                .AddField("Wild Cards", wildCards, true);
            var embed = builder.Build();
            return embed;
        }
    }
}