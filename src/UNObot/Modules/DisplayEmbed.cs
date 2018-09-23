using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

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
        public static async Task<Embed> DisplayGame(ulong serverid)
        {
            UNOdb db = new UNOdb();
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
                case "Green":
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
                if (id == (await db.GetPlayers(serverid)).Peek())
                    response += $"**{user.Username}** - {cardCount} card";
                else
                    response += $"{user.Username} - {cardCount} card";
                if (cardCount > 1)
                    response += "s\n";
                else
                    response += "\n";
            }
            var builder = new EmbedBuilder()
            .WithTitle("Current Game")
            .WithDescription("WIP")
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
        public static void DisplayCards(ulong userid)
        {

        }
    }
}
