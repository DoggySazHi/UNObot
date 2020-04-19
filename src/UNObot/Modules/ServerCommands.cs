using Discord.Commands;
using System;
using System.Threading.Tasks;
using Discord;
using UNObot.Services;

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

        [Command("psurvival", RunMode = RunMode.Async)]
        [Help(new[] { ".psurvival" }, "Get basic server information about the pSurvival Minecraft server.", true, "UNObot 2.4")]
        public async Task PSurvival()
        {
            var response = QueryHandlerService.GetInfoMC("williamle.com", 25432);
            if (response.ServerUp)
                await ReplyAsync($"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
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

        [Command("ouchies", RunMode = RunMode.Async)]
        [Help(new[] { ".ouchies" }, "That must hurt.", true, "UNObot 4.0.12")]
        public async Task GetOuchies()
        {
            var Message = await ReplyAsync("I am now querying the server, please wait warmly...");
            try
            {
                bool success = EmbedDisplayService.OuchiesEmbed("192.168.2.6", 27285, out var Embed);
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

        [Command("rcon", RunMode = RunMode.Async)]
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
                    MinecraftRCON.RCONStatus.CONN_FAIL => "Is the server up, and the IP/port correct?",
                    MinecraftRCON.RCONStatus.AUTH_FAIL => "Is the correct password used?",
                    MinecraftRCON.RCONStatus.EXEC_FAIL => "Is the command valid, and authentication correct?",
                    MinecraftRCON.RCONStatus.INT_FAIL => "Something failed internally, blame DoggySazHi.",
                    MinecraftRCON.RCONStatus.SUCCESS => "I lied. It worked, but Doggy broke the programming.",
                    _ => "I don't know what happened here."
                };
                await Message.ModifyAsync(o => o.Content = Error).ConfigureAwait(false);
            }
            else
            {
                await Message.ModifyAsync(o => o.Content = Output.Data).ConfigureAwait(false);
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
