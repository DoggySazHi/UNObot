using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Services;

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
        public static async Task<Embed> DisplayGame(ulong serverid)
        {
            uint cardColor = 0xFF0000;
            var card = await UNOdb.GetCurrentCard(serverid);

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
            ushort isPrivate = await UNOdb.GetGamemode(serverid);
            string server = Program._client.GetGuild(serverid).Name;
            foreach (ulong id in await UNOdb.GetPlayers(serverid))
            {
                var user = Program._client.GetUser(id);
                var cardCount = (await UNOdb.GetCards(id)).Count();
                if (isPrivate != 2)
                {
                    if (id == (await UNOdb.GetPlayers(serverid)).Peek())
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
                    if (id == (await UNOdb.GetPlayers(serverid)).Peek())
                        response += $"**{user.Username}** - ??? cards\n";
                    else
                        response += $"{user.Username} - ??? cards\n";
                }
            }
            var builder = new EmbedBuilder()
            .WithTitle("Current Game")
            .WithDescription(await UNOdb.GetDescription(serverid))
            .WithColor(new Color(cardColor))
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter(footer =>
            {
                footer
                    .WithText($"UNObot {Program.version} - By DoggySazHi")
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

        public static async Task<Embed> DisplayCards(ulong userid, ulong serverid)
        {
            string server = Program._client.GetGuild(serverid).Name;
            var currentCard = await UNOdb.GetCurrentCard(serverid);
            var cards = await UNOdb.GetCards(userid);
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

            Random r = ThreadSafeRandom.ThisThreadsRandom;

            var builder = new EmbedBuilder()
            .WithTitle("Cards in Hand")
                .WithDescription($"Current Card: {currentCard}")
            .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter(footer =>
            {
                footer
                    .WithText($"UNObot {Program.version} - By DoggySazHi")
                    .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
            })
                .WithThumbnailUrl(ImageHandler.GetImage(currentCard))
            .WithAuthor(author =>
            {
                author
                    .WithName($"Playing in {server}")
                    .WithIconUrl("https://williamle.com/unobot/unobot.png");
            })
            .AddField("Red Cards", RedCards, true)
            .AddField("Green Cards", GreenCards, true)
            .AddField("Blue Cards", BlueCards, true)
            .AddField("Yellow Cards", YellowCards, true)
            .AddField("Wild Cards", WildCards, true);
            var embed = builder.Build();
            return embed;
        }

        public static async Task<Tuple<Embed, Tuple<string, string, string>>> DisplayAddSong(ulong UserID, ulong ServerID, string SongURL)
        {
            var Information = await YoutubeService.GetSingleton().GetInfo(SongURL);
            string Server = Program._client.GetGuild(ServerID).Name;
            string Username = Program._client.GetUser(UserID).Username;
            Random r = ThreadSafeRandom.ThisThreadsRandom;

            var builder = new EmbedBuilder()
                .WithTitle(Information.Item1)
                .WithUrl(SongURL)
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithThumbnailUrl(Information.Item3)
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Added in {Server}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Duration", Information.Item2)
                .AddField("Requested By", Username);
            var embed = builder.Build();
            return new Tuple<Embed, Tuple<string, string, string>>(embed, Information);
        }

        public static Embed DisplayNowPlaying(Song Song, string CurrentDuration)
        {
            string Username = Program._client.GetUser(Song.RequestedBy).Username;
            string Servername = Program._client.GetGuild(Song.RequestedGuild).Name;
            Random r = ThreadSafeRandom.ThisThreadsRandom;

            var builder = new EmbedBuilder()
                .WithTitle(Song.Name)
                .WithUrl(Song.URL)
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithThumbnailUrl(Song.ThumbnailURL)
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Playing in {Servername}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Duration", $"{(string.IsNullOrEmpty(CurrentDuration) ? "" : $"{CurrentDuration}/")}{Song.Duration}")
                .AddField("Requested By", Username);
            return builder.Build();
        }

        public static Embed DisplaySongList(Song NowPlaying, List<Song> Songs)
        {
            Random r = ThreadSafeRandom.ThisThreadsRandom;
            string Server = Program._client.GetGuild(NowPlaying.RequestedGuild).Name;
            StringBuilder List = new StringBuilder();

            if (Songs.Count == 0)
                List.Append("There are no songs queued.");
            else
                foreach (Song s in Songs)
                {
                    string Username = Program._client.GetUser(s.RequestedBy).Username;
                    List.Append($"[{s.Name}]({s.URL}) - {s.Duration} - Added by {Username}");
                    List.Append("\n");
                }

            //TODO Make Description match others.
            var builder = new EmbedBuilder()
            .WithTitle("Now Playing")
            .WithDescription($"[{NowPlaying.Name}]({NowPlaying.URL})")
            .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter(footer =>
            {
                footer
                    .WithText($"UNObot {Program.version} - By DoggySazHi")
                    .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
            })
            .WithAuthor(author =>
            {
                author
                    .WithName($"Playing in {Server}")
                    .WithIconUrl("https://williamle.com/unobot/unobot.png");
            })
            .AddField("Queued", List.ToString());
            return builder.Build();
        }
    }
}
