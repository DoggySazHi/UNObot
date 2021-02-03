using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using UNObot.Plugins;
using UNObot.Plugins.Attributes;
using UNObot.Plugins.Helpers;
using UNObot.Templates;

namespace UNObot.Services
{
    internal class CommandHandlingService : IDisposable
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private IServiceProvider _provider;
        private readonly ILogger _logger;
        private readonly DatabaseService _db;
        private static List<Command> _loaded;

        public static IEnumerable<Command> Commands => _loaded;

        public CommandHandlingService(IServiceProvider provider, ILogger logger, DatabaseService db, DiscordSocketClient discord, CommandService commands)
        {
            _logger = logger;
            _discord = discord;
            _commands = commands;
            _db = db;
            _provider = provider;
            _commands.CommandExecuted += CommandExecuted;
            _discord.MessageReceived += MessageReceived;
            _loaded = new List<Command>();
        }

        internal async Task InitializeAsync(IServiceProvider provider, LoggerService logger)
        {
            _commands.Log += logger.LogCommand;
            _provider = provider;
            await AddModulesAsync(Assembly.GetEntryAssembly(), original: true);
            _discord.ReactionAdded += async (message, _, emote) => 
                await PluginHelper.DeleteReact(_discord, await message.GetOrDownloadAsync(), emote);
        }
        
        internal async Task<IEnumerable<ModuleInfo>> AddModulesAsync(Assembly assembly, IServiceCollection services = null, bool original = false)
        {
            var provider = _provider;
            if (services != null)
                provider = services.AddSingleton(_discord)
                    .AddSingleton(_logger)
                    .AddSingleton(_provider.GetRequiredService<IConfiguration>())
                    .AddSingleton(this) // Required for .help, which seeks duplicates.
                    .BuildServiceProvider();
            await LoadHelp(assembly, provider, original);
            return await _commands.AddModulesAsync(assembly, provider);
        }
        
        internal async Task<bool> RemoveModulesAsync(Type type)
        {
            return await _commands.RemoveModuleAsync(type);
        }
        
        internal async Task RemoveModulesAsync(Assembly assembly)
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

            if (!context.IsPrivate && await _db.ChannelEnforced(context.Guild.Id))
            {
                //start check
                var allowedChannels = await _db.GetAllowedChannels(context.Guild.Id);
                var currentChannels = context.Guild.TextChannels.ToList();
                var currentChannelsIDs = currentChannels.Select(channel => channel.Id).ToList();
                if (allowedChannels.Except(currentChannelsIDs).Any())
                {
                    var tempList = new List<ulong>(allowedChannels.Except(currentChannelsIDs));
                    foreach (var toRemove in tempList)
                        allowedChannels.Remove(toRemove);
                    await _db.SetAllowedChannels(context.Guild.Id, allowedChannels);
                }

                //end check
                if (allowedChannels.Count == 0)
                {
                    await context.Channel.SendMessageAsync(
                        "Warning: Since there are no channels that allow UNObot to speak normally, channel enforcement has been disabled.");
                    await _db.SetEnforceChannel(context.Guild.Id, false);
                }
                else if (!allowedChannels.Contains(context.Channel.Id))
                {
                    return;
                }
            }

            if (!(context.IsPrivate && message.HasCharPrefix('.', ref argPos)) && // If it's in a DM, forcibly use '.' prefix
                !(!context.IsPrivate && message.HasStringPrefix(await _db.GetPrefix(context.Guild.Id), ref argPos)) && // If it's in a server, query DB.
                !message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return; // Look for mentions.

            if (!context.IsPrivate)
                await _db.AddGame(context.Guild.Id);
            await _db.AddUser(context.User.Id, context.User.Username);
            try
            {
                var success = GetProvider(context, argPos, out var provider);
                if(!success)
                    await context.Channel.SendMessageAsync(
                        "This command cannot be run in DMs. Please try again in a server.");
                else
                    await _commands.ExecuteAsync(context, argPos, provider);
            }
            catch (Exception e)
            {
                _logger.Log(LogSeverity.Error, "While attempting to execute a command, we got an error!", e);
            }
        }

        private bool GetProvider(SocketCommandContext context, int argPos, out IServiceProvider provider)
        {
            provider = _provider;

            var message = context.Message;
            var messageString = message.ToString();
            var endOfCommand = messageString.IndexOf(' ', argPos);
            var attemptCommandExecute =
                messageString.Substring(argPos,
                    (endOfCommand == -1 ? messageString.Length : endOfCommand) - argPos);
            foreach (var command in _loaded)
            {
                var sameCommand =
                    command.CommandName.Equals(attemptCommandExecute,
                        StringComparison.CurrentCultureIgnoreCase) ||
                    command.Aliases.Any(o =>
                        o.Equals(attemptCommandExecute, StringComparison.CurrentCultureIgnoreCase));
                if (!sameCommand) continue;
                if (command.Services != null)
                    provider = command.Services;
                if (context.IsPrivate && command.DisableDMs)
                    return false;
            }
            return true;
        }
        
        private async Task CommandExecuted(Optional<CommandInfo> arg1, ICommandContext context, IResult result)
        {
            if (result.Error.HasValue)
            {
                switch (result.Error.Value)
                {
                    case CommandError.BadArgCount:
                        (await context.Channel.SendMessageAsync(
                            $"Hmm, that's not how it works. Type '<@{context.Client.CurrentUser.Id}> help' for the parameters of your command.")).MakeDeletable();
                        break;
                    case CommandError.ParseFailed:
                        (await context.Channel.SendMessageAsync(
                            "You dun goof. If it asks for numbers, type an actual number. If it asks for words, make sure to double quote around it.")).MakeDeletable();
                        break;
                    case CommandError.MultipleMatches:
                        (await context.Channel.SendMessageAsync(
                            $"There are multiple commands with the same name. Type '<@{context.Client.CurrentUser.Id}> help' to see which one you need.")).MakeDeletable();
                        break;
                    case CommandError.UnmetPrecondition:
                    case CommandError.UnknownCommand:
                    case CommandError.ObjectNotFound:
                    case CommandError.Exception:
                    case CommandError.Unsuccessful:
                        break;
                }
            }
        }

