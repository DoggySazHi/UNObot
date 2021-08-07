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
        
        public UNObotCommandContext(DiscordSocketClient discord, SocketUser user, SocketUserMessage message, ISocketMessageChannel channel)
            : this(discord, user, channel is IPrivateChannel ? null : (channel as SocketGuildChannel)?.Guild, message, channel is IPrivateChannel)
        {
            IsMessage = message != null;
        }
        
        public UNObotCommandContext(DiscordSocketClient discord, SocketUserMessage message)
            : this(discord, message.Author, message, message.Channel)
        {
            
        }
    }
}