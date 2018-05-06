using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private IServiceProvider _provider;

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
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
            // Add additional initialization code here...
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            int argPos = 0;
            if (!(message.HasCharPrefix('!', ref argPos)) || !message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;

            var context = new SocketCommandContext(_discord, message);
            if(context.IsPrivate)
            {
                await context.Channel.SendMessageAsync("I do not accept DM messages. Please use me in a guild/server.");
            }
            var result = await _commands.ExecuteAsync(context, argPos, _provider);
            if (result.Error.HasValue)
            {
                switch(result.Error.Value)
                {
                    case CommandError.UnknownCommand:
                        await context.Channel.SendMessageAsync("That's not a command dummy. Type '<@419374055792050176> help' for a list of commands.");
                        break;
                    case CommandError.BadArgCount:
                        await context.Channel.SendMessageAsync("Hmm, that's not how it works. Type '<@419374055792050176> help' for the parameters of your command.");
                        break;
                    case CommandError.ParseFailed:
                        await context.Channel.SendMessageAsync("You dun goof. If it asks for numbers, type an actual number. If it asks for words, make sure to double quote around it.");
                        break;
                    case CommandError.MultipleMatches:
                        await context.Channel.SendMessageAsync("There are multiple commands with the same name. Type '<@419374055792050176> help' to see which one you need.");
                        break;
                    case CommandError.UnmetPrecondition:
                        await context.Channel.SendMessageAsync("You do not have the **power** to run this command!");
                        break;
                    case CommandError.Exception:
                        await context.Channel.SendMessageAsync(":bomb: _UNObot has encountered a fatal error, but luckily, we have caught the error.\nPlease send all bug reports to DoggySazHi.");
                        break;
                    default:
                        await context.Channel.SendMessageAsync(":bomb: _UNObot has encountered a fatal error, and your action could not be completed.\nPlease send all bug reports to DoggySazHi.");
                        break;
                }
                #if DEBUG
                await context.Channel.SendMessageAsync($"Debug error: {result.ToString()}");
                #endif
            }
        }
    }
}