        internal Command FindCommand(string name)
        {
            var index = _loaded.FindIndex(o => o.CommandName == name);
            var index2 = _loaded.FindIndex(o => o.Aliases.Contains(name));
            Command cmd = null;
            if (index >= 0)
                cmd = _loaded[index];
            else if (index2 >= 0)
                cmd = _loaded[index2];
            return cmd;
        }

        internal async Task LoadHelp(Assembly asm, IServiceCollection services)
        {
            var provider = _provider;
            if (services != null)
                 provider = services.AddSingleton(_discord)
                    .AddSingleton(_logger)
                    .AddSingleton(_provider.GetRequiredService<IConfiguration>())
                    .AddSingleton(this) // Required for .help, which seeks duplicates.
                    .BuildServiceProvider();
            await LoadHelp(asm, provider);
        }
        
        private async Task LoadHelp(Assembly asm, IServiceProvider provider, bool original = false)
        {
            var types = from c in asm.GetTypes()
                where c.IsClass
                select c;
            foreach (var type in types)
            foreach (var module in type.GetMethods().Concat(type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)))
            {
                var helpAtt = module.GetCustomAttribute(typeof(HelpAttribute)) as HelpAttribute;
                var aliasAtt = module.GetCustomAttribute(typeof(AliasAttribute)) as AliasAttribute;
                var disableDmsAtt = module.GetCustomAttribute(typeof(DisableDMsAttribute)) as DisableDMsAttribute;
                var ownerOnlyAtt = module.GetCustomAttribute(typeof(RequireOwnerAttribute)) as RequireOwnerAttribute;

                var aliases = new List<string>();
                //check if it is a command
                if (!(module.GetCustomAttribute(typeof(CommandAttribute)) is CommandAttribute nameAtt)) continue;

                // var foundHelp = helpAtt == null ? "Missing help." : "Found help.";
                var disabledForDMs = disableDmsAtt != null;
                // _logger.Log(LogSeverity.Verbose, $"Loaded \"{nameAtt.Text}\". {foundHelp}");
                var positionCmd = _loaded.FindIndex(o => o.CommandName == nameAtt.Text && !o.Original);
                if (aliasAtt?.Aliases != null)
                    aliases = aliasAtt.Aliases.ToList();
                if (positionCmd < 0)
                {
                    var cmd = helpAtt != null
                        ? new Command(nameAtt.Text, aliases, helpAtt.Usages.ToList(), helpAtt.HelpMsg,
                            helpAtt.Active, helpAtt.Version, disabledForDMs)
                        : new Command(nameAtt.Text, aliases, new List<string> {$".{nameAtt.Text}"},
                            "No help is given for this command.", ownerOnlyAtt == null, "Unknown Version",
                            disabledForDMs);
                    if (original)
                        cmd.Original = true;
                    cmd.Services = provider;
                    _loaded.Add(cmd);
                }
                else
                {
                    _loaded[positionCmd].DisableDMs = disabledForDMs;
                    _loaded[positionCmd].Services = provider;
                    if (helpAtt != null)
                    {
                        if (!string.IsNullOrEmpty(helpAtt.HelpMsg))
                            _loaded[positionCmd].Help = helpAtt.HelpMsg;
                        _loaded[positionCmd].Usages =
                            _loaded[positionCmd].Usages.Union(helpAtt.Usages.ToList()).ToList();
                        _loaded[positionCmd].Active |= helpAtt.Active;
                        if (!string.IsNullOrEmpty(helpAtt.Version))
                            _loaded[positionCmd].Version = helpAtt.Version;
                    }

                    if (aliasAtt != null)
                        _loaded[positionCmd].Aliases = _loaded[positionCmd].Aliases
                            .Union((aliasAtt.Aliases ?? throw new InvalidOperationException()).ToList()).ToList();
                }
            }

            _loaded = _loaded.OrderBy(o => o.CommandName).ToList();
            _logger.Log(LogSeverity.Info, $"Loaded {_loaded.Count} commands!");

            //Fallback to help.json, ex; Updates, Custom help messages, or temporary troll "fixes"
            if (File.Exists("help.json"))
            {
                _logger.Log(LogSeverity.Info, "Loading help.json into memory...");

                using (var r = new StreamReader("help.json"))
                {
                    var json = await r.ReadToEndAsync();
                    foreach (var c in JsonConvert.DeserializeObject<List<Command>>(json))
                    {
                        var index = _loaded.FindIndex(o => o.CommandName == c.CommandName);
                        if (index >= 0 && _loaded[index].Help == "No help is given for this command.")
                        {
                            _loaded[index] = c;
                        }
                        else if (index < 0)
                        {
                            _logger.Log(LogSeverity.Warning,
                                "A command was added that isn't in UNObot's code. It will be added to the help list, but will not be active.");
                            var newCommand = c;
                            newCommand.Active = false;
                            _loaded.Add(newCommand);
                        }
                    }
                }

                _logger.Log(LogSeverity.Info, $"Loaded {_loaded.Count} commands including from help.json!");
            }
        }

        internal async Task ClearHelp()
        {
            _loaded.Clear();
            await LoadHelp(Assembly.GetEntryAssembly(), _provider, true);
        }

        public void Dispose()
        {
            ((IDisposable) _commands)?.Dispose();
            _discord?.Dispose();
        }
    }
}