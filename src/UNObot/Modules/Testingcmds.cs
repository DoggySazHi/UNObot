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
            var messages = await Context.Channel.GetMessagesAsync(length + 1).Flatten();

            await Context.Channel.DeleteMessagesAsync(messages);
            const int delay = 5000;
            var m = await ReplyAsync($"Purge completed. _This message will be deleted in {delay / 1000} seconds._");
            await Task.Delay(delay);
            await m.DeleteAsync();
        }
        [Command("pfpsteal")]
        public Task Pfpsteal(string user)
        {
            user = user.Trim(new Char[] { ' ', '<', '>', '!', '@' });
            if (!UInt64.TryParse(user, out ulong userid))
                return ReplyAsync("Mention the player with this command to see their stats.");
            Discord.WebSocket.DiscordSocketClient discordSocketClient = new Discord.WebSocket.DiscordSocketClient();
            Discord.WebSocket.SocketUser newuser = discordSocketClient.GetUser(userid);
            if (newuser == null)
                return ReplyAsync($"The user does not exist; did you type it wrong?");
            else
                return ReplyAsync($"<@{userid}>'s Profile Picture Link: {newuser.GetAvatarUrl()}");
        }
    }
}