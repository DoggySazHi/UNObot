﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace UNObot.Core.Services
{
    public class InputHandlerService
    {
        private readonly LoggerService _logger;
        private readonly DiscordSocketClient _client;
        public InputHandlerService(LoggerService logger, DiscordSocketClient client)
        {
            _logger = logger;
            _client = client;
        }
        
        private static readonly Dictionary<IEmote, string> Reactions = new Dictionary<IEmote, string>
        {
            {Emote.Parse("<:red:498252114972114999>"), "Red"},
            {Emote.Parse("<:green:498252148094533633>"), "Green"},
            {Emote.Parse("<:blue:498252191702843402>"), "Blue"},
            {Emote.Parse("<:yellow:498252218089078785>"), "Yellow"},
            {Emote.Parse("<:wild4:498258323108528129>"), "Wild +4"},
            {Emote.Parse("<:wildcolor:498258323397804052>"), "Wild Color"},
            {new Emoji("0⃣"), "0"},
            {new Emoji("1⃣"), "1"},
            {new Emoji("2⃣"), "2"},
            {new Emoji("3⃣"), "3"},
            {new Emoji("4⃣"), "4"},
            {new Emoji("5⃣"), "5"},
            {new Emoji("6⃣"), "6"},
            {new Emoji("7⃣"), "7"},
            {new Emoji("8⃣"), "8"},
            {new Emoji("9⃣"), "9"}
        };

        internal async Task ReactionAdded(Cacheable<IUserMessage, ulong> reactmessage,
            ISocketMessageChannel channel, SocketReaction reaction)
        {
            //TODO add check for what type of message (kete)
            string input;
            var message = await reactmessage.GetOrDownloadAsync();
            var reacter = _client.GetUser(reaction.UserId);

            if (reacter.IsBot || message.Author.Id != _client.CurrentUser.Id)
                return;

            /*
            Dictionary<IEmote, IUser[]> messagereactions = new Dictionary<IEmote, IUser[]>();

            foreach (var react in message.Reactions)
            {
                var users = await message.GetReactionUsersAsync(react.Key, 3).FlattenAsync();
                messagereactions.Add(react.Key, users.ToArray());
            }
            */

#if(DEBUG)
            if (Reactions.ContainsKey(reaction.Emote))
            {
                input = Reactions[reaction.Emote];
                await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                await channel.SendMessageAsync($"Recieved an {input} from {reacter.Username}");
            }
            else
                //probably a wrong icon
            {
                _logger.Log(LogSeverity.Debug, reaction.Emote.Name);
            }
#endif
        }

        internal static async Task AddReactions(IUserMessage s)
        {
            foreach (var emoji in Reactions.Keys)
                await s.AddReactionAsync(emoji);
        }
    }
}