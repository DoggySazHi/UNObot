using Discord.Commands;
using System;
using System.Threading.Tasks;
using Discord;
using UNObot.Services;
using static UNObot.Services.IRCON;

namespace UNObot.Modules
{
    public class ServerCommands : ModuleBase<SocketCommandContext>
    {
        [Command("ubows", RunMode = RunMode.Async), Alias("ubow")]
        [Help(new[] { ".ubows" }, "Get basic server information about the Unturned Bunker Official Wikia Server.", true, "UNObot 2.4")]
        public async Task UBOWS()
        {
            await CheckUnturned("108.61.100.48", 25444, UBOWServerLoggerService.GetSingleton().GetAverages());
        }

        [Command("unturnedreleasenotes", RunMode = RunMode.Async), Alias("urn")]
        [Help(new[] { ".unturnedreleasenotes" }, "Find out what's in the latest release notes for Unturned.", true, "UNObot 3.1.7")]
        public async Task URN()
        {
            await ReplyAsync(UnturnedReleaseNotes.GetLatestLink());
        }

        [Command("slamc", RunMode = RunMode.Async)]
        [Help(new[] { ".slamc" }, "Get basic server information about the Slightly Less Average Minecraft server.", true, "UNObot 2.4")]
        public async Task SLAMC()
        {
            var response = QueryHandlerService.GetInfoMC("williamle.com");
            if (response.ServerUp)
                await ReplyAsync($"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
        }

        [Command("pcreative", RunMode = RunMode.Async)]
        [Help(new[] { ".pcreative" }, "Get basic server information about the pCreative Minecraft server.", true, "UNObot 2.4")]
        public async Task PCreative()
        {
            await CheckMCNew("williamle.com", 25432);
        }

        [Command("checkmc", RunMode = RunMode.Async)]
        [Help(new[] { ".checkmc (ip) (port)" }, "Get basic server information about any Minecraft server.", true, "UNObot 2.4, UNObot 4.0.11")]
        public async Task CheckMCNew(string ip, ushort port = 25565)
        {
            var Message = await ReplyAsync("I am now querying the server, please wait warmly...");
            try
            {
                bool success = EmbedDisplayService.MinecraftQueryEmbed(ip, port, out var Embed);
                if (!success || Embed == null)
                {
                    await Message.ModifyAsync(o => o.Content = "Error: Apparently we couldn't get any information about this server.");
                    return;
                }
                await Message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = Embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for this server.", ex);
                await Message.ModifyAsync(o => o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }

        [Command("psurvival", RunMode = RunMode.Async)]
        [Help(new[] { ".psurvival" }, "Get basic server information about the pSurvival Minecraft server.", true, "UNObot 2.4")]
        public async Task PSurvival()
        {
            await CheckMCNew("williamle.com", 27285);
        }

        [Command("ouchies", RunMode = RunMode.Async)]
        [Help(new[] { ".ouchies" }, "That must hurt.", true, "UNObot 4.0.12")]
        public async Task GetOuchies()
        {
            var Message = await ReplyAsync("I am now querying the server, please wait warmly...");
            try
            {
                var success = EmbedDisplayService.OuchiesEmbed("williamle.com", 27285, out var Embed);
                if (!success || Embed == null)
                {
                    await Message.ModifyAsync(o => o.Content = "Error: Apparently we couldn't get any information about this server.");
                    return;
                }
                await Message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = Embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for this server.", ex);
                await Message.ModifyAsync(o => o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }
        
        [Command("ouchies", RunMode = RunMode.Async)]
        [Help(new[] { ".ouchies (Port)" }, "That must hurt.", true, "UNObot 4.0.12")]
        public async Task GetOuchies(ushort Port)
        {
            var Message = await ReplyAsync("I am now querying the server, please wait warmly...");
            try
            {
                var success = EmbedDisplayService.OuchiesEmbed("williamle.com", Port, out var Embed);
                if (!success || Embed == null)
                {
                    await Message.ModifyAsync(o => o.Content = "Error: Apparently we couldn't get any information about this server.");
                    return;
                }
                await Message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = Embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for this server.", ex);
                await Message.ModifyAsync(o => o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }

        [Command("locate", RunMode = RunMode.Async)]
        [Help(new[] { ".locate" }, "¿Dónde están?", true, "UNObot 4.0.16")]
        public async Task GetLocations()
        {
            var Message = await ReplyAsync("I am now querying the server, please wait warmly...");
            try
            {
                var success = EmbedDisplayService.LocationsEmbed("williamle.com", 27285, out var Embed);
                if (!success || Embed == null)
                {
                    await Message.ModifyAsync(o => o.Content = "Error: Apparently we couldn't get any information about this server.");
                    return;
                }
                await Message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = Embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for this server.", ex);
                await Message.ModifyAsync(o => o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }
        
        [Command("locate", RunMode = RunMode.Async)]
        [Help(new[] { ".locate (port)" }, "¿Dónde están?", true, "UNObot 4.0.16")]
        public async Task GetLocations(ushort Port)
        {
            var Message = await ReplyAsync("I am now querying the server, please wait warmly...");
            try
            {
                bool success = EmbedDisplayService.LocationsEmbed("williamle.com", Port, out var Embed);
                if (!success || Embed == null)
                {
                    await Message.ModifyAsync(o => o.Content = "Error: Apparently we couldn't get any information about this server.");
                    return;
                }
                await Message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = Embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for this server.", ex);
                await Message.ModifyAsync(o => o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }

        [Command("expay", RunMode = RunMode.Async)]
        [Help(new[] { ".expay (target) (amount)" }, "Where's my experience?", true, "UNObot 4.0.17")]
        public async Task GetLocations(string Target, string Amount)
        {
            var Message = await ReplyAsync("I am now contacting the server, please wait warmly...");
            try
            {
                var success = EmbedDisplayService.TransferEmbed("williamle.com", 27285, Context.User.Id, Target, Amount, out var Embed);
                if (!success || Embed == null)
                {
                    await Message.ModifyAsync(o => o.Content = "We had some difficulties displaying the status. Please try again?");
                    return;
                }
                await Message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = Embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for this server.", ex);
                await Message.ModifyAsync(o => o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }
        
        [Command("expay", RunMode = RunMode.Async)]
        [Help(new[] { ".expay (port) (target) (amount)" }, "Where's my experience?", true, "UNObot 4.0.17")]
        public async Task GetLocations(ushort Port, string Target, string Amount)
        {
            var Message = await ReplyAsync("I am now contacting the server, please wait warmly...");
            try
            {
                
                var success = EmbedDisplayService.TransferEmbed("williamle.com", Port, Context.User.Id, Target, Amount, out var Embed);
                if (!success || Embed == null)
                {
                    await Message.ModifyAsync(o => o.Content = "We had some difficulties displaying the status. Please try again?");
                    return;
                }
                await Message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = Embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for this server.", ex);
                await Message.ModifyAsync(o => o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }

        [Command("mctime", RunMode = RunMode.Async)]
        [Help(new[] { ".mctime" }, "SLEEP GUYS", true, "UNObot 4.0.16")]
        public async Task GetMCTime()
        {
            var Server = QueryHandlerService.SpecialServers[27285];
            await RunRCON(Server.Server, Server.RCONPort, Server.Password, "time query daytime");
        }
        
        [Command("mctime", RunMode = RunMode.Async)]
        [Help(new[] { ".mctime (port)" }, "SLEEP GUYS", true, "UNObot 4.0.16")]
        public async Task GetMCTime(ushort Port)
        {
            if (!QueryHandlerService.SpecialServers.ContainsKey(Port))
            {
                await ReplyAsync("This is not a valid server port!");
                return;
            }
            var Server = QueryHandlerService.SpecialServers[Port];
            await RunRCON(Server.Server, Server.RCONPort, Server.Password, "time query daytime");
        }

        [Command("unofficialwiki", RunMode = RunMode.Async), Alias("unwiki")]
        [Help(new[] { ".unofficialwiki" }, "Get basic server information about the Unofficial Wikia Server.", true, "UNObot 2.4")]
        public async Task UnoffWiki()
        {
            await CheckUnturned("williamle.com", 27040);
        }

        [Command("checkunturned", RunMode = RunMode.Async), Alias("checku")]
        [Help(new[] { ".checkunturned (ip) (port)" }, "Get basic server information about any Unturned server.", true, "UNObot 3.7")]
        public async Task CheckUnturnedServer(string ip, ushort port = 27015)
        {
            await CheckUnturned(ip, port);
        }

        private const string TooLong = "[Message is too long; trimmed.]\n";

        [Command("rcon", RunMode = RunMode.Async)]
        [RequireOwner]
        [Help(new[] {".rcon (ip) (port) (password) (command)"},
            "Run a command on a remote server. Limited to DoggySazHi ATM.", true, "UNObot 4.0.12")]
        public async Task RunRCON(string IP, ushort Port, string Password, [Remainder] string Command)
        {
            var Message = await ReplyAsync("Executing...");
            var Success = QueryHandlerService.SendRCON(IP, Port, Command, Password, out var Output);
            if (!Success)
            {
                var Error = "Failed to execute command. ";
                Error += Output.Status switch
                {
                    RCONStatus.CONN_FAIL => "Is the server up, and the IP/port correct?",
                    RCONStatus.AUTH_FAIL => "Is the correct password used?",
                    RCONStatus.EXEC_FAIL => "Is the command valid, and authentication correct?",
                    RCONStatus.INT_FAIL => "Something failed internally, blame DoggySazHi.",
                    RCONStatus.SUCCESS => "I lied. It worked, but Doggy broke the programming.",
                    _ => "I don't know what happened here."
                };
                await Message.ModifyAsync(o => o.Content = Error).ConfigureAwait(false);
            }
            else
            {
                var RCONMessage = Output.Data;
                if (string.IsNullOrWhiteSpace(RCONMessage))
                    RCONMessage = "Command executed successfully; server returned nothing.";
                if (RCONMessage.Length > 1995 - TooLong.Length)
                    RCONMessage = TooLong + RCONMessage.Substring(0, 1995 - TooLong.Length);
                await Message.ModifyAsync(o => o.Content = RCONMessage).ConfigureAwait(false);
            }
        }

        [Command("rconexec", RunMode = RunMode.Async)]
        [RequireOwner]
        [Help(new[] {".rconexec (command)"},
            "Run a command on a remote server. Limited to DoggySazHi ATM.", true, "UNObot 4.0.12")]
        public async Task ExecRCON([Remainder] string command)
        {
            var Message = await ReplyAsync("Executing...");
            var Success = QueryHandlerService.ExecuteRCON(Context.User.Id, command, out var Output);
            if (!Success)
            {
                var Error = "Failed to execute command. ";
                if (Output == null)
                {
                    Error += "You did not open a connection with .rcon!";
                }
                else
                {
                    Error += Output.Status switch
                    {
                        RCONStatus.CONN_FAIL => "Is the server up, and the IP/port correct?",
                        RCONStatus.AUTH_FAIL => "Is the correct password used?",
                        RCONStatus.EXEC_FAIL => "Is the command valid, and authentication correct? This should never appear.",
                        RCONStatus.INT_FAIL => "Something failed internally, blame DoggySazHi.",
                        RCONStatus.SUCCESS => "I lied. It worked, but Doggy broke the programming.",
                        _ => "I don't know what happened here."
                    };
                }
                
                await Message.ModifyAsync(o => o.Content = Error).ConfigureAwait(false);
            }
            else
            {
                var RCONMessage = Output.Data;
                if (string.IsNullOrWhiteSpace(RCONMessage))
                    RCONMessage = "Command executed successfully; server returned nothing.";
                if (RCONMessage.Length > 1995 - TooLong.Length)
                    RCONMessage = TooLong + RCONMessage.Substring(0, 1995 - TooLong.Length);
                await Message.ModifyAsync(o => o.Content = RCONMessage).ConfigureAwait(false);
            }
        }

        [Command("rcon", RunMode = RunMode.Async)]
        [RequireOwner]
        [Help(new[] {".rcon (ip) (port) (password)"},
            "Run a command on a remote server. Limited to DoggySazHi ATM.", true, "UNObot 4.0.12")]
        public async Task RunRCON(string IP, ushort Port, string Password)
        {
            var Message = await ReplyAsync("Initializing...");
            var Success = QueryHandlerService.CreateRCON(IP, Port, Password, Context.User.Id, out var Output);
            if (!Success)
            {
                var Error = "Failed to login. ";
                Error += Output.Status switch
                {
                    RCONStatus.CONN_FAIL => "Is the server up, and the IP/port correct?",
                    RCONStatus.AUTH_FAIL => "Is the correct password used?",
                    RCONStatus.EXEC_FAIL => "Is the command valid, and authentication correct? This should never appear.",
                    RCONStatus.INT_FAIL => "Something failed internally, blame DoggySazHi.",
                    RCONStatus.SUCCESS => "An existing RCON connection exists for your user. Please close it first.",
                    _ => "I don't know what happened here."
                };
                await Message.ModifyAsync(o => o.Content = Error).ConfigureAwait(false);
            }
            else
            {
                var RCONMessage = Output.Data;
                if (string.IsNullOrWhiteSpace(RCONMessage))
                    RCONMessage = "Connection created!";
                await Message.ModifyAsync(o => o.Content = RCONMessage).ConfigureAwait(false);
            }
        }

        [Command("rcon", RunMode = RunMode.Async)]
        [RequireOwner]
        [Help(new[] {".rcon (command)"},
            "Run a command on a remote server. Limited to DoggySazHi ATM.", true, "UNObot 4.0.12")]
        public async Task RunRCON(string Trigger)
        {
            switch (Trigger.ToLower().Trim())
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
            var Message = await ReplyAsync("Searching...");
            var Success = QueryHandlerService.CloseRCON(Context.User.Id);
            if (!Success)
            {
                await Message.ModifyAsync(o => o.Content = "Could not find an open connection owned by you.");
            }
            else
            {
                await Message.ModifyAsync(o => o.Content = "Successfully closed your connection.");
            }
        }

        private async Task GetRCON()
        {
            var Message = await ReplyAsync("Searching...");
            var Success = QueryHandlerService.ExecuteRCON(Context.User.Id, "", out var Output);
            if (!Success)
            {
                await Message.ModifyAsync(o => o.Content = "Could not find an open connection owned by you.");
            }
            else
            {
                await Message.ModifyAsync(o => o.Content = $"Connected to {Output.Server.Address} on {Output.Server.Port}.");
            }
        }

        public async Task CheckUnturned(string ip, ushort port = 27015, ServerAverages Averages = null)
        {
            var Message = await ReplyAsync("I am now querying the server, please wait warmly...");
            try
            {
                bool success = EmbedDisplayService.UnturnedQueryEmbed(ip, port, out var Embed, Averages);
                if (!success || Embed == null)
                {
                    await Message.ModifyAsync(o => o.Content = "Error: Apparently we couldn't get any information about this server.");
                    return;
                }
                await Message.ModifyAsync(o =>
                {
                    o.Content = "";
                    o.Embed = Embed;
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogSeverity.Error, "Error loading embeds for a server.", ex);
                await Message.ModifyAsync(o => o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }
    }
}
