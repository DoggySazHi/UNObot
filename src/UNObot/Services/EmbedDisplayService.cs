﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
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
        public static async Task<Embed> DisplayGame(ulong ServerID)
        {
            var card = await UNODatabaseService.GetCurrentCard(ServerID);

            uint cardColor = card.Color switch
            {
                "Red" => (uint) 0xFF0000,
                "Blue" => 0x0000FF,
                "Yellow" => 0xFFFF00,
                _ => 0x00FF00,
            };
            var response = "";
            var Gamemode = await UNODatabaseService.GetGamemode(ServerID);
            string server = Program._client.GetGuild(ServerID).Name;
            foreach (ulong id in await UNODatabaseService.GetPlayers(ServerID))
            {
                var user = Program._client.GetUser(id);
                var cardCount = (await UNODatabaseService.GetCards(id)).Count();
                if (!Gamemode.HasFlag(UNOCoreServices.Gamemodes.Private))
                {
                    if (id == (await UNODatabaseService.GetPlayers(ServerID)).Peek())
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
                    if (id == (await UNODatabaseService.GetPlayers(ServerID)).Peek())
                        response += $"**{user.Username}** - ??? cards\n";
                    else
                        response += $"{user.Username} - ??? cards\n";
                }
            }

            var builder = new EmbedBuilder()
                .WithTitle("Current Game")
                .WithDescription(await UNODatabaseService.GetDescription(ServerID))
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

        public static async Task<Embed> DisplayCards(ulong userid, ulong ServerID)
        {
            string server = Program._client.GetGuild(ServerID).Name;
            var currentCard = await UNODatabaseService.GetCurrentCard(ServerID);
            var cards = await UNODatabaseService.GetCards(userid);
            cards = cards.OrderBy(o => o.Color).ThenBy(o => o.Value).ToList();

            string RedCards = "";
            string GreenCards = "";
            string BlueCards = "";
            string YellowCards = "";
            string WildCards = "";

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

        public static Tuple<Embed, Tuple<string, string, string>> DisplayAddSong(ulong UserID, ulong ServerID,
            string SongURL, Tuple<string, string, string> Information)
        {
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
                .AddField("Duration",
                    $"{(string.IsNullOrEmpty(CurrentDuration) ? "" : $"{CurrentDuration}/")}{Song.Duration}")
                .AddField("Requested By", Username);
            return builder.Build();
        }

        public static async Task<Tuple<Embed, Playlist>> DisplayPlaylist(ulong UserID, ulong ServerID, string SongURL)
        {
            string Username = Program._client.GetUser(UserID).Username;
            string Servername = Program._client.GetGuild(ServerID).Name;
            Random r = ThreadSafeRandom.ThisThreadsRandom;
            var Playlist = await YoutubeService.GetSingleton().GetPlaylist(SongURL);
            var Thumbnail = await YoutubeService.GetSingleton().GetPlaylistThumbnail(Playlist.Id);

            var builder = new EmbedBuilder()
                .WithTitle(Playlist.Title)
                .WithUrl(Playlist.Url)
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithThumbnailUrl(Thumbnail)
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Playing in {Servername}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Description", $"{Playlist.Description}")
                .AddField("Author", $"{Playlist.Author}")
                .AddField("Requested By", Username);
            return new Tuple<Embed, Playlist>(builder.Build(), Playlist);
        }

        public static Tuple<Embed, int> DisplaySongList(Song NowPlaying, List<Song> Songs, int Page)
        {
            List<StringBuilder> Containers = new List<StringBuilder>();
            Random r = ThreadSafeRandom.ThisThreadsRandom;
            string Server = Program._client.GetGuild(NowPlaying.RequestedGuild).Name;

            int Index = 0;

            if (Songs.Count == 0)
                Containers.Add(new StringBuilder("There are no songs queued."));

            while (Index < Songs.Count)
            {
                StringBuilder List = new StringBuilder();

                for (int i = 0; i < 9; i++)
                {
                    if (Index >= Songs.Count)
                        break;
                    Song s = Songs[Index++];
                    string Username = Program._client.GetUser(s.RequestedBy).Username;
                    string NextLine = $"``{Index}.``[{s.Name}]({s.URL}) |``{s.Duration} Requested by: {Username}``\n\n";
                    if (List.Length + NextLine.Length > 1024)
                    {
                        Index--;
                        break;
                    }

                    List.Append(NextLine);
                }

                Containers.Add(List);
            }

            if (Page <= 0 || Page > Containers.Count)
                return new Tuple<Embed, int>(null, Containers.Count);
            return new Tuple<Embed, int>(
                DisplaySongList(Server, r, Page, Containers.Count, Containers[Page - 1], NowPlaying), Containers.Count);
        }

        private static Embed DisplaySongList(string Server, Random r, int Page, int MaxPages, StringBuilder List,
            Song NowPlaying)
        {
            var builder = new EmbedBuilder()
                .WithTitle("Now Playing")
                .WithDescription(
                    $"[{NowPlaying.Name}]({NowPlaying.URL}) |``{NowPlaying.Duration} Requested by: {Program._client.GetUser(NowPlaying.RequestedBy).Username}``")
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
                        .WithName($"Page {Page}/{MaxPages} | Playing in {Server}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("Queued", List.ToString());

            return builder.Build();
        }

        private const int Attempts = 3;

        public static bool UnturnedQueryEmbed(string IP, ushort Port, out Embed Result, ServerAverages Averages = null)
        {
            A2S_INFO Information = null;
            bool InformationGet = false;
            A2S_PLAYER Players = null;
            bool PlayersGet = false;
            A2S_RULES Rules = null;
            bool RulesGet = false;
            for (int i = 0; i < Attempts; i++)
            {
                if (!InformationGet)
                    InformationGet = GetInfo(IP, (ushort) (Port + 1), out Information);
                if (!PlayersGet)
                    PlayersGet = GetPlayers(IP, (ushort) (Port + 1), out Players);
                if (!RulesGet)
                    RulesGet = GetRules(IP, (ushort) (Port + 1), out Rules);
            }

            if (!InformationGet || !PlayersGet || !RulesGet)
            {
                Result = null;
                return false;
            }

            var Random = ThreadSafeRandom.ThisThreadsRandom;
            string ServerName = Information.Name;
            string ServerImageURL = Rules.Rules
                .FirstOrDefault(o => o.Name.Contains("Browser_Icon", StringComparison.OrdinalIgnoreCase)).Value;
            string ServerDescription = "";
            string Map = Information.Map;
            string PlayersOnline = "";
            bool VACEnabled = Information.VAC == A2S_INFO.VACFlags.Secured;
            string UnturnedVersion = Rules.Rules
                .FirstOrDefault(o => o.Name.Contains("unturned", StringComparison.OrdinalIgnoreCase)).Value;
            string RocketModVersion = "";
            string RocketModPlugins = "";

            ServerImageURL ??= "";

            UnturnedVersion ??= $"Unknown Version ({Information.Version}?)";

            int DescriptionLines = Convert.ToInt32(Rules.Rules
                .Find(o => o.Name.Contains("Browser_Desc_Full_Count", StringComparison.OrdinalIgnoreCase)).Value
                .Trim());
            for (int i = 0; i < DescriptionLines; i++)
                ServerDescription += Rules.Rules.FirstOrDefault(o =>
                    o.Name.Contains($"Browser_Desc_Full_Line_{i}", StringComparison.OrdinalIgnoreCase)).Value;
            for (int i = 0; i < Players.PlayerCount; i++)
            {
                PlayersOnline +=
                    $"{Players.Players[i].Name} - {HumanReadable(Players.Players[i].Duration)}";
                if (i != Players.PlayerCount - 1)
                    PlayersOnline += "\n";
            }

            int RocketExists =
                Rules.Rules.FindIndex(o => o.Name.Contains("rocket", StringComparison.OrdinalIgnoreCase));
            int RocketPluginsExists =
                Rules.Rules.FindIndex(o => o.Name.Contains("rocketplugins", StringComparison.OrdinalIgnoreCase));
            if (RocketExists != -1)
                RocketModVersion = Rules.Rules[RocketExists].Value;
            if (RocketPluginsExists != -1)
                RocketModPlugins = Rules.Rules[RocketPluginsExists].Value;

            var builder = new EmbedBuilder()
                .WithTitle("Description")
                .WithDescription(ServerDescription)
                .WithColor(new Color(Random.Next(0, 256), Random.Next(0, 256), Random.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithThumbnailUrl(ServerImageURL)
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Server Query of {ServerName}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("IP", IP, true)
                .AddField("Port", Port, true)
                .AddField("Map", Map, true)
                .AddField("VAC Security", VACEnabled ? "Enabled" : "Disabled", true)
                .AddField($"Versions",
                    $"Unturned: {UnturnedVersion}{(RocketModVersion == "" ? "" : $"\nRocketMod: {RocketModVersion}")}",
                    true);
            if (!string.IsNullOrWhiteSpace(RocketModPlugins))
                builder.AddField($"RocketMod Plugins", RocketModPlugins, true);
            builder.AddField($"Players: {Information.Players}/{Information.MaxPlayers}",
                string.IsNullOrWhiteSpace(PlayersOnline) ? "Nobody's online!" : PlayersOnline, true);
            if (Averages != null)
                builder.AddField("Server Averages",
                    $"Last hour: {Averages.AverageLastHour:N2} players\n" +
                    $"Last 24 hours: {Averages.AverageLast24H:N2} players\n" +
                    $"Last week: {Averages.AverageLastWeek:N2} players\n" +
                    $"Last month: {Averages.AverageLastMonth:N2} players\n" +
                    $"Last year: {Averages.AverageLastYear:N2} players\n", true);
            Result = builder.Build();
            return true;
        }
        
        public static bool MinecraftQueryEmbed(string IP, ushort Port, out Embed Result)
        {
            //TODO with the new option to disable status, it might be that queries work but not simple statuses.
            var DefaultStatus = new MCStatus(IP, Port);
            var ExtendedGet = GetInfoMCNew(IP, Port, out var ExtendedStatus);

            if (!DefaultStatus.ServerUp && !ExtendedGet)
            {
                Result = null;
                return false;
            }

            List<MCUser> MCUserInfo = null;
            if (OutsideServers.Contains(IP) && SpecialServers.ContainsKey(Port))
            {
                var Server = SpecialServers[Port];
                MCUserInfo = GetMCUsers(Server.Server, Server.RCONPort, Server.Password, out _);
            }

            var Random = ThreadSafeRandom.ThisThreadsRandom;
            var ServerDescription = DefaultStatus.Motd;
            var PlayersOnline = DefaultStatus.CurrentPlayers == "0" ? "" : "Unknown (server doesn't have query on!)";

            if (ExtendedStatus != null && ExtendedGet)
            {
                PlayersOnline = "";
                for (int i = 0; i < ExtendedStatus.Players.Length; i++)
                {
                    PlayersOnline += $"{ExtendedStatus.Players[i]}";
                    if (MCUserInfo != null)
                    {
                        var UserInfo = MCUserInfo.Find(o => o.Username == ExtendedStatus.Players[i]);
                        if (UserInfo != null)
                            PlayersOnline += $"\n- **Ouchies:** {UserInfo.Ouchies} | **Health:** {UserInfo.Health} | **Food:** {UserInfo.Food}\n- **Experience:** {UserInfo.Experience}";
                        else
                            PlayersOnline += "\n Unknown stats.";
                    }

                    if (i != ExtendedStatus.Players.Length - 1)
                        PlayersOnline += "\n";
                }
            }

            var builder = new EmbedBuilder()
                .WithTitle("MOTD")
                .WithDescription(ServerDescription)
                .WithColor(new Color(Random.Next(0, 256), Random.Next(0, 256), Random.Next(0, 256)))
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
                        .WithName($"Minecraft Server Query of {IP}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("IP", IP, true)
                .AddField("Port", Port, true)
                .AddField("Version", $"{DefaultStatus.Version}", true)
                .AddField("Ping", $"{DefaultStatus.Delay} ms", true)
                .AddField($"Players: {DefaultStatus.CurrentPlayers}/{DefaultStatus.MaximumPlayers}",
                    string.IsNullOrWhiteSpace(PlayersOnline) ? "Nobody's online!" : PlayersOnline, true);
            Result = builder.Build();
            return true;
        }

        public static bool OuchiesEmbed(string IP, ushort Port, out Embed Result)
        {
            var Random = ThreadSafeRandom.ThisThreadsRandom;

            if (!OutsideServers.Contains(IP) || !SpecialServers.ContainsKey(Port))
            {
            
                Result = new EmbedBuilder()
                    .WithColor(new Color(Random.Next(0, 256), Random.Next(0, 256), Random.Next(0, 256)))
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
                            .WithName($"Ouchies of {IP}")
                            .WithIconUrl("https://williamle.com/unobot/unobot.png");
                    })
                    .AddField("Mukyu~", "Invalid port for checking ouchies!").Build();
                return true;
            }
            
            MinecraftStatus Status = null;
            var ExtendedGet = false;
            for (var i = 0; i < Attempts; i++)
            {
                if (!ExtendedGet)
                    ExtendedGet = GetInfoMCNew(IP, Port, out Status);
            }

            if (!ExtendedGet || Status == null)
            {
                Result = null;
                return false;
            }

            var Server = SpecialServers[Port];
            var Ouchies = GetMCUsers(Server.Server, Server.RCONPort, Server.Password, out _);

            var PlayersOnline = "";

            foreach (var Item in Ouchies)
                PlayersOnline += $"{Item.Username} - {Item.Ouchies} Ouchies\n";
            // Doesn't seem to affect embeds. PlayersOnline = PlayersOnline.Substring(0, PlayersOnline.Length - 1);

            var builder = new EmbedBuilder()
                .WithTitle("MOTD")
                .WithDescription(Status.MOTD)
                .WithColor(new Color(Random.Next(0, 256), Random.Next(0, 256), Random.Next(0, 256)))
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
                        .WithName($"Ouchies of {IP}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("IP", IP, true)
                .AddField("Port", Port, true)
                .AddField("Version", $"{Status.Version}", true)
                .AddField("Ouchies Listing", PlayersOnline, true);
            Result = builder.Build();
            return true;
        }

        public static bool LocationsEmbed(string IP, ushort Port, out Embed Result)
        {
            var Random = ThreadSafeRandom.ThisThreadsRandom;

            if (!OutsideServers.Contains(IP) || !SpecialServers.ContainsKey(Port))
            {
            
                Result = new EmbedBuilder()
                    .WithColor(new Color(Random.Next(0, 256), Random.Next(0, 256), Random.Next(0, 256)))
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
                            .WithName($"Player Locations of {IP}")
                            .WithIconUrl("https://williamle.com/unobot/unobot.png");
                    })
                    .AddField("Mukyu~", "Invalid port for checking ouchies!").Build();
                return true;
            }

            MinecraftStatus Status = null;
            var ExtendedGet = false;
            for (var i = 0; i < Attempts; i++)
            {
                if (!ExtendedGet)
                    ExtendedGet = GetInfoMCNew(IP, Port, out Status);
            }

            if (!ExtendedGet || Status == null)
            {
                Result = null;
                return false;
            }

            var Server = SpecialServers[Port];
            var Users = GetMCUsers(Server.Server, Server.RCONPort, Server.Password, out _);

            var PlayersOnline = "";
            foreach(var User in Users)
                if (User.Online)
                {
                    PlayersOnline +=
                        $"{User.Username} - **X:** {User.Coordinates[0]:N2} **Y:** {User.Coordinates[1]:N2} **Z:** {User.Coordinates[2]:N2} ";
                    if(User.Coordinates.Length == 4)
                        PlayersOnline += User.Coordinates[3] switch
                        {
                            1 => "**End**",
                            -1 => "**Nether**",
                            _ => "**Overworld**"
                        };
                    PlayersOnline += "\n";
                }

            if (PlayersOnline == "")
                PlayersOnline = "Nobody's online!";
            // Doesn't seem to affect embeds. PlayersOnline = PlayersOnline.Substring(0, PlayersOnline.Length - 1);

            var builder = new EmbedBuilder()
                .WithTitle("MOTD")
                .WithDescription(Status.MOTD)
                .WithColor(new Color(Random.Next(0, 256), Random.Next(0, 256), Random.Next(0, 256)))
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
                        .WithName($"Player Locations of {IP}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField("IP", IP, true)
                .AddField("Port", Port, true)
                .AddField("Version", $"{Status.Version}", true)
                .AddField("Players", PlayersOnline, true);
            Result = builder.Build();
            return true;
        }

        public static bool TransferEmbed(string IP, ushort Port, ulong Source, string Target, string AmountIn, out Embed Result)
        {
            var MessageTitle = "Mukyu~";
            var Message = "General error; IDK what happened, see UNObot logs.";

            try
            {
                if (!OutsideServers.Contains(IP) || !SpecialServers.ContainsKey(Port))
                {
                    Message = "This server does not support experience transfer.";
                }
                else
                {
                    // One-shot query, since it takes too long if it does fail. Plus, it's only one query instead of multi-A2S.
                    var ExtendedGet = GetInfoMCNew(IP, Port, out var Status);

                    if (!ExtendedGet || Status == null)
                    {
                        Message = "Could not connect to the server!";
                    }
                    else
                    {
                        var Server = SpecialServers[Port];
                        var Users = GetMCUsers(Server.Server, Server.RCONPort, Server.Password, out var Client, false);
                        var SourceMCUsername = UNODatabaseService.GetMinecraftUser(Source).GetAwaiter().GetResult();
                        var SourceUser = Users.Find(o => o.Online && o.Username == SourceMCUsername);
                        var TargetUser = Users.Find(o => o.Online && o.Username == Target);

                        var TextAmount = AmountIn.ToLower().Trim();
                        var NumAmount = int.TryParse(AmountIn, out var Amount);
                        if (TextAmount == "all" || TextAmount == "max")
                        {
                            Amount = 1;
                            NumAmount = true;
                        }
                        if (SourceMCUsername == null)
                            Message = "Failed to find a username associated to this Discord account.";
                        else if (SourceUser == null)
                            Message = "You must be online to make this request.";
                        else if (TargetUser == null)
                            Message = "The target user must be online to make this request.";
                        else if (!NumAmount || Amount <= 0)
                            Message = "Invalid amount! It must be a positive number.";
                        else if (SourceUser.Experience < Amount)
                            Message = $"You have {SourceUser.Experience}, but you're trying to give {Amount}.";
                        else
                        {
                            if (TextAmount == "all" || TextAmount == "max")
                                Amount = SourceUser.Experience;
                            Client.Execute($"xp add {SourceUser.Username} -{Amount} points", true);
                            Client.Execute($"xp add {TargetUser.Username} {Amount} points");
                            MessageTitle = "Nice.";
                            
                            Message = $"Transfer successful.{(SourceUser == TargetUser ? " But why?" : "")}\n" +
                                      $"{SourceUser.Username}: {SourceUser.Experience} → {SourceUser.Experience -= Amount}\n" +
                                      $"{TargetUser.Username}: {TargetUser.Experience} → {TargetUser.Experience += Amount}\n";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LoggerService.Log(LogSeverity.Error, Message, e);
            }

            var Random = ThreadSafeRandom.ThisThreadsRandom;
            
            var builder = new EmbedBuilder()
                .WithColor(new Color(Random.Next(0, 256), Random.Next(0, 256), Random.Next(0, 256)))
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
                        .WithName($"Experience Transfer for {IP}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField(MessageTitle, Message);
            Result = builder.Build();
            return true;
        }
        
        public static bool WebhookEmbed(WebhookListener.CommitInfo Info, out Embed Result)
        {
            var Random = ThreadSafeRandom.ThisThreadsRandom;

            var builder = new EmbedBuilder()
                .WithColor(new Color(Random.Next(0, 256), Random.Next(0, 256), Random.Next(0, 256)))
                .WithTimestamp(Info.CommitDate)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Push by {Info.UserName} for {Info.RepoName}")
                        .WithIconUrl(Info.UserAvatar);
                })
                .WithThumbnailUrl(Info.RepoAvatar)
                .WithDescription(Info.CommitMessage)
                .AddField("Commit Hash", Info.CommitHash.Substring(0, Math.Min(7, Info.CommitHash.Length)), true);
            Result = builder.Build();
            return true;
        }

        public static bool OctoprintEmbed(WebhookListener.OctoprintInfo Info, out Embed Result)
        {
            var Random = ThreadSafeRandom.ThisThreadsRandom;

            var builder = new EmbedBuilder()
                .WithColor(new Color(Random.Next(0, 256), Random.Next(0, 256), Random.Next(0, 256)))
                .WithTimestamp(Info.Timestamp)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {Program.version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"{Info.Topic} - {Info.Job?["file"]?["name"] ?? "Unknown File"}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .WithDescription(Info.Message)
                .AddField("Status", Info.State["text"], true)
                .AddField("Progress", Info.Progress["completion"]?.ToObject<double>().ToString("N2") + "%", true)
                .AddField("Time", HumanReadable(Info.Progress["printTime"]?.ToObject<float>() ?? 0), true);
            var Estimate = Info.Progress["printTimeLeft"]?.ToObject<float>();
            if (Estimate != null)
                builder.AddField("Estimated Time Left", HumanReadable(Estimate.Value), true);
            Result = builder.Build();
            return true;
        }
    }
}
