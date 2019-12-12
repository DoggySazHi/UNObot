using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

#pragma warning disable CS1701 // Assuming assembly reference matches identity
#pragma warning disable CS1702 // Assuming assembly reference matches identity

namespace UNObot.Modules
{
    public class Testingcmds : ModuleBase<SocketCommandContext>
    {
        [Command("purge", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.Administrator), RequireBotPermission(ChannelPermission.ManageMessages)]
        [Help(new string[] { ".purge (number of messages)" }, "Delete messages via a range. Testing command; do not rely on forever.", false, "UNObot 1.4")]
        public async Task Purge(int length)
        {
            var messages = await Context.Channel.GetMessagesAsync(length + 1).FlattenAsync();

            if (!(Context.Channel is ITextChannel textchannel))
            {
                Console.WriteLine("error cast");
                return;
            }
            await textchannel.DeleteMessagesAsync(messages);
        }

        [Command("ubows", RunMode = RunMode.Async), Alias("ubow")]
        [Help(new string[] { ".ubows" }, "Get basic server information about the Unturned Bunker Official Wikia Server.", true, "UNObot 2.4")]
        public async Task UBOWS()
        {
            //add one for query port
            bool success = QueryHandler.GetInfo("108.61.100.48", 25445, out A2S_INFO response);
            if (!success)
            {
                await ReplyAsync("Error: Apparently we couldn't get any information about UBOWS.");
                return;
            }
            if (response.Map == "Carpat")
                response.Map = "~~Carpat~~ **Carpet**";
            int players = Convert.ToInt32(response.Players);
            if (players == 0 && new Random().Next(0, 5) == 0)
                players = -1;
            await ReplyAsync($"Name: {response.Name}\n" +
                             $"Players: {players}/{Convert.ToInt32(response.MaxPlayers)}\n" +
                             $"Map: {response.Map}\n" +
                             $"IP: 108.61.100.48\n" +
                             $"Port: {response.Port}");
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
            var response = QueryHandler.GetInfoMC("23.243.79.108");
            if (response.ServerUp)
                await ReplyAsync($"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
        }

        [Command("psurvival", RunMode = RunMode.Async)]
        [Help(new string[] { ".psurvival" }, "Get basic server information about the pSurvival Minecraft server.", true, "UNObot 2.4")]
        public async Task PSurvival()
        {
            var response = QueryHandler.GetInfoMC("23.243.79.108", 25432);
            if (response.ServerUp)
                await ReplyAsync($"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
        }
        [Command("checkmc", RunMode = RunMode.Async)]
        [Help(new string[] { ".checkmc (ip) (port)" }, "Get basic server information about any Minecraft server.", true, "UNObot 2.4")]
        public async Task CheckMC(string ip, ushort port)
        {
            var response = QueryHandler.GetInfoMC(ip, port);
            if (response.ServerUp)
                await ReplyAsync($"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
        }

        [Command("unofficialwiki", RunMode = RunMode.Async), Alias("unwiki")]
        [Help(new string[] { ".unofficialwiki" }, "Get basic server information about the Unofficial Wikia Server.", true, "UNObot 2.4")]
        public async Task UnoffWiki()
        {
            bool success = QueryHandler.GetInfo("23.243.79.108", 27041, out A2S_INFO response);
            if (!success)
            {
                await ReplyAsync("Error: Apparently we couldn't get any information about the Unofficial Wiki Server.");
                return;
            }
            await ReplyAsync($"Name: {response.Name}\n" +
                             $"Players: {Convert.ToInt32(response.Players)}/{Convert.ToInt32(response.MaxPlayers)}\n" +
                             $"Map: {response.Map}");
        }

        [Command("checkunturned", RunMode = RunMode.Async), Alias("checku")]
        [Help(new string[] { ".checkunturned (ip) (port)" }, "Get basic server information about any Unturned server.", true, "UNObot 3.7")]
        public async Task CheckUnturned(string ip, ushort port = 27015)
        {
            // query port = 1+ normal port
            bool success = QueryHandler.GetInfo(ip, ++port, out A2S_INFO response);
            if (!success)
            {
                await ReplyAsync("Error: Apparently we couldn't get any information about this server. Use the normal (join) port, as this automatically takes care of query ports.");
                return;
            }
            await ReplyAsync($"Name: {response.Name}\n" +
                             $"Players: {Convert.ToInt32(response.Players)}/{Convert.ToInt32(response.MaxPlayers)}\n" +
                             $"Map: {response.Map}");
        }

        [Command("helpmeplz", RunMode = RunMode.Async), RequireOwner]
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

        [Command("moltthink", RunMode = RunMode.Async)]
        [Help(new string[] { ".moltthink" }, "Think like Molt.", true, "UNObot 3.0 Beta 1")]
        public async Task MoltThink()
        {
            await ReplyAsync("<:moltthink:471842854591791104>");
        }
        [Command("moltthinkreact", RunMode = RunMode.Async)]
        [Help(new string[] { ".moltthinkreact" }, "React by thinking as Molt.", true, "UNObot 3.0 Beta 1")]
        public async Task MoltThinkReact()
            => await MoltThinkReact(1);

        [Command("moltthinkreact", RunMode = RunMode.Async)]
        [Help(new string[] { ".moltthinkreact (number of messages)" }, "React by thinking as Molt.", true, "UNObot 3.0 Beta 1")]
        public async Task MoltThinkReact(int numMessages)
        {
            IEmote emote = await Context.Client.GetGuild(420005591155605535).GetEmoteAsync(471842854591791104);
            await BaseReact(numMessages, emote);
        }

        [Command("oof", RunMode = RunMode.Async)]
        [Help(new string[] { ".oof" }, "Oof.", true, "UNObot 3.0 Beta 1")]
        public async Task OOF()
        {
            await ReplyAsync("<:oof:559961296418635776>");
        }

        [Command("oofreact", RunMode = RunMode.Async)]
        [Help(new string[] { ".oofreact" }, "Damn, oof.", true, "UNObot 3.0 Beta 1")]
        public async Task OOFReact()
            => await OOFReact(1);

        [Command("oofreact", RunMode = RunMode.Async)]
        [Help(new string[] { ".oofreact (number of messages)" }, "Damn, oof.", true, "UNObot 3.0 Beta 1")]
        public async Task OOFReact(int numMessages)
        {
            IEmote emote = await Context.Client.GetGuild(420005591155605535).GetEmoteAsync(559961296418635776);
            await BaseReact(numMessages, emote);
        }

        [Command("calculateemote", RunMode = RunMode.Async)]
        public async Task CalculateEmote([Remainder] string Input)
        {
            await ReplyAsync($"Server: ``{Context.Guild.Id}`` Emote: ``{Input}``").ConfigureAwait(false);
        }

        [Command("emote", RunMode = RunMode.Async)]
        public async Task Emote(ulong Server, ulong Emote)
        {
            try
            {
                IEmote emote = await Context.Client.GetGuild(Server).GetEmoteAsync(Emote);
                await ReplyAsync(emote.ToString());
            }
            catch(Exception)
            {
                await ReplyAsync("Failed to get emote!");
            }
        }

        [Command("emotereact", RunMode = RunMode.Async)]
        public async Task EmoteReact(ulong Server, ulong Emote, int numMessages)
        {
            try
            {
                IEmote emote = await Context.Client.GetGuild(Server).GetEmoteAsync(Emote);
                await BaseReact(numMessages, emote);
            }
            catch(Exception)
            {
                await ReplyAsync("Failed to get emote!");
            }
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
        public async Task Translate(string From, string To, [Remainder] string Message)
        {
            
        }

        /*
        [Command("getbuttons", RunMode = RunMode.Async)]
        [Help(new string[] { "yes." }, "", false, "no")]
        public async Task AddButtons()
        {
            var message = await ReplyAsync("Loading buttons...");
            await InputHandler.AddReactions(message);
            await message.ModifyAsync(o => o.Content = "Finished loading buttons!");
        }
        */

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