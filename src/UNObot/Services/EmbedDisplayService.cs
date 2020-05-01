using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using YoutubeExplode.Playlists;

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
            string response = "";
            ushort isPrivate = await UNODatabaseService.GetGamemode(ServerID);
            string server = Program._client.GetGuild(ServerID).Name;
            foreach (ulong id in await UNODatabaseService.GetPlayers(ServerID))
            {
                var user = Program._client.GetUser(id);
                var cardCount = (await UNODatabaseService.GetCards(id)).Count();
                if (isPrivate != 2)
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
                    InformationGet = QueryHandlerService.GetInfo(IP, (ushort) (Port + 1), out Information);
                if (!PlayersGet)
                    PlayersGet = QueryHandlerService.GetPlayers(IP, (ushort) (Port + 1), out Players);
                if (!RulesGet)
                    RulesGet = QueryHandlerService.GetRules(IP, (ushort) (Port + 1), out Rules);
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
                    $"{Players.Players[i].Name} - {QueryHandlerService.HumanReadable(Players.Players[i].Duration)}";
                if (i != Players.PlayerCount - 1)
                    PlayersOnline += "\n";
            }

            int RocketExists =
                Rules.Rules.FindIndex(o => o.Name.Contains($"rocket", StringComparison.OrdinalIgnoreCase));
            int RocketPluginsExists =
                Rules.Rules.FindIndex(o => o.Name.Contains($"rocketplugins", StringComparison.OrdinalIgnoreCase));
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

        class MCUser
        {
            public string Username { get; set; }
            public string Ouchies { get; set; }
            public bool Online { get; set; }
            public double[] Coordinates { get; set; }
            public string Health { get; set; }
            public string Food { get; set; }
            public int Experience { get; set; }
        }

        // NOTE: It's the query port!
        private static List<MCUser> GetMCUsers(string IP, ushort Port, string Password, out MinecraftRCON Client, bool Dispose = true)
        {
            var Output = new List<MCUser>();

            // Smaller than ulong keys, big enough for RNG.
            var RandomKey = (ulong) new Random().Next(0, 10000);
            var Success = QueryHandlerService.CreateRCON(IP, Port, Password, RandomKey, out Client);
            if (!Success) return Output;

            Client.Execute("list", true);
            if (Client.Status != MinecraftRCON.RCONStatus.SUCCESS) return Output;
            var PlayerListOnline = Client.Data.Substring(Client.Data.IndexOf(':') + 1).Split(',').ToList();

            Client.Execute("scoreboard players list", true);
            var PlayerListTotal = Client.Data.Substring(Client.Data.IndexOf(':') + 1).Split(',').ToList();

            foreach(var Player in PlayerListTotal)
            {
                var Name = Player.Replace((char) 0, ' ').Trim();
                Client.Execute($"scoreboard players get {Name} Ouchies", true);
                if (Client.Status == MinecraftRCON.RCONStatus.SUCCESS)
                {
                    var Ouchies = Client.Data.Contains("has") ? Client.Data.Split(' ')[2] : "0";
                    Output.Add(new MCUser
                    {
                        Username = Name,
                        Ouchies = Ouchies
                    });
                }
            }

            foreach (var o in PlayerListOnline)
            {
                var Name = o.Replace((char) 0, ' ').Trim();
                if (string.IsNullOrWhiteSpace(Name)) continue;
                Client.Execute(
                    $"execute as {Name} at @s run summon minecraft:armor_stand ~ ~ ~ {{Invisible:1b,PersistenceRequired:1b,Tags:[\"coordfinder\"]}}",
                    true);
                Client.Execute("execute as @e[tag=coordfinder] at @s run tp @s ~ ~ ~", true);
                double[] Coordinates = null;
                if (Client.Status == MinecraftRCON.RCONStatus.SUCCESS)
                {
                    try
                    {
                        var CoordinateMessage = Regex.Replace(Client.Data, @"[^0-9\+\-\. ]", "").Trim().Split(' ');
                        Coordinates = new[]
                        {
                            double.Parse(CoordinateMessage[0]),
                            double.Parse(CoordinateMessage[1]),
                            double.Parse(CoordinateMessage[2])
                        };
                    }
                    catch (FormatException)
                    {
                        LoggerService.Log(LogSeverity.Warning, $"Failed to process coordinates. Response: {Client.Data}");
                    }
                }
                Client.Execute($"scoreboard players get {Name} Health", true);
                var Health = Client.Data.Contains("has") ? Client.Data.Split(' ')[2] : "??";
                Client.Execute($"scoreboard players get {Name} Food", true);
                var Food = Client.Data.Contains("has") ? Client.Data.Split(' ')[2] : "??";
                Client.Execute("execute as @e[tag=coordfinder] at @s run kill @s", true);
                Client.Execute($"execute as {Name} at @s run experience query @s points", true);
                var PointData = Client.Data;
                Client.Execute($"execute as {Name} at @s run experience query @s levels", true);
                var Experience = 0;
                if (Client.Status == MinecraftRCON.RCONStatus.SUCCESS)
                {
                    try
                    {
                        var Points = int.Parse(PointData.Split(' ')[2]);
                        var Levels = int.Parse(Client.Data.Split(' ')[2]);
                        Experience = (int) Exp(Levels, Points);
                    }
                    catch (FormatException)
                    {
                        LoggerService.Log(LogSeverity.Warning, $"Failed to process coordinates. Response: {Client.Data}");
                    }
                }
                foreach (var CorrectUser in Output.Where(User => User.Username == Name))
                {
                    CorrectUser.Coordinates = Coordinates;
                    CorrectUser.Health = Health;
                    CorrectUser.Food = Food;
                    CorrectUser.Online = true;
                    CorrectUser.Experience = Experience;
                }
            }

            if(Dispose)
                Client.Dispose();
            return Output;
        }
        private static double Exp(int levels, int points)
        {
            if(levels <= 16)
                return Math.Pow(levels, 2) + 6 * levels + points;
            if (levels <= 31)
                return 2.5 * Math.Pow(levels, 2) - 40.5 * levels + 360 + points;
            return 4.5 * Math.Pow(levels, 2) - 162.5 * levels + 2220 + points;
        }

        public static bool MinecraftQueryEmbed(string IP, ushort Port, out Embed Result)
        {
            MCStatus DefaultStatus = null;
            MinecraftStatus ExtendedStatus = null;
            var ExtendedGet = false;
            for (var i = 0; i < Attempts; i++)
            {
                DefaultStatus ??= new MCStatus(IP, Port);
                if (!ExtendedGet)
                    ExtendedGet = QueryHandlerService.GetInfoMCNew(IP, Port, out ExtendedStatus);
            }

            if (DefaultStatus == null || !DefaultStatus.ServerUp && !ExtendedGet)
            {
                Result = null;
                return false;
            }

            List<MCUser> MCUserInfo = null;
            if ((IP == "127.0.0.1" || IP == "williamle.com" || IP == "localhost" || IP == "192.168.2.6") && Port == 27285)
            {
                MCUserInfo = GetMCUsers("192.168.2.6", 27286, "mukyumukyu", out _);
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
                        var UserInfo = MCUserInfo.Where(o => o.Username == ExtendedStatus.Players[i]).ToList();
                        if (UserInfo.Count != 0)
                            PlayersOnline += $"\n- **Ouchies: **{UserInfo[0].Ouchies} | **Health:** {UserInfo[0].Health} | **Food:** {UserInfo[0].Food}";
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
            if (IP != "127.0.0.1" && IP != "williamle.com" && IP != "localhost" && IP != "192.168.2.6" || Port != 27285)
            {
                Result = null;
                return false;
            }

            MinecraftStatus Status = null;
            var ExtendedGet = false;
            for (var i = 0; i < Attempts; i++)
            {
                if (!ExtendedGet)
                    ExtendedGet = QueryHandlerService.GetInfoMCNew(IP, Port, out Status);
            }

            if (!ExtendedGet || Status == null)
            {
                Result = null;
                return false;
            }

            var Ouchies = GetMCUsers("192.168.2.6", 27286, "mukyumukyu", out _);

            var Random = ThreadSafeRandom.ThisThreadsRandom;
            string PlayersOnline = "";

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
            if (IP != "127.0.0.1" && IP != "williamle.com" && IP != "localhost" && IP != "192.168.2.6" || Port != 27285)
            {
                Result = null;
                return false;
            }

            MinecraftStatus Status = null;
            var ExtendedGet = false;
            for (var i = 0; i < Attempts; i++)
            {
                if (!ExtendedGet)
                    ExtendedGet = QueryHandlerService.GetInfoMCNew(IP, Port, out Status);
            }

            if (!ExtendedGet || Status == null)
            {
                Result = null;
                return false;
            }

            var Users = GetMCUsers("192.168.2.6", 27286, "mukyumukyu", out _);

            var Random = ThreadSafeRandom.ThisThreadsRandom;
            var PlayersOnline = "";
            if (Users.Count == 0)
                PlayersOnline = "Nobody's online!";
            else foreach(var User in Users)
                if(User.Online)
                    PlayersOnline += $"{User.Username} - **X:** {User.Coordinates[0]:N2} **Y:** {User.Coordinates[1]:N2} **Z:** {User.Coordinates[2]:N2}\n";
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

        public static bool TransferEmbed(string IP, ushort Port, ulong Source, string Target, int Amount, out Embed Result)
        {
            var MessageTitle = "Mukyu~";
            var Message = "General error; IDK what happened, see UNObot logs.";

            try
            {
                if (IP != "127.0.0.1" && IP != "williamle.com" && IP != "localhost" && IP != "192.168.2.6" || Port != 27285)
                {
                    Message = "This server does not support experience transfer.";
                }
                else
                {
                    MinecraftStatus Status = null;
                    var ExtendedGet = false;
                    for (var i = 0; i < Attempts; i++)
                    {
                        if (!ExtendedGet)
                            ExtendedGet = QueryHandlerService.GetInfoMCNew(IP, Port, out Status);
                    }

                    if (!ExtendedGet || Status == null)
                    {
                        Message = "Could not connect to the server!";
                    }
                    else
                    {
                        var Users = GetMCUsers("192.168.2.6", 27286, "mukyumukyu", out var Client, false);
                        var SourceMCUsername = UNODatabaseService.GetMinecraftUser(Source).GetAwaiter().GetResult();
                        var SourceUser = Users.Find(o => o.Online && o.Username == SourceMCUsername);
                        var TargetUser = Users.Find(o => o.Online && o.Username == Target);

                        if (Amount <= 0)
                            Message = "Invalid amount! It must be a positive number.";
                        else if (SourceMCUsername == null)
                            Message = "Failed to find a username associated to this Discord account.";
                        else if (SourceUser == null)
                            Message = "You must be online to make this request.";
                        else if (TargetUser == null)
                            Message = "The target user must be online to make this request.";
                        else if (SourceUser.Experience < Amount)
                            Message = $"You have {SourceUser.Experience}, but you're trying to give {Amount}.";
                        else
                        {
                            Client.Execute($"xp add {SourceUser.Username} -{Amount} points", true);
                            Client.Execute($"xp add {TargetUser.Username} {Amount} points");
                            MessageTitle = "Nice.";
                            Message = "Transfer successful.\n" +
                                      $"{SourceUser.Username}: {SourceUser.Experience} → {SourceUser.Experience - Amount}\n" +
                                      $"{TargetUser.Username}: {TargetUser.Experience} → {TargetUser.Experience + Amount}\n";
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
                        .WithName("Experience Transfer")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .AddField(MessageTitle, Message);
            Result = builder.Build();
            return true;
        }
    }
}
