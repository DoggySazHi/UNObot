using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json.Linq;
using YoutubeExplode.Playlists;
using static UNObot.Services.MinecraftProcessorService;
using static UNObot.Services.QueryHandlerService;

namespace UNObot.Services
{
    public static class ImageHandler
    {
        public static string GetImage(Card c)
        {
            return $"https://williamle.com/unobot/{c.Color}_{c.Value}.png";
        }
    }

    public static class EmbedDisplayService
    {
        private const int Attempts = 3;

        public static async Task<Embed> DisplayGame(ulong serverId)
        {
            var card = await UNODatabaseService.GetCurrentCard(serverId);

            uint cardColor = card.Color switch
            {
                "Red" => (uint) 0xFF0000,
                "Blue" => 0x0000FF,
                "Yellow" => 0xFFFF00,
                _ => 0x00FF00
            };
            var response = "";
            var gamemode = await UNODatabaseService.GetGameMode(serverId);
            var server = Program.Client.GetGuild(serverId).Name;
            foreach (var id in await UNODatabaseService.GetPlayers(serverId))
            {
                var user = Program.Client.GetUser(id);
                var cardCount = (await UNODatabaseService.GetCards(id)).Count();
                if (!gamemode.HasFlag(UNOCoreServices.GameMode.Private))
                {
                    if (id == (await UNODatabaseService.GetPlayers(serverId)).Peek())
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
                    if (id == (await UNODatabaseService.GetPlayers(serverId)).Peek())
                        response += $"**{user.Username}** - ??? cards\n";
                    else
                        response += $"{user.Username} - ??? cards\n";
                }
            }

            var builder = new EmbedBuilder()
                .WithTitle("Current Game")
                .WithDescription(await UNODatabaseService.GetDescription(serverId))
                .WithColor(new Color(cardColor))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
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

        public static async Task<Embed> DisplayCards(ulong userid, ulong serverId)
        {
            var server = Program.Client.GetGuild(serverId).Name;
            var currentCard = await UNODatabaseService.GetCurrentCard(serverId);
            var cards = await UNODatabaseService.GetCards(userid);
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
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
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

        public static Tuple<Embed, Tuple<string, string, string>> DisplayAddSong(ulong userId, ulong serverId,
            string songUrl, Tuple<string, string, string> information)
        {
            var server = Program.Client.GetGuild(serverId).Name;
            var username = Program.Client.GetUser(userId).Username;
            var r = ThreadSafeRandom.ThisThreadsRandom;

            var builder = new EmbedBuilder()
                .WithTitle(information.Item1)
                .WithUrl(songUrl)
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithThumbnailUrl(information.Item3)
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Added in {server}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Duration", information.Item2)
                .AddField("Requested By", username);
            var embed = builder.Build();
            return new Tuple<Embed, Tuple<string, string, string>>(embed, information);
        }

        public static Embed DisplayNowPlaying(Song song, string currentDuration)
        {
            var username = Program.Client.GetUser(song.RequestedBy).Username;
            var servername = Program.Client.GetGuild(song.RequestedGuild).Name;
            var r = ThreadSafeRandom.ThisThreadsRandom;

            var builder = new EmbedBuilder()
                .WithTitle(song.Name)
                .WithUrl(song.Url)
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithThumbnailUrl(song.ThumbnailUrl)
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Playing in {servername}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Duration",
                    $"{(string.IsNullOrEmpty(currentDuration) ? "" : $"{currentDuration}/")}{song.Duration}")
                .AddField("Requested By", username);
            return builder.Build();
        }

        public static async Task<Tuple<Embed, Playlist>> DisplayPlaylist(ulong userId, ulong serverId, string songUrl)
        {
            var username = Program.Client.GetUser(userId).Username;
            var servername = Program.Client.GetGuild(serverId).Name;
            var r = ThreadSafeRandom.ThisThreadsRandom;
            var playlist = await YoutubeService.GetSingleton().GetPlaylist(songUrl);
            var thumbnail = await YoutubeService.GetSingleton().GetPlaylistThumbnail(playlist.Id);

            var builder = new EmbedBuilder()
                .WithTitle(playlist.Title)
                .WithUrl(playlist.Url)
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithThumbnailUrl(thumbnail)
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Playing in {servername}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Description", $"{playlist.Description}")
                .AddField("Author", $"{playlist.Author}")
                .AddField("Requested By", username);
            return new Tuple<Embed, Playlist>(builder.Build(), playlist);
        }

        public static Tuple<Embed, int> DisplaySongList(Song nowPlaying, List<Song> songs, int page)
        {
            var containers = new List<StringBuilder>();
            var r = ThreadSafeRandom.ThisThreadsRandom;
            var server = Program.Client.GetGuild(nowPlaying.RequestedGuild).Name;

            var index = 0;

            if (songs.Count == 0)
                containers.Add(new StringBuilder("There are no songs queued."));

            while (index < songs.Count)
            {
                var list = new StringBuilder();

                for (var i = 0; i < 9; i++)
                {
                    if (index >= songs.Count)
                        break;
                    var s = songs[index++];
                    var username = Program.Client.GetUser(s.RequestedBy).Username;
                    var nextLine = $"``{index}.``[{s.Name}]({s.Url}) |``{s.Duration} Requested by: {username}``\n\n";
                    if (list.Length + nextLine.Length > 1024)
                    {
                        index--;
                        break;
                    }

                    list.Append(nextLine);
                }

                containers.Add(list);
            }

            if (page <= 0 || page > containers.Count)
                return new Tuple<Embed, int>(null, containers.Count);
            return new Tuple<Embed, int>(
                DisplaySongList(server, r, page, containers.Count, containers[page - 1], nowPlaying), containers.Count);
        }

        private static Embed DisplaySongList(string server, Random r, int page, int maxPages, StringBuilder list,
            Song nowPlaying)
        {
            var builder = new EmbedBuilder()
                .WithTitle("Now Playing")
                .WithDescription(
                    $"[{nowPlaying.Name}]({nowPlaying.Url}) |``{nowPlaying.Duration} Requested by: {Program.Client.GetUser(nowPlaying.RequestedBy).Username}``")
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Page {page}/{maxPages} | Playing in {server}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Queued", list.ToString());

            return builder.Build();
        }

        public static bool UnturnedQueryEmbed(string ip, ushort port, out Embed result, ServerAverages averages = null)
        {
            A2SInfo information = null;
            var informationGet = false;
            A2SPlayer players = null;
            var playersGet = false;
            A2SRules rules = null;
            var rulesGet = false;
            for (var i = 0; i < Attempts; i++)
            {
                if (!informationGet)
                    informationGet = GetInfo(ip, (ushort) (port + 1), out information);
                if (!playersGet)
                    playersGet = GetPlayers(ip, (ushort) (port + 1), out players);
                if (!rulesGet)
                    rulesGet = GetRules(ip, (ushort) (port + 1), out rules);
            }

            if (!informationGet || !playersGet || !rulesGet)
            {
                result = null;
                return false;
            }

            var random = ThreadSafeRandom.ThisThreadsRandom;
            var serverName = information.Name;
            var serverImageUrl = rules.Rules
                .FirstOrDefault(o => o.Name.Contains("Browser_Icon", StringComparison.OrdinalIgnoreCase)).Value;
            var serverDescription = "";
            var map = information.Map;
            var playersOnline = "";
            var vacEnabled = information.Vac == A2SInfo.VacFlags.Secured;
            var unturnedVersion = rules.Rules
                .FirstOrDefault(o => o.Name.Contains("unturned", StringComparison.OrdinalIgnoreCase)).Value;
            var rocketModVersion = "";
            var rocketModPlugins = "";

            serverImageUrl ??= "";

            unturnedVersion ??= $"Unknown Version ({information.Version}?)";

            var descriptionLines = Convert.ToInt32(rules.Rules
                .Find(o => o.Name.Contains("Browser_Desc_Full_Count", StringComparison.OrdinalIgnoreCase)).Value
                .Trim());
            for (var i = 0; i < descriptionLines; i++)
                serverDescription += rules.Rules.FirstOrDefault(o =>
                    o.Name.Contains($"Browser_Desc_Full_Line_{i}", StringComparison.OrdinalIgnoreCase)).Value;
            for (var i = 0; i < players.PlayerCount; i++)
            {
                playersOnline +=
                    $"{players.Players[i].Name} - {HumanReadable(players.Players[i].Duration)}";
                if (i != players.PlayerCount - 1)
                    playersOnline += "\n";
            }

            var rocketExists =
                rules.Rules.FindIndex(o => o.Name.Contains("rocket", StringComparison.OrdinalIgnoreCase));
            var rocketPluginsExists =
                rules.Rules.FindIndex(o => o.Name.Contains("rocketplugins", StringComparison.OrdinalIgnoreCase));
            if (rocketExists != -1)
                rocketModVersion = rules.Rules[rocketExists].Value;
            if (rocketPluginsExists != -1)
                rocketModPlugins = rules.Rules[rocketPluginsExists].Value;

            var builder = new EmbedBuilder()
                .WithTitle("Description")
                .WithDescription(serverDescription)
                .WithColor(new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithThumbnailUrl(serverImageUrl)
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Server Query of {serverName}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("IP", ip, true)
                .AddField("Port", port, true)
                .AddField("Map", map, true)
                .AddField("VAC Security", vacEnabled ? "Enabled" : "Disabled", true)
                .AddField("Versions",
                    $"Unturned: {unturnedVersion}{(rocketModVersion == "" ? "" : $"\nRocketMod: {rocketModVersion}")}",
                    true);
            if (!string.IsNullOrWhiteSpace(rocketModPlugins))
                builder.AddField("RocketMod Plugins", rocketModPlugins, true);
            builder.AddField($"Players: {information.Players}/{information.MaxPlayers}",
                string.IsNullOrWhiteSpace(playersOnline) ? "Nobody's online!" : playersOnline, true);
            if (averages != null)
                builder.AddField("Server Averages",
                    $"Last hour: {averages.AverageLastHour:N2} players\n" +
                    $"Last 24 hours: {averages.AverageLast24H:N2} players\n" +
                    $"Last week: {averages.AverageLastWeek:N2} players\n" +
                    $"Last month: {averages.AverageLastMonth:N2} players\n" +
                    $"Last year: {averages.AverageLastYear:N2} players\n", true);
            result = builder.Build();
            return true;
        }

        public static bool MinecraftQueryEmbed(string ip, ushort port, out Embed result)
        {
            //TODO with the new option to disable status, it might be that queries work but not simple statuses.
            var defaultStatus = new MCStatus(ip, port);
            var extendedGet = GetInfoMCNew(ip, port, out var extendedStatus);

            if (!defaultStatus.ServerUp && !extendedGet)
            {
                result = null;
                return false;
            }

            List<MCUser> mcUserInfo = null;
            if (OutsideServers.Contains(ip) && SpecialServers.ContainsKey(port))
            {
                var server = SpecialServers[port];
                mcUserInfo = GetMCUsers(server.Server, server.RCONPort, server.Password, out _);
            }

            var random = ThreadSafeRandom.ThisThreadsRandom;
            var serverDescription = defaultStatus.Motd;
            var playersOnline = defaultStatus.CurrentPlayers == "0" ? "" : "Unknown (server doesn't have query on!)";

            if (extendedStatus != null && extendedGet)
            {
                playersOnline = "";
                for (var i = 0; i < extendedStatus.Players.Length; i++)
                {
                    playersOnline += $"{extendedStatus.Players[i]}";
                    if (mcUserInfo != null)
                    {
                        var userInfo = mcUserInfo.Find(o => o.Username == extendedStatus.Players[i]);
                        if (userInfo != null)
                            playersOnline +=
                                $"\n- **Ouchies:** {userInfo.Ouchies} | **Health:** {userInfo.Health} | **Food:** {userInfo.Food}\n- **Experience:** {userInfo.Experience}";
                        else
                            playersOnline += "\n Unknown stats.";
                    }

                    if (i != extendedStatus.Players.Length - 1)
                        playersOnline += "\n";
                }
            }

            var builder = new EmbedBuilder()
                .WithTitle("MOTD")
                .WithDescription(serverDescription)
                .WithColor(new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Minecraft Server Query of {ip}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("IP", ip, true)
                .AddField("Port", port, true)
                .AddField("Version", $"{defaultStatus.Version}", true)
                .AddField("Ping", $"{defaultStatus.Delay} ms", true)
                .AddField($"Players: {defaultStatus.CurrentPlayers}/{defaultStatus.MaximumPlayers}",
                    string.IsNullOrWhiteSpace(playersOnline) ? "Nobody's online!" : playersOnline, true);
            result = builder.Build();
            return true;
        }

        public static bool OuchiesEmbed(string ip, ushort port, out Embed result)
        {
            var random = ThreadSafeRandom.ThisThreadsRandom;

            if (!OutsideServers.Contains(ip) || !SpecialServers.ContainsKey(port))
            {
                result = new EmbedBuilder()
                    .WithColor(new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256)))
                    .WithTimestamp(DateTimeOffset.Now)
                    .WithFooter(footer =>
                    {
                        footer
                            .WithText($"UNObot {Program.Version} - By DoggySazHi")
                            .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                    })
                    .WithAuthor(author =>
                    {
                        author
                            .WithName($"Ouchies of {ip}")
                            .WithIconUrl("https://williamle.com/unobot/unobot.png");
                    })
                    .AddField("Mukyu~", "Invalid port for checking ouchies!").Build();
                return true;
            }

            MinecraftStatus status = null;
            var extendedGet = false;
            for (var i = 0; i < Attempts; i++)
                if (!extendedGet)
                    extendedGet = GetInfoMCNew(ip, port, out status);

            if (!extendedGet || status == null)
            {
                result = null;
                return false;
            }

            var server = SpecialServers[port];
            var ouchies = GetMCUsers(server.Server, server.RCONPort, server.Password, out _);

            var playersOnline = "";

            foreach (var item in ouchies)
                playersOnline += $"{item.Username} - {item.Ouchies} Ouchies\n";
            // Doesn't seem to affect embeds. PlayersOnline = PlayersOnline.Substring(0, PlayersOnline.Length - 1);

            var builder = new EmbedBuilder()
                .WithTitle("MOTD")
                .WithDescription(status.Motd)
                .WithColor(new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Ouchies of {ip}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("IP", ip, true)
                .AddField("Port", port, true)
                .AddField("Version", $"{status.Version}", true)
                .AddField("Ouchies Listing", playersOnline, true);
            result = builder.Build();
            return true;
        }

        public static bool LocationsEmbed(string ip, ushort port, out Embed result)
        {
            var random = ThreadSafeRandom.ThisThreadsRandom;

            if (!OutsideServers.Contains(ip) || !SpecialServers.ContainsKey(port))
            {
                result = new EmbedBuilder()
                    .WithColor(new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256)))
                    .WithTimestamp(DateTimeOffset.Now)
                    .WithFooter(footer =>
                    {
                        footer
                            .WithText($"UNObot {Program.Version} - By DoggySazHi")
                            .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                    })
                    .WithAuthor(author =>
                    {
                        author
                            .WithName($"Player Locations of {ip}")
                            .WithIconUrl("https://williamle.com/unobot/unobot.png");
                    })
                    .AddField("Mukyu~", "Invalid port for checking ouchies!").Build();
                return true;
            }

            MinecraftStatus status = null;
            var extendedGet = false;
            for (var i = 0; i < Attempts; i++)
                if (!extendedGet)
                    extendedGet = GetInfoMCNew(ip, port, out status);

            if (!extendedGet || status == null)
            {
                result = null;
                return false;
            }

            var server = SpecialServers[port];
            var users = GetMCUsers(server.Server, server.RCONPort, server.Password, out _);

            var playersOnline = "";
            foreach (var user in users)
                if (user.Online)
                {
                    playersOnline +=
                        $"{user.Username} - **X:** {user.Coordinates[0]:N2} **Y:** {user.Coordinates[1]:N2} **Z:** {user.Coordinates[2]:N2} ";
                    if (user.Coordinates.Length == 4)
                        playersOnline += user.Coordinates[3] switch
                        {
                            1 => "**End**",
                            -1 => "**Nether**",
                            _ => "**Overworld**"
                        };
                    playersOnline += "\n";
                }

            if (playersOnline == "")
                playersOnline = "Nobody's online!";
            // Doesn't seem to affect embeds. PlayersOnline = PlayersOnline.Substring(0, PlayersOnline.Length - 1);

            var builder = new EmbedBuilder()
                .WithTitle("MOTD")
                .WithDescription(status.Motd)
                .WithColor(new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Player Locations of {ip}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("IP", ip, true)
                .AddField("Port", port, true)
                .AddField("Version", $"{status.Version}", true)
                .AddField("Players", playersOnline, true);
            result = builder.Build();
            return true;
        }

        public static bool TransferEmbed(string ip, ushort port, ulong source, string target, string amountIn,
            out Embed result)
        {
            var messageTitle = "Mukyu~";
            var message = "General error; IDK what happened, see UNObot logs.";

            try
            {
                if (!OutsideServers.Contains(ip) || !SpecialServers.ContainsKey(port))
                {
                    message = "This server does not support experience transfer.";
                }
                else
                {
                    // One-shot query, since it takes too long if it does fail. Plus, it's only one query instead of multi-A2S.
                    var extendedGet = GetInfoMCNew(ip, port, out var status);

                    if (!extendedGet || status == null)
                    {
                        message = "Could not connect to the server!";
                    }
                    else
                    {
                        var server = SpecialServers[port];
                        var users = GetMCUsers(server.Server, server.RCONPort, server.Password, out var client, false);
                        var sourceMCUsername = UNODatabaseService.GetMinecraftUser(source).GetAwaiter().GetResult();
                        var sourceUser = users.Find(o => o.Online && o.Username == sourceMCUsername);
                        var targetUser = users.Find(o => o.Online && o.Username == target);

                        var textAmount = amountIn.ToLower().Trim();
                        var numAmount = int.TryParse(amountIn, out var amount);
                        if (textAmount == "all" || textAmount == "max")
                        {
                            amount = 1;
                            numAmount = true;
                        }

                        if (sourceMCUsername == null)
                        {
                            message = "Failed to find a username associated to this Discord account.";
                        }
                        else if (sourceUser == null)
                        {
                            message = "You must be online to make this request.";
                        }
                        else if (targetUser == null)
                        {
                            message = "The target user must be online to make this request.";
                        }
                        else if (!numAmount || amount <= 0)
                        {
                            message = "Invalid amount! It must be a positive number.";
                        }
                        else if (sourceUser.Experience < amount)
                        {
                            message = $"You have {sourceUser.Experience}, but you're trying to give {amount}.";
                        }
                        else
                        {
                            if (textAmount == "all" || textAmount == "max")
                                amount = sourceUser.Experience;
                            client.Execute($"xp add {sourceUser.Username} -{amount} points", true);
                            client.Execute($"xp add {targetUser.Username} {amount} points");
                            messageTitle = "Nice.";

                            message = $"Transfer successful.{(sourceUser == targetUser ? " But why?" : "")}\n" +
                                      $"{sourceUser.Username}: {sourceUser.Experience} → {sourceUser.Experience -= amount}\n" +
                                      $"{targetUser.Username}: {targetUser.Experience} → {targetUser.Experience += amount}\n";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LoggerService.Log(LogSeverity.Error, message, e);
            }

            var random = ThreadSafeRandom.ThisThreadsRandom;

            var builder = new EmbedBuilder()
                .WithColor(new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Experience Transfer for {ip}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField(messageTitle, message);
            result = builder.Build();
            return true;
        }

        public static bool WebhookEmbed(WebhookListener.CommitInfo info, out Embed result)
        {
            var random = ThreadSafeRandom.ThisThreadsRandom;

            var builder = new EmbedBuilder()
                .WithColor(new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256)))
                .WithTimestamp(info.CommitDate)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Push by {info.UserName} for {info.RepoName}")
                        .WithIconUrl(info.UserAvatar);
                })
                .WithThumbnailUrl(info.RepoAvatar)
                .WithDescription(info.CommitMessage)
                .AddField("Commit Hash", info.CommitHash.Substring(0, Math.Min(7, info.CommitHash.Length)), true);
            result = builder.Build();
            return true;
        }

        public static bool OctoprintEmbed(WebhookListener.OctoprintInfo info, out Embed result)
        {
            var random = ThreadSafeRandom.ThisThreadsRandom;

            var builder = new EmbedBuilder()
                .WithColor(new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256)))
                .WithTimestamp(info.Timestamp)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"{info.Topic} - {info.Job?["file"]?["name"] ?? "Unknown File"}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .WithDescription(info.Message)
                .AddField("Status", info.State["text"], true);
            var completion = info.Progress["completion"];
            var printTime = info.Progress["printTime"];
            var printTimeLeft = info.Progress["printTimeLeft"];
            var bytesFile = info.Job["file"]?["size"];
            var bytesPrinted = info.Progress["filepos"];
            if (completion != null && completion.Type != JTokenType.Null)
                builder.AddField("Progress", completion.ToObject<double>().ToString("N2") + "%", true);
            if (printTime != null && printTime.Type != JTokenType.Null)
                builder.AddField("Time", HumanReadable(printTime.ToObject<float>()), true);
            if (printTimeLeft != null && printTimeLeft.Type != JTokenType.Null)
                builder.AddField("Estimated Time Left", HumanReadable(printTimeLeft.ToObject<float>()), true);
            if (bytesFile != null && bytesFile.Type != JTokenType.Null)
                builder.AddField("File Size", (bytesFile.ToObject<float>() / 1000000.0).ToString("N2") + " MB", true);
            if (bytesPrinted != null && bytesPrinted.Type != JTokenType.Null)
                builder.AddField("Bytes Printed", (bytesPrinted.ToObject<float>() / 1000000.0).ToString("N2") + " MB",
                    true);
            result = builder.Build();
            return true;
        }
    }
}