using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using UNObot.Plugins;

namespace ConnectBot.Services
{
    public class ButtonHandler
    {
        private static bool _init;
        private readonly DiscordSocketClient _client;
        private readonly DatabaseService _db;
        public DropPiece Callback;

        public delegate Task DropPiece(IUNObotCommandContext context, string[] args);

        public ButtonHandler(DiscordSocketClient client, DatabaseService db)
        {
            _client = client;
            _db = db;
            if (_init) return;
            _init = true;
            client.ReactionAdded += ReactionAdded;
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> inMessage, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            var message = await inMessage.GetOrDownloadAsync();
            if (message.Author.Id != _client.CurrentUser.Id) return;
            if (!reaction.User.IsSpecified) return;
            if (channel.Value is not ITextChannel serverChannel) return;
            var context =
                new UNObotCommandContext(_client, reaction.User.Value as SocketUser, serverChannel.Guild as SocketGuild, message as SocketUserMessage, false);
            var game = await _db.GetGame(context.Guild.Id);
            if (game.Queue.GameStarted() && game.Queue.CurrentPlayer().Player == reaction.UserId)
            {
                var number = -1;
                for(var i = 0; i < _numbers.Length; i++)
                    if (_numbers[i].Name == reaction.Emote.Name)
                        number = i;
                if (number != -1 && Callback != null)
                    _ = Callback(context, new[] {"", number.ToString()}).ConfigureAwait(false);
            }
        }

        private readonly IEmote[] _numbers = {
            new Emoji("\u0030\u20E3"),
            new Emoji("\u0031\u20E3"),
            new Emoji("\u0032\u20E3"),
            new Emoji("\u0033\u20E3"),
            new Emoji("\u0034\u20E3"),
            new Emoji("\u0035\u20E3"),
            new Emoji("\u0036\u20E3"),
            new Emoji("\u0037\u20E3"),
            new Emoji("\u0038\u20E3"),
            new Emoji("\u0039\u20E3"),
            new Emoji( "\U0001F51F") // in short, a "bruh" moment
        };

        public async Task AddNumbers(IUserMessage message, Range range)
        {
            if (range.End.Value > _numbers.Length)
                range = new Range(range.Start, new Index(0, true));
            try
            {
                await message.AddReactionsAsync(_numbers[range]);
            }
            catch (CommandException)
            {
                 /* well, crap! */
            }
        }

        public async Task ClearReactions(IUserMessage message, IUser user)
        {
            try
            {
                foreach (var emoji in _numbers)
                {
                    var users = await message.GetReactionUsersAsync(emoji, 50).FlattenAsync();
                    var react = users.FirstOrDefault(o => o.Id == user.Id);
                    if (react != null)
                        await message.RemoveReactionAsync(emoji, user);
                }
            }
            catch (CommandException)
            {
                // No permissions to delete!
            }
        }
    }
}