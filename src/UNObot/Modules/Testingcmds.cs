using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace UNOBot.Modules
{
    public class Testingcmds : ModuleBase<SocketCommandContext>
    {
        [Command("purge"),RequireUserPermission(GuildPermission.Administrator), RequireBotPermission(ChannelPermission.ManageMessages)]
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
            if(Context.User.Id != 191397590946807809)
            {
                await ReplyAsync("You can only exit if you're DoggySazHi.");
                return;
            }
            await ReplyAsync("Sorry to be a hassle. Goodbye world!");
            Environment.Exit(0);
        }
        [Command("ubows"), Alias("ubow")]
        public async Task UBOWS()
        {
            bool success = UNObot.Modules.QueryHandler.GetInfo("108.61.100.48", 25445, out UNObot.Modules.A2S_INFO response);
            if(!success)
            {
                await ReplyAsync("Error: Apparently we couldn't get any information about UBOWS.");
                return;
            }
            await ReplyAsync($"Name: {response.Name}\n" +
                             $"Players: {Convert.ToInt32(response.Players)}/{Convert.ToInt32(response.MaxPlayers)}\n" +
                             $"Map: {response.Map}");
        }
    }
}