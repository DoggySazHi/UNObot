using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace UNObot.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private IServiceProvider _provider;

        public CommandHandlingService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
            _commands.Log += LoggerService.GetSingleton().LogCommand;
            _provider = provider;
            _discord.MessageReceived += MessageReceived;
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;
            await AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task<IEnumerable<ModuleInfo>> AddModulesAsync(Assembly assembly)
        {
            return await _commands.AddModulesAsync(assembly, _provider);
        }
        
        public async Task<bool> RemoveModulesAsync(Type type)
        {
            return await _commands.RemoveModuleAsync(type);
        }
        
        public async Task RemoveModulesAsync(Assembly assembly)
        {
            foreach(var type in assembly.GetTypes())
                if(typeof(ModuleBase<SocketCommandContext>).IsAssignableFrom(type))
                    await _commands.RemoveModuleAsync(type);
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            var argPos = 0;
            var context = new SocketCommandContext(_discord, message);

            if (!context.IsPrivate && await UNODatabaseService.ChannelEnforced(context.Guild.Id))
            {
                //start check
                var allowedChannels = await UNODatabaseService.GetAllowedChannels(context.Guild.Id);
                var currentChannels = context.Guild.TextChannels.ToList();
                var currentChannelsIDs = currentChannels.Select(channel => channel.Id).ToList();
                if (allowedChannels.Except(currentChannelsIDs).Any())
                {
                    var tempList = new List<ulong>(allowedChannels.Except(currentChannelsIDs));
                    foreach (var toRemove in tempList)
                        allowedChannels.Remove(toRemove);
                    await UNODatabaseService.SetAllowedChannels(context.Guild.Id, allowedChannels);
                }

                //end check
                if (allowedChannels.Count == 0)
                {
                    await context.Channel.SendMessageAsync(
                        "Warning: Since there are no channels that allow UNObot to speak normally, enforcechannels has been disabled.");
                    await UNODatabaseService.SetEnforceChannel(context.Guild.Id, false);
                }
                else if (!allowedChannels.Contains(context.Channel.Id))
                {
                    return;
                }
            }

            if (!message.HasCharPrefix('.', ref argPos) &&
                !message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;

            if (!context.IsPrivate)
                await UNODatabaseService.AddGame(context.Guild.Id);
            await UNODatabaseService.AddUser(context.User.Id, context.User.Username);
            try
            {
                if (context.IsPrivate)
                {
                    var messageString = message.ToString();
                    var endOfCommand = messageString.IndexOf(' ', argPos);
                    var attemptCommandExecute =
                        messageString.Substring(argPos,
                            (endOfCommand == -1 ? messageString.Length : endOfCommand) - argPos);
                    foreach (var command in Program.Commands)
                    {
                        if (!command.DisableDMs) continue;
                        var sameCommand =
                            command.CommandName.Equals(attemptCommandExecute,
                                StringComparison.CurrentCultureIgnoreCase) ||
                            command.Aliases.Any(o =>
                                o.Equals(attemptCommandExecute, StringComparison.CurrentCultureIgnoreCase));
                        if (sameCommand)
                        {
                            await context.Channel.SendMessageAsync(
                                "This command cannot be run in DMs. Please try again in a server.");
                            return;
                        }
                    }
                }

                var result = await _commands.ExecuteAsync(context, argPos, _provider);
                if (result.Error.HasValue)
                {
                    switch (result.Error.Value)
                    {
                        case CommandError.BadArgCount:
                            await context.Channel.SendMessageAsync(
                                $"Hmm, that's not how it works. Type '<@{context.Client.CurrentUser.Id}> help' for the parameters of your command.");
                            break;
                        case CommandError.ParseFailed:
                            await context.Channel.SendMessageAsync(
                                "You dun goof. If it asks for numbers, type an actual number. If it asks for words, make sure to double quote around it.");
                            break;
                        case CommandError.MultipleMatches:
                            await context.Channel.SendMessageAsync(
                                $"There are multiple commands with the same name. Type '<@{context.Client.CurrentUser.Id}> help' to see which one you need.");
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
            catch (Exception e)
            {
                LoggerService.Log(LogSeverity.Error, "While attempting to execute a command, we got an error!", e);
            }
        }
    }
}