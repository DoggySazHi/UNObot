using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace UNObot.Modules
{
    public static class InputHandler
    {
        static readonly Dictionary<string, string> reactions = new Dictionary<string, string>
        {
            {"<:red:498252114972114999>", "Red"},
            {"<:green:498252148094533633>", "Green"},
            {"<:blue:498252191702843402>", "Blue"},
            {"<:yellow:498252218089078785>", "Yellow"},
            {"<:wild4:498258323108528129>", "Wild +4"},
            {"<:wildcolor:498258323397804052>", "Wild Color"},
            {"0⃣", "0"},
            {"1⃣", "1"},
            {"2⃣", "2"},
            {"3⃣", "3"},
            {"4⃣", "4"},
            {"5⃣", "5"},
            {"6⃣", "6"},
            {"7⃣", "7"},
            {"8⃣", "8"},
            {"9⃣", "9"}
        };
        public static async Task ReactionAdded(Cacheable<IUserMessage, ulong> before, ISocketMessageChannel after, SocketReaction channel)
        {
            //TODO add check for what type of message (kete)
            string input;
            var message = await before.GetOrDownloadAsync();
            Dictionary<IEmote, IUser[]> messagereactions = new Dictionary<IEmote, IUser[]>();

            foreach (var reaction in message.Reactions)
            {
                var users = await message.GetReactionUsersAsync(reaction.Key);
                messagereactions.Add(reaction.Key, users.ToArray());
            }

            await message.RemoveReactionAsync(channel.Emote, channel.User.Value);
            if (reactions.ContainsKey(channel.Emote.Name))
                input = reactions[channel.Emote.Name];
            else
                //probably a wrong icon
                Console.WriteLine(channel.Emote.Name);
        }
        public static async Task AddReactions(SocketUserMessage s)
        {
            foreach (string emoji in reactions.Keys)
                await s.AddReactionAsync(new Emoji("1"));
        }
    }
}
