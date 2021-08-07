using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace UNObot.Plugins
{
    public interface IUNObotCommandContext : ICommandContext
    {
        bool IsPrivate { get; }
        bool IsMessage { get; }
    }
    
    public class UNObotCommandContext : IUNObotCommandContext
    { 
        public DiscordSocketClient Client { get; }
        public SocketGuild Guild { get; } 
        public ISocketMessageChannel Channel { get; }
        public SocketUser User { get; }
        public SocketUserMessage Message { get; }

        IDiscordClient ICommandContext.Client => Client;
        IGuild ICommandContext.Guild => Guild;
        IMessageChannel ICommandContext.Channel => Channel;
        IUser ICommandContext.User => User;
        IUserMessage ICommandContext.Message => Message;
        
        public bool IsPrivate { get; }
        
        public bool IsMessage { get; }

        public UNObotCommandContext(DiscordSocketClient client, SocketUser user, SocketGuild guild, SocketUserMessage message, bool isPrivate)
        {
            Client = client;
            User = user;
            Guild = guild;
            Channel = message.Channel;
            Message = message;
            IsPrivate = isPrivate;
        }
        
        public UNObotCommandContext(DiscordSocketClient discord, SocketUserMessage message)
        {
            Client = discord;
            User = message.Author;
            Channel = message.Channel;
            Message = message;
            IsPrivate = message.Channel is IPrivateChannel;
            Guild = IsPrivate ? null : (message.Channel as SocketGuildChannel)?.Guild;
            IsMessage = true;
        }
    }
}