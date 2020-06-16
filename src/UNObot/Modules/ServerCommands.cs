﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Services;
using static UNObot.Services.IRCON;

namespace UNObot.Modules
{
    public class ServerCommands : ModuleBase<SocketCommandContext>
    {
        private const string TooLong = "[Message is too long; trimmed.]\n";

        [Command("ubows", RunMode = RunMode.Async)]
        [Alias("ubow")]
        [Help(new[] {".ubows"}, "Get basic server information about the Unturned Bunker Official Wikia Server.", true,
            "UNObot 2.4")]
        public async Task Ubows()
        {
            await CheckUnturned("108.61.100.48", 25444, UBOWServerLoggerService.GetSingleton().GetAverages());
        }

        [Command("unturnedreleasenotes", RunMode = RunMode.Async)]
        [Alias("urn")]
        [Help(new[] {".unturnedreleasenotes"}, "Find out what's in the latest release notes for Unturned.", true,
            "UNObot 3.1.7")]
        public async Task Urn()
        {
            await ReplyAsync(UnturnedReleaseNotes.GetLatestLink());
        }

        [Command("slamc", RunMode = RunMode.Async)]
        [Help(new[] {".slamc"}, "Get basic server information about the Slightly Less Average Minecraft server.", true,
            "UNObot 2.4")]
        public async Task Slamc()
        {
            var response = QueryHandlerService.GetInfoMC("williamle.com");
            if (response.ServerUp)
                await ReplyAsync(
                    $"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
        }

        [Command("pcreative", RunMode = RunMode.Async)]
        [Help(new[] {".pcreative"}, "Get basic server information about the pCreative Minecraft server.", true,
            "UNObot 2.4")]
        public async Task PCreative()
        {
            await CheckMCNew("williamle.com", 25432);
        }

        [Command("checkmc", RunMode = RunMode.Async)]
        [Help(new[] {".checkmc (ip) (port)"}, "Get basic server information about any Minecraft server.", true,
            "UNObot 2.4, UNObot 4.0.11")]
        public async Task CheckMCNew(string ip, ushort port = 25565)
        {
            var message = await ReplyAsync("I am now querying the server, please wait warmly...");
            try
            {
                var success = EmbedDisplayService.MinecraftQueryEmbed(ip, port, out var embed);
                if (!success || embed == null)
                {
                    await message.ModifyAsync(o =>
                        o.Content = "Error: Apparently we couldn't get any information about this server.");
                    return;
                }

                await message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for this server.", ex);
                await message.ModifyAsync(o =>
                    o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }

        [Command("psurvival", RunMode = RunMode.Async)]
        [Help(new[] {".psurvival"}, "Get basic server information about the pSurvival Minecraft server.", true,
            "UNObot 2.4")]
        public async Task PSurvival()
        {
            await CheckMCNew("williamle.com", 27285);
        }

        [Command("ouchies", RunMode = RunMode.Async)]
        [Help(new[] {".ouchies"}, "That must hurt.", true, "UNObot 4.0.12")]
        public async Task GetOuchies()
        {
            var message = await ReplyAsync("I am now querying the server, please wait warmly...");
            try
            {
                var success = EmbedDisplayService.OuchiesEmbed("williamle.com", 27285, out var embed);
                if (!success || embed == null)
                {
                    await message.ModifyAsync(o =>
                        o.Content = "Error: Apparently we couldn't get any information about this server.");
                    return;
                }

                await message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for this server.", ex);
                await message.ModifyAsync(o =>
                    o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }

        [Command("ouchies", RunMode = RunMode.Async)]
        [Help(new[] {".ouchies (Port)"}, "That must hurt.", true, "UNObot 4.0.12")]
        public async Task GetOuchies(ushort port)
        {
            var message = await ReplyAsync("I am now querying the server, please wait warmly...");
            try
            {
                var success = EmbedDisplayService.OuchiesEmbed("williamle.com", port, out var embed);
                if (!success || embed == null)
                {
                    await message.ModifyAsync(o =>
                        o.Content = "Error: Apparently we couldn't get any information about this server.");
                    return;
                }

                await message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for this server.", ex);
                await message.ModifyAsync(o =>
                    o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }

        [Command("locate", RunMode = RunMode.Async)]
        [Help(new[] {".locate"}, "¿Dónde están?", true, "UNObot 4.0.16")]
        public async Task GetLocations()
        {
            var message = await ReplyAsync("I am now querying the server, please wait warmly...");
            try
            {
                var success = EmbedDisplayService.LocationsEmbed("williamle.com", 27285, out var embed);
                if (!success || embed == null)
                {
                    await message.ModifyAsync(o =>
                        o.Content = "Error: Apparently we couldn't get any information about this server.");
                    return;
                }

                await message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for this server.", ex);
                await message.ModifyAsync(o =>
                    o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }

        [Command("locate", RunMode = RunMode.Async)]
        [Help(new[] {".locate (port)"}, "¿Dónde están?", true, "UNObot 4.0.16")]
        public async Task GetLocations(ushort port)
        {
            var message = await ReplyAsync("I am now querying the server, please wait warmly...");
            try
            {
                var success = EmbedDisplayService.LocationsEmbed("williamle.com", port, out var embed);
                if (!success || embed == null)
                {
                    await message.ModifyAsync(o =>
                        o.Content = "Error: Apparently we couldn't get any information about this server.");
                    return;
                }

                await message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for this server.", ex);
                await message.ModifyAsync(o =>
                    o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }

        [Command("expay", RunMode = RunMode.Async)]
        [Help(new[] {".expay (target) (amount)"}, "Where's my experience?", true, "UNObot 4.0.17")]
        public async Task GetLocations(string target, string amount)
        {
            var message = await ReplyAsync("I am now contacting the server, please wait warmly...");
            try
            {
                var success = EmbedDisplayService.TransferEmbed("williamle.com", 27285, Context.User.Id, target, amount,
                    out var embed);
                if (!success || embed == null)
                {
                    await message.ModifyAsync(o =>
                        o.Content = "We had some difficulties displaying the status. Please try again?");
                    return;
                }

                await message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for this server.", ex);
                await message.ModifyAsync(o =>
                    o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }

        [Command("expay", RunMode = RunMode.Async)]
        [Help(new[] {".expay (port) (target) (amount)"}, "Where's my experience?", true, "UNObot 4.0.17")]
        public async Task GetLocations(ushort port, string target, string amount)
        {
            var message = await ReplyAsync("I am now contacting the server, please wait warmly...");
            try
            {
                var success = EmbedDisplayService.TransferEmbed("williamle.com", port, Context.User.Id, target, amount,
                    out var embed);
                if (!success || embed == null)
                {
                    await message.ModifyAsync(o =>
                        o.Content = "We had some difficulties displaying the status. Please try again?");
                    return;
                }

                await message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for this server.", ex);
                await message.ModifyAsync(o =>
                    o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }

        [Command("mctime", RunMode = RunMode.Async)]
        [Help(new[] {".mctime"}, "SLEEP GUYS", true, "UNObot 4.0.16")]
        public async Task GetMCTime()
        {
            var server = QueryHandlerService.SpecialServers[27285];
            await RunRCON(server.Server, server.RCONPort, server.Password, "time query daytime");
        }

        [Command("mctime", RunMode = RunMode.Async)]
        [Help(new[] {".mctime (port)"}, "SLEEP GUYS", true, "UNObot 4.0.16")]
        public async Task GetMCTime(ushort port)
        {
            if (!QueryHandlerService.SpecialServers.ContainsKey(port))
            {
                await ReplyAsync("This is not a valid server port!");
                return;
            }

            var server = QueryHandlerService.SpecialServers[port];
            await RunRCON(server.Server, server.RCONPort, server.Password, "time query daytime");
        }

        [Command("unofficialwiki", RunMode = RunMode.Async)]
        [Alias("unwiki")]
        [Help(new[] {".unofficialwiki"}, "Get basic server information about the Unofficial Wikia Server.", true,
            "UNObot 2.4")]
        public async Task UnoffWiki()
        {
            await CheckUnturned("williamle.com", 27040);
        }

        [Command("checkunturned", RunMode = RunMode.Async)]
        [Alias("checku")]
        [Help(new[] {".checkunturned (ip) (port)"}, "Get basic server information about any Unturned server.", true,
            "UNObot 3.7")]
        public async Task CheckUnturnedServer(string ip, ushort port = 27015)
        {
            await CheckUnturned(ip, port);
        }

        [Command("rcon", RunMode = RunMode.Async)]
        [RequireOwner]
        [Help(new[] {".rcon (ip) (port) (password) (command)"},
            "Run a command on a remote server. Limited to DoggySazHi ATM.", true, "UNObot 4.0.12")]
        public async Task RunRCON(string ip, ushort port, string password, [Remainder] string command)
        {
            var message = await ReplyAsync("Executing...");
            var success = QueryHandlerService.SendRCON(ip, port, command, password, out var output);
            if (!success)
            {
                var error = "Failed to execute command. ";
                error += output.Status switch
                {
                    RCONStatus.ConnFail => "Is the server up, and the IP/port correct?",
                    RCONStatus.AuthFail => "Is the correct password used?",
                    RCONStatus.ExecFail => "Is the command valid, and authentication correct?",
                    RCONStatus.IntFail => "Something failed internally, blame DoggySazHi.",
                    RCONStatus.Success => "I lied. It worked, but Doggy broke the programming.",
                    _ => "I don't know what happened here."
                };
                await message.ModifyAsync(o => o.Content = error).ConfigureAwait(false);
            }
            else
            {
                var rconMessage = output.Data;
                if (string.IsNullOrWhiteSpace(rconMessage))
                    rconMessage = "Command executed successfully; server returned nothing.";
                if (rconMessage.Length > 1995 - TooLong.Length)
                    rconMessage = TooLong + rconMessage.Substring(0, 1995 - TooLong.Length);
                await message.ModifyAsync(o => o.Content = rconMessage).ConfigureAwait(false);
            }
        }

        [Command("rconexec", RunMode = RunMode.Async)]
        [RequireOwner]
        [Help(new[] {".rconexec (command)"},
            "Run a command on a remote server. Limited to DoggySazHi ATM.", true, "UNObot 4.0.12")]
        public async Task ExecRCON([Remainder] string command)
        {
            var message = await ReplyAsync("Executing...");
            var success = QueryHandlerService.ExecuteRCON(Context.User.Id, command, out var output);
            if (!success)
            {
                var error = "Failed to execute command. ";
                if (output == null)
                    error += "You did not open a connection with .rcon!";
                else
                    error += output.Status switch
                    {
                        RCONStatus.ConnFail => "Is the server up, and the IP/port correct?",
                        RCONStatus.AuthFail => "Is the correct password used?",
                        RCONStatus.ExecFail =>
                        "Is the command valid, and authentication correct? This should never appear.",
                        RCONStatus.IntFail => "Something failed internally, blame DoggySazHi.",
                        RCONStatus.Success => "I lied. It worked, but Doggy broke the programming.",
                        _ => "I don't know what happened here."
                    };

                await message.ModifyAsync(o => o.Content = error).ConfigureAwait(false);
            }
            else
            {
                var rconMessage = output.Data;
                if (string.IsNullOrWhiteSpace(rconMessage))
                    rconMessage = "Command executed successfully; server returned nothing.";
                if (rconMessage.Length > 1995 - TooLong.Length)
                    rconMessage = TooLong + rconMessage.Substring(0, 1995 - TooLong.Length);
                await message.ModifyAsync(o => o.Content = rconMessage).ConfigureAwait(false);
            }
        }

        [Command("rcon", RunMode = RunMode.Async)]
        [RequireOwner]
        [Help(new[] {".rcon (ip) (port) (password)"},
            "Run a command on a remote server. Limited to DoggySazHi ATM.", true, "UNObot 4.0.12")]
        public async Task RunRCON(string ip, ushort port, string password)
        {
            var message = await ReplyAsync("Initializing...");
            var success = QueryHandlerService.CreateRCON(ip, port, password, Context.User.Id, out var output);
            if (!success)
            {
                var error = "Failed to login. ";
                error += output.Status switch
                {
                    RCONStatus.ConnFail => "Is the server up, and the IP/port correct?",
                    RCONStatus.AuthFail => "Is the correct password used?",
                    RCONStatus.ExecFail =>
                    "Is the command valid, and authentication correct? This should never appear.",
                    RCONStatus.IntFail => "Something failed internally, blame DoggySazHi.",
                    RCONStatus.Success => "An existing RCON connection exists for your user. Please close it first.",
                    _ => "I don't know what happened here."
                };
                await message.ModifyAsync(o => o.Content = error).ConfigureAwait(false);
            }
            else
            {
                var rconMessage = output.Data;
                if (string.IsNullOrWhiteSpace(rconMessage))
                    rconMessage = "Connection created!";
                await message.ModifyAsync(o => o.Content = rconMessage).ConfigureAwait(false);
            }
        }

        [Command("rcon", RunMode = RunMode.Async)]
        [RequireOwner]
        [Help(new[] {".rcon (command)"},
            "Run a command on a remote server. Limited to DoggySazHi ATM.", true, "UNObot 4.0.12")]
        public async Task RunRCON(string trigger)
        {
            switch (trigger.ToLower().Trim())
            {
                case "close":
                    await CloseRCON();
                    break;
                case "status":
                    await GetRCON();
                    break;
                default:
                    await ReplyAsync("Invalid command. Use \"close\" or \"status\".");
                    break;
            }
        }

        private async Task CloseRCON()
        {
            var message = await ReplyAsync("Searching...");
            var success = QueryHandlerService.CloseRCON(Context.User.Id);
            if (!success)
                await message.ModifyAsync(o => o.Content = "Could not find an open connection owned by you.");
            else
                await message.ModifyAsync(o => o.Content = "Successfully closed your connection.");
        }

        private async Task GetRCON()
        {
            var message = await ReplyAsync("Searching...");
            var success = QueryHandlerService.ExecuteRCON(Context.User.Id, "", out var output);
            if (!success)
                await message.ModifyAsync(o => o.Content = "Could not find an open connection owned by you.");
            else
                await message.ModifyAsync(o =>
                    o.Content = $"Connected to {output.Server.Address} on {output.Server.Port}.");
        }

        public async Task CheckUnturned(string ip, ushort port = 27015, ServerAverages averages = null)
        {
            var message = await ReplyAsync("I am now querying the server, please wait warmly...");
            try
            {
                var success = EmbedDisplayService.UnturnedQueryEmbed(ip, port, out var embed, averages);
                if (!success || embed == null)
                {
                    await message.ModifyAsync(o =>
                        o.Content = "Error: Apparently we couldn't get any information about this server.");
                    return;
                }

                await message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for a server.", ex);
                await message.ModifyAsync(o =>
                    o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }
    }
}