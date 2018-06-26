using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordBot.Modules
{
    public class Testingcmds : ModuleBase<SocketCommandContext>
    {
        [Command("purge"),RequireUserPermission(GuildPermission.Administrator)]
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
            const int delay = 5000;
            var m = await ReplyAsync($"Purge completed. _This message will be deleted in {delay / 1000} seconds._");
            await Task.Delay(delay);
            await m.DeleteAsync();
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
    }
}