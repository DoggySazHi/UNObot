using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using UNObot.Modules;

namespace UNOBot.Modules
{
    public class Testingcmds : ModuleBase<SocketCommandContext>
    {
        [Command("purge"), RequireUserPermission(GuildPermission.Administrator), RequireBotPermission(ChannelPermission.ManageMessages)]
        [Help(new string[] { ".purge (number of messages)" }, "Delete messages via a range. Testing command; do not rely on forever.", false, "UNObot 1.4")]
        public async Task Purge(int length)
        {
            var messages = await Context.Channel.GetMessagesAsync(length + 1).FlattenAsync();

            ITextChannel textchannel = Context.Channel as ITextChannel;
            if (textchannel == null)
            {
                Console.WriteLine("error cast");
                return;
            }
            await textchannel.DeleteMessagesAsync(messages);
        }
        [Command("exit")]
        [Help(new string[] { ".exit" }, "Kill UNObot when he's being a bad bot. Only Doggy and other \"special\" people can do it.", false, "UNObot 1.4")]
        public async Task Exit()
        {
            switch (Context.User.Id)
            {
                case 191397590946807809:
                    break;
                case 278524552462598145:
                    await ReplyAsync("Wait, this isn't Doggy... eh who cares?");
                    break;
                default:
                    await ReplyAsync("You can only exit if you're DoggySazHi.");
                    return;
            }
            await ReplyAsync("Sorry to be a hassle. Goodbye world!");
            Environment.Exit(0);
        }
        [Command("ubows"), Alias("ubow")]
        [Help(new string[] { ".ubows" }, "Get basic server information about the Unturned Bunker Official Wikia Server.", true, "UNObot 2.4")]
        public async Task UBOWS()
        {
            bool success = QueryHandler.GetInfo("108.61.100.48", 25445, out A2S_INFO response);
            if (!success)
            {
                await ReplyAsync("Error: Apparently we couldn't get any information about UBOWS.");
                return;
            }
            if (response.Map == "Carpat")
                response.Map = "~~Carpat~~ **Carpet**";
            await ReplyAsync($"Name: {response.Name}\n" +
                             $"Players: {Convert.ToInt32(response.Players)}/{Convert.ToInt32(response.MaxPlayers)}\n" +
                             $"Map: {response.Map}");
        }

        [Command("slamc")]
        [Help(new string[] { ".slamc" }, "Get basic server information about the Slightly Less Average Minecraft server.", true, "UNObot 2.4")]
        public async Task SLAMC()
        {
            var response = QueryHandler.GetInfoMC("23.243.79.108");
            if (response.ServerUp)
                await ReplyAsync($"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
        }

        [Command("psurvival")]
        [Help(new string[] { ".psurvival" }, "Get basic server information about the pSurvival Minecraft server.", true, "UNObot 2.4")]
        public async Task PSurvival()
        {
            var response = QueryHandler.GetInfoMC("23.243.79.108", 25432);
            if (response.ServerUp)
                await ReplyAsync($"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
        }
        [Command("checkmc")]
        [Help(new string[] { ".checkmc (ip) (port)" }, "Get basic server information about any Minecraft server.", true, "UNObot 2.4")]
        public async Task CheckMC(string ip, ushort port)
        {
            var response = QueryHandler.GetInfoMC(ip, port);
            if (response.ServerUp)
                await ReplyAsync($"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
        }

        [Command("unofficialwiki"), Alias("unwiki")]
        [Help(new string[] { ".unofficialwiki" }, "Get basic server information about the Unofficial Wikia Server.", true, "UNObot 2.4")]
        public async Task UnoffWiki()
        {
            bool success = QueryHandler.GetInfo("23.243.79.108", 27041, out UNObot.Modules.A2S_INFO response);
            if (!success)
            {
                await ReplyAsync("Error: Apparently we couldn't get any information about the Unofficial Wiki Server.");
                return;
            }
            await ReplyAsync($"Name: {response.Name}\n" +
                             $"Players: {Convert.ToInt32(response.Players)}/{Convert.ToInt32(response.MaxPlayers)}\n" +
                             $"Map: {response.Map}");
        }

        [Command("helpmeplz"), RequireOwner]
        public async Task HelpmePlz(int length)
        {
            var messages = await Context.Channel.GetMessagesAsync(length + 1).FlattenAsync();

            if (!(Context.Channel is ITextChannel textchannel))
            {
                Console.WriteLine("error cast");
                return;
            }
            await textchannel.DeleteMessagesAsync(messages);
        }

        [Command("moltthink")]
        [Help(new string[] { ".moltthink" }, "Think like Molt.", true, "UNObot 3.0 Beta 1")]
        public async Task MoltThink()
        {
            await ReplyAsync("<:moltthink:471842854591791104>");
        }
        [Command("moltthinkreact")]
        [Help(new string[] { ".moltthinkreact" }, "React by thinking as Molt.", true, "UNObot 3.0 Beta 1")]
        public async Task MoltThinkReact()
            => await MoltThinkReact(1);

        [Command("moltthinkreact")]
        [Help(new string[] { ".moltthinkreact (number of messages)" }, "React by thinking as Molt.", true, "UNObot 3.0 Beta 1")]
        public async Task MoltThinkReact(int numMessages)
        {
            IEmote emote = await Context.Client.GetGuild(420005591155605535).GetEmoteAsync(471842854591791104);
            await BaseReact(numMessages, emote);
        }

        [Command("oof")]
        [Help(new string[] { ".oof" }, "Oof.", true, "UNObot 3.0 Beta 1")]
        public async Task OOF()
        {
            await ReplyAsync("<:oof:443773918319476757>");
        }

        [Command("oofreact")]
        [Help(new string[] { ".oofreact" }, "Damn, oof.", true, "UNObot 3.0 Beta 1")]
        public async Task OOFReact()
            => await OOFReact(1);

        [Command("oofreact")]
        [Help(new string[] { ".oofreact (number of messages)" }, "Damn, oof.", true, "UNObot 3.0 Beta 1")]
        public async Task OOFReact(int numMessages)
        {
            IEmote emote = await Context.Client.GetGuild(420005591155605535).GetEmoteAsync(443773918319476757);
            await BaseReact(numMessages, emote);
        }

        public async Task BaseReact(int numMessages, IEmote emote)
        {
            var messages = await Context.Channel.GetMessagesAsync(numMessages + 1).FlattenAsync();
            var message = messages.Last();

            if (!(message is IUserMessage updatedMessage))
            {
                await ReplyAsync("Couldn't add reaction!");
                return;
            }
            //IEmote emote = await Context.Client.GetGuild(420005591155605535).GetEmoteAsync(471842854591791104);
            //Emote emote = Emote emote = Emote.Parse("<:dotnet:232902710280716288>");
            //Emoji emoji = new Emoji("👍");
            await updatedMessage.AddReactionAsync(emote);
            await Purge(0);
        }
        /*
        Timer spamTimer = new Timer();
        ulong server = 0;

        [Command("enablespam")]
        public async Task StartSpam()
        {
            spamTimer = new Timer
            {
                Interval = 60000,
                AutoReset = true,
            };
            spamTimer.Elapsed += Spam;
            spamTimer.Start();
            server = Context.Guild.Id;
            await ReplyAsync("Started timer!");
        }

        async void Spam(object sender, ElapsedEventArgs e)
        {
            if (server == 0)
                spamTimer.Dispose();
            else
                await UNObot.Program.SendMessage("AHHHHHHH", server);
        }
        */
    }
}