using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Discord;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;
using UNObot.Plugins.TerminalCore;
using static UNObot.ServerQuery.Services.MinecraftProcessorService;

namespace UNObot.ServerQuery.Services
{
    public class EmbedService
    {
        private const int Attempts = 3;

        private readonly ILogger _logger;
        private readonly IConfig _config;
        private readonly DatabaseService _db;
        private readonly QueryHandlerService _query;
        private readonly MinecraftProcessorService _minecraft;

        public EmbedService(ILogger logger, IConfig config, DatabaseService db, QueryHandlerService query,
            MinecraftProcessorService minecraft)
        {
            _logger = logger;
            _config = config;
            _db = db;
            _query = query;
            _minecraft = minecraft;
        }
        
        public bool UnturnedQueryEmbed(string ip, ushort port, out Embed result, ServerAverages averages = null)
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
                    informationGet = _query.GetInfo(ip, (ushort) (port + 1), out information);
                if (!playersGet)
                    playersGet = _query.GetPlayers(ip, (ushort) (port + 1), out players);
                if (!rulesGet)
                    rulesGet = _query.GetRules(ip, (ushort) (port + 1), out rules);
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
                    $"{players.Players[i].Name} - {TimeHelper.HumanReadable(players.Players[i].Duration)}";
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
                        .WithText($"UNObot {_config.Version} - By DoggySazHi")
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
                    (averages.Ready ? "" : "**Warning**: UNObot is still loading stats...") + 
                    $"Last hour: {averages.AverageLastHour:N2} players\n" +
                    $"Last 24 hours: {averages.AverageLast24H:N2} players\n" +
                    $"Last week: {averages.AverageLastWeek:N2} players\n" +
                    $"Last month: {averages.AverageLastMonth:N2} players\n" +
                    $"Last year: {averages.AverageLastYear:N2} players\n", true);
            result = builder.Build();
            return true;
        }

        public bool MinecraftQueryEmbed(string ip, ushort port, out Embed result)
        {
            //TODO with the new option to disable status, it might be that queries work but not simple statuses.
            var defaultStatus = new MCStatus(ip, port);
            var extendedGet = _query.GetInfoMCNew(ip, port, out var extendedStatus);

            if (!defaultStatus.ServerUp && !extendedGet)
            {
                result = null;
                return false;
            }

            List<MCUser> mcUserInfo = null;
            if (_query.OutsideServers.Contains(ip) && _query.SpecialServers.ContainsKey(port))
            {
                var server = _query.SpecialServers[port];
                mcUserInfo = _minecraft.GetMCUsers(server.Server, server.RCONPort, server.Password, out _);
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
                                $"\n- {(userInfo.Ouchies != null ? $"**Ouchies:** {userInfo.Ouchies} | " : "" )}**Health:** {userInfo.Health} | **Food:** {userInfo.Food}\n- **Experience:** {userInfo.Experience} ({userInfo.ExperienceLevels} levels)";
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
                        .WithText($"UNObot {_config.Version} - By DoggySazHi")
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
        
        public bool MinecraftPEQueryEmbed(string ip, ushort port, out Embed result)
        {
            var ping = new Stopwatch();
            ping.Start();
            var extendedGet = _query.GetInfoMCNew(ip, port, out var extendedStatus);
            ping.Stop();

            if (!extendedGet)
            {
                result = null;
                return false;
            }

            List<MCUser> mcUserInfo = null;
            if (_query.OutsideServers.Contains(ip) && _query.SpecialServers.ContainsKey(port))
            {
                var server = _query.SpecialServers[port];
                mcUserInfo = _minecraft.GetMCUsers(server.Server, server.RCONPort, server.Password, out _);
            }

            var random = ThreadSafeRandom.ThisThreadsRandom;
            var serverDescription = extendedStatus.Motd;
            
            var playersOnline = "";
            for (var i = 0; i < extendedStatus.Players.Length; i++)
            {
                playersOnline += $"{extendedStatus.Players[i]}";
                if (mcUserInfo != null)
                {
                    var userInfo = mcUserInfo.Find(o => o.Username == extendedStatus.Players[i]);
                    if (userInfo != null)
                        playersOnline +=
                            $"\n- **Ouchies:** {userInfo.Ouchies} | **Health:** {userInfo.Health} | **Food:** {userInfo.Food}\n- **Experience:** {userInfo.Experience} ({userInfo.ExperienceLevels} levels)";
                    else
                        playersOnline += "\n Unknown stats.";
                }

                if (i != extendedStatus.Players.Length - 1)
                    playersOnline += "\n";
            }

            var builder = new EmbedBuilder()
                .WithTitle("MOTD")
                .WithDescription(serverDescription)
                .WithColor(new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {_config.Version} - By DoggySazHi")
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
                .AddField("Version", $"{extendedStatus.Version}", true)
                .AddField("Ping", $"{ping.ElapsedMilliseconds} ms", true)
                .AddField($"Players: {extendedStatus.Players.Length}/{extendedStatus.MaxPlayers}",
                    string.IsNullOrWhiteSpace(playersOnline) ? "Nobody's online!" : playersOnline, true);
            result = builder.Build();
            return true;
        }

        public bool OuchiesEmbed(string ip, ushort port, out Embed result)
        {
            var random = ThreadSafeRandom.ThisThreadsRandom;

            if (!_query.OutsideServers.Contains(ip) || !_query.SpecialServers.ContainsKey(port))
            {
                result = new EmbedBuilder()
                    .WithColor(new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256)))
                    .WithTimestamp(DateTimeOffset.Now)
                    .WithFooter(footer =>
                    {
                        footer
                            .WithText($"UNObot {_config.Version} - By DoggySazHi")
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
                    extendedGet = _query.GetInfoMCNew(ip, port, out status);

            if (!extendedGet || status == null)
            {
                result = null;
                return false;
            }

            var server = _query.SpecialServers[port];
            var ouchies = _minecraft.GetMCUsers(server.Server, server.RCONPort, server.Password, out _);

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
                        .WithText($"UNObot {_config.Version} - By DoggySazHi")
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

        public bool LocationsEmbed(string ip, ushort port, out Embed result)
        {
            var random = ThreadSafeRandom.ThisThreadsRandom;

            if (!_query.OutsideServers.Contains(ip) || !_query.SpecialServers.ContainsKey(port))
            {
                result = new EmbedBuilder()
                    .WithColor(new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256)))
                    .WithTimestamp(DateTimeOffset.Now)
                    .WithFooter(footer =>
                    {
                        footer
                            .WithText($"UNObot {_config.Version} - By DoggySazHi")
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
                    extendedGet = _query.GetInfoMCNew(ip, port, out status);

            if (!extendedGet || status == null)
            {
                result = null;
                return false;
            }

            var server = _query.SpecialServers[port];
            var users = _minecraft.GetMCUsers(server.Server, server.RCONPort, server.Password, out _);

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
                        .WithText($"UNObot {_config.Version} - By DoggySazHi")
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

        public bool TransferEmbed(string ip, ushort port, ulong source, string target, string amountIn,
            out Embed result)
        {
            var messageTitle = "Mukyu~";
            var message = "General error; IDK what happened, see UNObot logs.";

            try
            {
                if (!_query.OutsideServers.Contains(ip) || !_query.SpecialServers.ContainsKey(port))
                {
                    message = "This server does not support experience transfer.";
                }
                else
                {
                    // One-shot query, since it takes too long if it does fail. Plus, it's only one query instead of multi-A2S.
                    var extendedGet = _query.GetInfoMCNew(ip, port, out var status);

                    if (!extendedGet || status == null)
                    {
                        message = "Could not connect to the server!";
                    }
                    else
                    {
                        var server = _query.SpecialServers[port];
                        var users = _minecraft.GetMCUsers(server.Server, server.RCONPort, server.Password, out var client, false);
                        var sourceMCUsername = _db.GetMinecraftUser(source).GetAwaiter().GetResult();
                        var sourceUser = users.Find(o => o.Online && o.Username == sourceMCUsername);
                        var targetUser = users.Find(o => o.Online && o.Username == target);

                        var textOriginal = amountIn.ToLower().Trim();
                        var textAmount = textOriginal.Replace('l', ' ').Replace('t', ' ');
                        var numAmount = int.TryParse(textAmount, out var amount);
                        var fullTransfer = textOriginal.Contains("all") || textOriginal.Contains("max");
                        if (fullTransfer)
                        {
                            amount = 1;
                            numAmount = true;
                        }
                        else
                        {
                            var option = (textOriginal.Contains('l') ? 1 : 0) + (textOriginal.Contains('t') ? 2 : 0);
                            switch (option)
                            {
                                case 1 when sourceUser != null:
                                    amount = (int) (Exp(sourceUser.ExperienceLevels, 0) - Exp(sourceUser.ExperienceLevels - amount, 0));
                                    break;
                                case 2 when targetUser != null:
                                    amount -= targetUser.Experience;
                                    break;
                                case 3 when targetUser != null:
                                    amount = (int) Exp(amount, 0) - targetUser.Experience;
                                    break;
                            }
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
                            if (fullTransfer)
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
                _logger.Log(LogSeverity.Error, message, e);
            }

            var random = ThreadSafeRandom.ThisThreadsRandom;

            var builder = new EmbedBuilder()
                .WithColor(new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {_config.Version} - By DoggySazHi")
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
    }
}