using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

#pragma warning disable CS1701 // Assuming assembly reference matches identity
#pragma warning disable CS1702 // Assuming assembly reference matches identity

namespace UNObot.Modules
{
    public static class ImageHandler
    {
        public static string GetImage(Card c)
        {
            return $"https://williamle.com/unobot/{c.Color}_{c.Value}.png";
        }
    }
    public static class DisplayEmbed
    {
        readonly static UNOdb db = new UNOdb();
        public static async Task<Embed> DisplayGame(ulong serverid)
        {
            uint cardColor = 0xFF0000;
            var card = await db.GetCurrentCard(serverid);

            switch (card.Color)
            {
                case "Red":
                    cardColor = 0xFF0000;
                    break;
                case "Blue":
                    cardColor = 0x0000FF;
                    break;
                case "Yellow":
                    cardColor = 0xFFFF00;
                    break;
                default:
                    cardColor = 0x00FF00;
                    break;
            }
            string response = "";
            ushort isPrivate = await db.GetGamemode(serverid);
            string server = Program._client.GetGuild(serverid).Name;
            foreach (ulong id in await db.GetPlayers(serverid))
            {
                var user = Program._client.GetUser(id);
                var cardCount = (await db.GetCards(id)).Count();
                if (isPrivate != 2)
                {
                    if (id == (await db.GetPlayers(serverid)).Peek())
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
                    if (id == (await db.GetPlayers(serverid)).Peek())
                        response += $"**{user.Username}** - ??? cards\n";
                    else
                        response += $"{user.Username} - ??? cards\n";
                }
            }
            var builder = new EmbedBuilder()
            .WithTitle("Current Game")
            .WithDescription(await db.GetDescription(serverid))
            .WithColor(new Color(cardColor))
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter(footer =>
            {
                footer
                    .WithText($"UNObot {Program.version} - By DoggySazHi")
                    .WithIconUrl("https://cdn.discordapp.com/avatars/191397590946807809/efaab2638e2f463f09881d4233ec84c9.png");
            })
            .WithThumbnailUrl(ImageHandler.GetImage(card))
            .WithAuthor(author =>
            {
                author
                    .WithName($"Playing in {server}")
                    .WithIconUrl("https://cdn.discordapp.com/avatars/477616287997231105/02408a548a232053f61694fa86c91a12.png");
            })
            .AddField("Current Players", response, true)
            .AddField("Current Card", card, true);
            var embed = builder.Build();
            return embed;
            //Remember to ReplyAsync("It is now <@832983482589>'s turn.", Embed);
        }

        public static async Task<Embed> DisplayCards(ulong userid, ulong serverid)
        {
            string server = Program._client.GetGuild(serverid).Name;
            var currentCard = await db.GetCurrentCard(serverid);
            var cards = await db.GetCards(userid);
            cards = cards.OrderBy(o => o.Color).ThenBy(o => o.Value).ToList();

            string RedCards = "";
            string GreenCards = "";
            string BlueCards = "";
            string YellowCards = "";
            string WildCards = "";

            string temp = cards[0].Color;
            foreach (Card c in cards)
            {
                switch (c.Color)
                {
                    case "Red":
                        if (c.Color == currentCard.Color || c.Value == currentCard.Value)
                            RedCards += $"**{c}**\n";
                        else
                            RedCards += $"{c}\n";
                        break;
                    case "Green":
                        if (c.Color == currentCard.Color || c.Value == currentCard.Value)
                            GreenCards += $"**{c}**\n";
                        else
                            GreenCards += $"{c}\n";
                        break;
                    case "Blue":
                        if (c.Color == currentCard.Color || c.Value == currentCard.Value)
                            BlueCards += $"**{c}**\n";
                        else
                            BlueCards += $"{c}\n";
                        break;
                    case "Yellow":
                        if (c.Color == currentCard.Color || c.Value == currentCard.Value)
                            YellowCards += $"**{c}**\n";
                        else
                            YellowCards += $"{c}\n";
                        break;
                    default:
                        WildCards += $"{c}\n";
                        break;
                }
            }

            RedCards += RedCards == "" ? "There are no cards available." : "";
            GreenCards += GreenCards == "" ? "There are no cards available." : "";
            BlueCards += BlueCards == "" ? "There are no cards available." : "";
            YellowCards += YellowCards == "" ? "There are no cards available." : "";
            WildCards += WildCards == "" ? "There are no cards available." : "";

            var builder = new EmbedBuilder()
            .WithTitle("Cards in Hand")
                .WithDescription($"Current Card: {currentCard}")
            .WithColor(new Color(UNOcore.r.Next(0, 256), UNOcore.r.Next(0, 256), UNOcore.r.Next(0, 256)))
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter(footer =>
            {
                footer
                    .WithText($"UNObot {Program.version} - By DoggySazHi")
                    .WithIconUrl("https://cdn.discordapp.com/avatars/191397590946807809/efaab2638e2f463f09881d4233ec84c9.png");
            })
                .WithThumbnailUrl(ImageHandler.GetImage(currentCard))
            .WithAuthor(author =>
            {
                author
                    .WithName($"Playing in {server}")
                    .WithIconUrl("https://cdn.discordapp.com/avatars/477616287997231105/02408a548a232053f61694fa86c91a12.png");
            })
            .AddField("Red Cards", RedCards, true)
            .AddField("Green Cards", GreenCards, true)
            .AddField("Blue Cards", BlueCards, true)
            .AddField("Yellow Cards", YellowCards, true)
            .AddField("Wild Cards", WildCards, true);
            var embed = builder.Build();
            return embed;
        }
    }
}
