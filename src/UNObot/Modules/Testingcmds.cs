using System;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using UNObot.Modules;

namespace UNOBot.Modules
{
    public class Testingcmds : ModuleBase<SocketCommandContext>
    {
        [Command("purge"), RequireUserPermission(GuildPermission.Administrator), RequireBotPermission(ChannelPermission.ManageMessages)]
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
        public async Task SLAMC()
        {
            var response = QueryHandler.GetInfoMC("23.243.79.108");
            if (response.ServerUp)
                await ReplyAsync($"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
        }

        [Command("psurvival")]
        public async Task PSurvival()
        {
            var response = QueryHandler.GetInfoMC("23.243.79.108", 25432);
            if (response.ServerUp)
                await ReplyAsync($"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
        }
        [Command("checkmc")]
        public async Task CheckMC(string ip, ushort port)
        {
            var response = QueryHandler.GetInfoMC(ip, port);
            if (response.ServerUp)
                await ReplyAsync($"Current players: {response.CurrentPlayers}/{response.MaximumPlayers}\nCurrently running on {response.Version}.");
            else
                await ReplyAsync("The server seems to be down from here...");
        }

        [Command("unofficialwiki"), Alias("unwiki")]
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
        /*
        [Command("helpme"), RequireOwner]
        public async Task TestPerm1()
        {
            var messages = await Context.Channel.GetMessagesAsync(1000).FlattenAsync();

            ITextChannel textchannel = Context.Channel as ITextChannel;
            if (textchannel == null)
            {
                Console.WriteLine("error cast");
                return;
            }
            await textchannel.DeleteMessagesAsync(messages);
        }
        */
        [Command("helpmeplz"), RequireOwner]
        public async Task HelpmePlz(int length)
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