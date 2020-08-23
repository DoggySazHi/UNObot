using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace UNObot.Plugins.Helpers
{
    public static class PluginHelper
    {
        private static readonly Emote Delete;
        
        static PluginHelper()
        {
            Delete = Emote.Parse("<:trash:747166938467401808>");
        }
        
        public static string Directory()
        {
            var folder = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!,
                Assembly.GetCallingAssembly().GetName().Name!);
            if (!System.IO.Directory.Exists(folder))
                System.IO.Directory.CreateDirectory(folder);
            return folder;
        }

        public static IUserMessage MakeDeletable(this IUserMessage message)
        {
            message.AddReactionAsync(Delete).GetAwaiter().GetResult();
            return message;
        }

        public static async Task DeleteReact(DiscordSocketClient client, IUserMessage message, SocketReaction emote)
        {
            if (message.Author.Id == client.CurrentUser.Id && emote.UserId != client.CurrentUser.Id && emote.Emote.Equals(Delete))
                await message.DeleteAsync();
        }
    }
}