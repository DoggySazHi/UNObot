using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Plugins.Attributes;
using UNObot.Plugins.Helpers;
using UNObot.Services;

namespace UNObot.Modules
{
    public class SettingsCommands : ModuleBase<SocketCommandContext>
    {
        private readonly DatabaseService _db;
        private readonly EmbedDisplayService _embed;
        
        public SettingsCommands(DatabaseService db, EmbedDisplayService embed)
        {
            _db = db;
            _embed = embed;
        }
        
        [Command("setdefaultchannel", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [Alias("adddefaultchannel")]
        [DisableDMs]
        [Help(new[] {".setdefaultchannel"}, "Set the default channel for UNObot to chat in. Managers only.", true,
            "UNObot 2.0")]
        public async Task SetDefaultChannel()
        {
            await ReplyAsync($":white_check_mark: Set default UNO channel to #{Context.Channel.Name}.");
            await _db.SetDefaultChannel(Context.Guild.Id, Context.Channel.Id);
            await _db.SetHasDefaultChannel(Context.Guild.Id, true);

            //default channel should be allowed, by default
            var currentChannels = await _db.GetAllowedChannels(Context.Guild.Id);
            currentChannels.Add(Context.Channel.Id);
            await _db.SetAllowedChannels(Context.Guild.Id, currentChannels);
        }

        [Command("removedefaultchannel", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [Alias("deletedefaultchannel")]
        [DisableDMs]
        [Help(new[] {".removedefaultchannel"}, "Remove the default channel for UNObot to chat in. Managers only.", true,
            "UNObot 2.0")]
        public async Task RemoveDefaultChannel()
        {
            await ReplyAsync(":white_check_mark: Removed default UNO channel, assuming there was one.");
            if (!await DatabaseExtensions.HasDefaultChannel(_db.ConnString, Context.Guild.Id))
            {
                var channel = await DatabaseExtensions.GetDefaultChannel(_db.ConnString, Context.Guild.Id);
                //remove default channel
                var currentChannels = await _db.GetAllowedChannels(Context.Guild.Id);
                currentChannels.Remove(channel);
                await _db.SetAllowedChannels(Context.Guild.Id, currentChannels);
            }

            //ok tbh, it should be null, but doesn't really matter imo
            await _db.SetDefaultChannel(Context.Guild.Id, Context.Guild.DefaultChannel.Id);
            await _db.SetHasDefaultChannel(Context.Guild.Id, false);
        }

        [Command("enforcechannels", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [Alias("forcechannels")]
        [DisableDMs]
        [Help(new[] {".enforcechannels"},
            "Only allow UNObot to recieve commands from enforced channels. Managers only.", true, "UNObot 2.0")]
        public async Task EnforceChannel()
        {
            //start check (make sure all channels exist at time of enforcing)
            var allowedChannels = await _db.GetAllowedChannels(Context.Guild.Id);
            var currentChannels = Context.Guild.TextChannels.ToList();
            var currentChannelsIDs = new List<ulong>();
            foreach (var channel in currentChannels)
                currentChannelsIDs.Add(channel.Id);
            if (allowedChannels.Except(currentChannelsIDs).Any())
            {
                foreach (var toRemove in allowedChannels.Except(currentChannelsIDs))
                    allowedChannels.Remove(toRemove);
                await _db.SetAllowedChannels(Context.Guild.Id, allowedChannels);
            }

            //end check
            if (allowedChannels.Count == 0)
            {
                await Context.Channel.SendMessageAsync(
                    "Error: Cannot enable enforcechannels if there are no allowed channels!");
                return;
            }

            if (!await DatabaseExtensions.HasDefaultChannel(_db.ConnString, Context.Guild.Id))
            {
                await Context.Channel.SendMessageAsync(
                    "Error: Cannot enable enforcechannels if there is no default channel!");
                return;
            }

            var enforce = await _db.ChannelEnforced(Context.Guild.Id);
            await _db.SetEnforceChannel(Context.Guild.Id, !enforce);
            if (!enforce)
                await ReplyAsync(
                    ":white_check_mark: Currently enforcing UNObot to only respond to messages in the filter.");
            else
                await ReplyAsync(":white_check_mark: Currently allowing UNObot to respond to messages from anywhere.");
        }

        [Command("addallowedchannel", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [DisableDMs]
        [Help(new[] {".addallowedchannel"}, "Allow the current channel to accept commands. Managers only.", true,
            "UNObot 2.0")]
        public async Task AddAllowedChannel()
        {
            if (!await DatabaseExtensions.HasDefaultChannel(_db.ConnString, Context.Guild.Id))
            {
                await ReplyAsync("Error: You need to set a default channel first.");
            }
            else if (await DatabaseExtensions.GetDefaultChannel(_db.ConnString, Context.Guild.Id) == Context.Channel.Id)
            {
                await ReplyAsync(
                    "The default UNO channel has been set to this already; there is no need to add this as a default channel.");
            }
            else if ((await _db.GetAllowedChannels(Context.Guild.Id)).Contains(Context.Channel.Id))
            {
                await ReplyAsync("This channel is already allowed! To see all channels, use .listallowedchannels.");
            }
            else
            {
                var currentChannels = await _db.GetAllowedChannels(Context.Guild.Id);
                currentChannels.Add(Context.Channel.Id);
                await _db.SetAllowedChannels(Context.Guild.Id, currentChannels);
                await ReplyAsync(
                    $"Added #{Context.Channel.Name} to the list of allowed channels. Make sure you .enforcechannels for this to work.");
            }
        }

        [Command("listallowedchannels", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [DisableDMs]
        [Help(new[] {".listallowedchannels"},
            "See all channels that UNObot can accept commands if enforced mode was on.", true, "UNObot 2.0")]
        public async Task ListAllowedChannels()
        {
            var allowedChannels = await _db.GetAllowedChannels(Context.Guild.Id);
            //start check
            var currentChannels = Context.Guild.TextChannels.ToList();
            var currentChannelsIDs = new List<ulong>();
            foreach (var channel in currentChannels)
                currentChannelsIDs.Add(channel.Id);
            if (allowedChannels.Except(currentChannelsIDs).Any())
            {
                foreach (var toRemove in allowedChannels.Except(currentChannelsIDs))
                    allowedChannels.Remove(toRemove);
                await _db.SetAllowedChannels(Context.Guild.Id, allowedChannels);
            }

            //end check
            var enforced = await _db.ChannelEnforced(Context.Guild.Id);
            var yesno = enforced ? "Currently enforcing channels." : "Not enforcing channels.";
            var response = $"{yesno}\nCurrent channels allowed: \n";
            if (allowedChannels.Count == 0)
            {
                await ReplyAsync(
                    "There are no channels that are currently allowed. Add them with .addallowedchannel and .enforcechannels.");
                return;
            }

            foreach (var id in allowedChannels)
                response += $"- <#{id}>\n";
            await ReplyAsync(response);
        }

        [Command("removeallowedchannel", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [DisableDMs]
        [Help(new[] {".removeallowedchannel"},
            "Remove a channel that UNObot previously was allowed to accept commands from.", true, "UNObot 2.0")]
        public async Task RemoveAllowedChannel()
        {
            //start check
            var allowedChannels = await _db.GetAllowedChannels(Context.Guild.Id);
            var currentChannels = Context.Guild.TextChannels.ToList();
            var currentChannelsIDs = new List<ulong>();
            foreach (var channel in currentChannels)
                currentChannelsIDs.Add(channel.Id);
            if (allowedChannels.Except(currentChannelsIDs).Any())
            {
                foreach (var toRemove in allowedChannels.Except(currentChannelsIDs))
                    allowedChannels.Remove(toRemove);
                await _db.SetAllowedChannels(Context.Guild.Id, allowedChannels);
            }

            //end check
            if (allowedChannels.Contains(Context.Channel.Id))
            {
                allowedChannels.Remove(Context.Channel.Id);
                await _db.SetAllowedChannels(Context.Guild.Id, allowedChannels);
                await ReplyAsync($":white_check_mark: Removed <#{Context.Channel.Id}> from the allowed channels!");
            }
            else
            {
                await ReplyAsync(":no_entry: This channel was never an allowed channel.");
            }
        }

        [Command("settings", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [DisableDMs]
        [Help(new[] {".settings"},
            "Access configurable settings for UNObot.", true, "UNObot 4.3")]
        public async Task ViewSettings()
        {
            var settings = new Setting("General Settings");
            settings.UpdateSetting("Enforce Channels", true);
            settings.UpdateSetting("Channels Enforced", System.Array.Empty<string>());
            SettingsManager.RegisterSettings("UNObot", settings);
            await ReplyAsync("", embed: _embed.SettingsEmbed(null));
        }
    }
}