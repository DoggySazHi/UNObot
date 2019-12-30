using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace UNObot.Services
{
    public class CommandHandlingService
    {
        readonly DiscordSocketClient _discord;
        readonly CommandService _commands;
        IServiceProvider _provider;

        public CommandHandlingService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;

            _discord.MessageReceived += MessageReceived;
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            // Add additional initialization code here...
        }

        async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            int argPos = 0;
            var context = new SocketCommandContext(_discord, message);

            if (await UNODatabaseService.EnforceChannel(context.Guild.Id))
            {
                //start check
                var allowedChannels = await UNODatabaseService.GetAllowedChannels(context.Guild.Id);
                var currentChannels = context.Guild.TextChannels.ToList();
                var currentChannelsIDs = new List<ulong>();
                foreach (var channel in currentChannels)
                    currentChannelsIDs.Add(channel.Id);
                if (allowedChannels.Except(currentChannelsIDs).Any())
                {
                    foreach (var toRemove in allowedChannels.Except(currentChannelsIDs))
                        allowedChannels.Remove(toRemove);
                    await UNODatabaseService.SetAllowedChannels(context.Guild.Id, allowedChannels);
                }
                //end check
                if (allowedChannels.Count == 0)
                {
                    await context.Channel.SendMessageAsync("Warning: Since there are no channels that allow UNObot to speak normally, enforcechannels has been disabled.");
                    await UNODatabaseService.SetEnforceChannel(context.Guild.Id, false);
                }
                else if (!(allowedChannels.Contains(context.Channel.Id)))
                    return;
            }
            if (!(message.HasCharPrefix('.', ref argPos)) && !message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;

            if (context.IsPrivate)
            {
                await context.Channel.SendMessageAsync("I do not accept DM messages. Please use me in a guild/server.");
                return;
            }
            await UNODatabaseService.AddGame(context.Guild.Id);
            await UNODatabaseService.AddUser(context.User.Id, context.User.Username);
            var result = await _commands.ExecuteAsync(context, argPos, _provider);
            if (result.Error.HasValue)
            {
                switch (result.Error.Value)
                {
                    case CommandError.BadArgCount:
                        await context.Channel.SendMessageAsync($"Hmm, that's not how it works. Type '<@{context.Client.CurrentUser.Id}> help' for the parameters of your command.");
                        break;
                    case CommandError.ParseFailed:
                        await context.Channel.SendMessageAsync("You dun goof. If it asks for numbers, type an actual number. If it asks for words, make sure to double quote around it.");
                        break;
                    case CommandError.MultipleMatches:
                        await context.Channel.SendMessageAsync($"There are multiple commands with the same name. Type '<@{context.Client.CurrentUser.Id}> help' to see which one you need.");
                        break;
                    case CommandError.UnmetPrecondition:
                        //await context.Channel.SendMessageAsync("You do not have the **power** to run this command!");
                        break;
                    case CommandError.UnknownCommand:
                    case CommandError.ObjectNotFound:
                    case CommandError.Exception:
                    case CommandError.Unsuccessful:
                        break;
                }
#if DEBUG
                if (result.Error.Value != CommandError.UnknownCommand)
                    await context.Channel.SendMessageAsync($"Debug error: {result}");
#endif
            }
        }
    }
}