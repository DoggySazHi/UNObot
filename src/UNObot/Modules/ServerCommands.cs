using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UNObot.Services;

namespace UNObot.Modules
{
    public class ServerCommands : ModuleBase<SocketCommandContext>
    {
        [Command("ubows", RunMode = RunMode.Async), Alias("ubow")]
        [Help(new string[] { ".ubows" }, "Get basic server information about the Unturned Bunker Official Wikia Server.", true, "UNObot 2.4")]
        public async Task UBOWS()
        {
            await CheckUnturned("108.61.100.48", 25444, UBOWServerLoggerService.GetSingleton().GetAverages());
        }

        [Command("unturnedreleasenotes", RunMode = RunMode.Async), Alias("urn")]
        [Help(new string[] { ".unturnedreleasenotes" }, "Find out what's in the latest release notes for Unturned.", true, "UNObot 3.1.7")]
        public async Task URN()
        {
            await ReplyAsync(UnturnedReleaseNotes.GetLatestLink());
        }

        [Command("slamc", RunMode = RunMode.Async)]
        [Help(new string[] { ".slamc" }, "Get basic server information about the Slightly Less Average Minecraft server.", true, "UNObot 2.4")]
        public async Task SLAMC()
        {
            var response = QueryHandlerService.GetInfoMC("23.243.79.108");
            if (response.ServerUp)
                await ReplyAsync($"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
        }

        [Command("psurvival", RunMode = RunMode.Async)]
        [Help(new string[] { ".psurvival" }, "Get basic server information about the pSurvival Minecraft server.", true, "UNObot 2.4")]
        public async Task PSurvival()
        {
            var response = QueryHandlerService.GetInfoMC("23.243.79.108", 25432);
            if (response.ServerUp)
                await ReplyAsync($"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
        }
        [Command("checkmc", RunMode = RunMode.Async)]
        [Help(new string[] { ".checkmc (ip) (port)" }, "Get basic server information about any Minecraft server.", true, "UNObot 2.4")]
        public async Task CheckMC(string ip, ushort port)
        {
            var response = QueryHandlerService.GetInfoMC(ip, port);
            if (response.ServerUp)
                await ReplyAsync($"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
        }

        [Command("unofficialwiki", RunMode = RunMode.Async), Alias("unwiki")]
        [Help(new string[] { ".unofficialwiki" }, "Get basic server information about the Unofficial Wikia Server.", true, "UNObot 2.4")]
        public async Task UnoffWiki()
        {
            await CheckUnturned("23.243.79.108", 27040);
        }

        [Command("checkunturned", RunMode = RunMode.Async), Alias("checku")]
        [Help(new string[] { ".checkunturned (ip) (port)" }, "Get basic server information about any Unturned server.", true, "UNObot 3.7")]
        public async Task CheckUnturned(string ip, ushort port = 27015)
        {
            await CheckUnturned(ip, port, null);
        }

        public async Task CheckUnturned(string ip, ushort port = 27015, ServerAverages Averages = null)
        {
            var Message = await ReplyAsync("I am now querying the server, please wait warmly...");
            try
            {
                bool success = EmbedDisplayService.UnturnedQueryEmbed(Context.Guild.Id, ip, port, out var Embed, Averages);
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
            catch(Exception)
            {
                await Message.ModifyAsync(o => o.Content = "We had some difficulties displaying the status. Please try again?");
            }
        }
    }
}
