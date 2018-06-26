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