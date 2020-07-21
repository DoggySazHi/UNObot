using Discord;
using Discord.Commands;

namespace ConnectBot.Templates
{
    public interface ICommandContextEx : ICommandContext
    {
        bool IsPrivate { get; set; }
        bool IsMessage { get; set; }
    }
    
    public class FakeContext : ICommandContextEx
    {
        public IDiscordClient Client { get; }
        public IGuild Guild { get; }
        public IMessageChannel Channel { get; }
        public IUser User { get; }
        public IUserMessage Message { get; }
        
        public bool IsPrivate { get; set; }
        
        public bool IsMessage { get; set; }

        public FakeContext(IDiscordClient client, IUser user, IGuild guild, IUserMessage message, bool isPrivate)
        {
            Client = client;
            User = user;
            Guild = guild;
            Channel = message.Channel;
            Message = message;
            IsPrivate = isPrivate;
        }

        public FakeContext(SocketCommandContext context)
        {
            Client = context.Client;
            User = context.User;
            Guild = context.Guild;
            Channel = context.Channel;
            Message = context.Message;
            IsPrivate = context.IsPrivate;
            IsMessage = true;
        }
    }
}