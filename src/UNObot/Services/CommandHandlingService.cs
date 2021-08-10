using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using UNObot.Plugins;
using UNObot.Plugins.Attributes;
using UNObot.Plugins.Helpers;
using UNObot.Templates;

namespace UNObot.Services
{
    public class CommandHandlingService : IDisposable
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
            _discord.InteractionCreated += InteractionCreated;
            _loaded = new List<Command>();
        }

        public async Task InitializeAsync(IServiceProvider provider, LoggerService logger)
        {
            _commands.Log += logger.LogCommand;
            _provider = provider;
            await AddModulesAsync(Assembly.GetEntryAssembly(), original: true);
            _discord.ReactionAdded += async (message, _, emote) => 
                await PluginHelper.DeleteReact(_discord, await message.GetOrDownloadAsync(), emote);
        }
        
        public async Task<IEnumerable<ModuleInfo>> AddModulesAsync(Assembly assembly, IServiceCollection services = null, bool original = false)
        {
            var provider = _provider;
            if (services != null)
                provider = services.AddSingleton(_discord)
                    .AddSingleton(_logger)
                    .AddSingleton(_provider.GetRequiredService<IUNObotConfig>())
                    .AddSingleton(this) // Required for .help, which seeks duplicates.
                    .BuildServiceProvider();
            await LoadHelp(assembly, provider, original);
            return await _commands.AddModulesAsync(assembly, provider);
        }
        
        public async Task<bool> RemoveModulesAsync(Type type)
        {
            return await _commands.RemoveModuleAsync(type);
        }
        
        public async Task RemoveModulesAsync(Assembly assembly)
        {
            foreach(var type in assembly.GetTypes())
                if(typeof(ModuleBase<UNObotCommandContext>).IsAssignableFrom(type))
                    await _commands.RemoveModuleAsync(type);
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (rawMessage is not SocketUserMessage { Source: MessageSource.User } message) return;

            var context = new UNObotCommandContext(_discord, message);
            
            var enforcementMessage = await EnforcementPermitsMessage(context);
            if (enforcementMessage != null)
            {
                await context.User.SendMessageAsync(enforcementMessage);
                return;
            }
            
            if (!context.IsPrivate)
                await _db.RegisterServer(context.Guild.Id);
            await _db.RegisterUser(context.User.Id, context.User.Username);
            
            var argPos = await IsUserCommand(context);
            if (argPos < 0)
                return;

            try
            {
                var success = GetProvider(context.Message.Content, context.IsPrivate, argPos, out var provider);
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
        
        private async Task InteractionCreated(SocketInteraction arg)
        {
            if (arg is not SocketSlashCommand command) return;
            if (command.User.IsBot || command.User.IsWebhook) return;

            var context = new UNObotCommandContext(_discord, arg.User, null, arg.Channel);
            
            var enforcementMessage = await EnforcementPermitsMessage(context);
            if (enforcementMessage != null)
            {
                await arg.RespondAsync(enforcementMessage, ephemeral: true);
                return;
            }
            
            if (!context.IsPrivate)
                await _db.RegisterServer(context.Guild.Id);
            await _db.RegisterUser(context.User.Id, context.User.Username);
            
            try
            {
                var message = $"{command.Data.Name}{command.Data.Options.Aggregate("", (a, b) => $"{a} {b.Value}")}";
                var success = GetProvider(message, context.IsPrivate, 0, out var provider);
                if(!success)
                    await command.RespondAsync(
                        "This command cannot be run in DMs. Please try again in a server.", ephemeral: true);
                else
                    await _commands.ExecuteAsync(context, message, provider);
            }
            catch (Exception e)
            {
                _logger.Log(LogSeverity.Error, "While attempting to execute a command, we got an error!", e);
            }
        }

        /// <summary>
        /// Check if the server's enforcement options allow UNObot to receive a command in a certain channel.
        /// </summary>
        /// <param name="context">The context of the message.</param>
        /// <returns>Returns null if allowed, otherwise a string with the message.</returns>
        private async Task<string> EnforcementPermitsMessage(UNObotCommandContext context)
        {
            // We don't care about DMs or if the server doesn't care about enforcement.
            if (context.IsPrivate || !await _db.ChannelEnforced(context.Guild.Id)) return null;
            
            // Filter allowed channels to the channels that are on the server.
            var allowedChannels = await _db.GetAllowedChannels(context.Guild.Id);
            var currentChannels = context.Guild.TextChannels.ToList();
            var currentChannelsIDs = currentChannels.Select(channel => channel.Id).ToList();
            if (allowedChannels.Except(currentChannelsIDs).Any())
            {
                // Only runs if an allowed channel was deleted.
                var tempList = new List<ulong>(allowedChannels.Except(currentChannelsIDs));
                foreach (var toRemove in tempList)
                    allowedChannels.Remove(toRemove);
                await _db.SetAllowedChannels(context.Guild.Id, allowedChannels);
            }

            if (allowedChannels.Count == 0)
            {
                await context.Channel.SendMessageAsync(
                    "Warning: Since there are no channels that allow UNObot to speak normally, channel enforcement has been disabled.");
                await _db.SetEnforceChannel(context.Guild.Id, false);
            }
            else if (!allowedChannels.Contains(context.Channel.Id))
            {
                var outMessage = "I am not allowed to receive commands in the channel you invoked me in.\nTry one of these channels:";
                outMessage += allowedChannels.Aggregate("", (a, b) => $"{a} <#{b}>");
                return outMessage;
            }

            return null;
        }

        private async Task<int> IsUserCommand(UNObotCommandContext context)
        {
            var argPos = -1;

            if (context.Message.HasMentionPrefix(_discord.CurrentUser, ref argPos))
                return argPos;
            
            if (context.IsPrivate) // If it's in a DM, forcibly use '.' prefix
            {
                context.Message.HasCharPrefix('.', ref argPos);
                return argPos;
            }

            context.Message.HasStringPrefix(await _db.GetPrefix(context.Guild.Id), ref argPos); // If it's in a server, query DB.
            return argPos;
        }

        private bool GetProvider(string message, bool isPrivate, int argPos, out IServiceProvider provider)
        {
            provider = _provider;

            var endOfCommand = message.IndexOf(' ', argPos);
            var attemptCommandExecute =
                message.Substring(argPos,
                    (endOfCommand == -1 ? message.Length : endOfCommand) - argPos);
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
                if (isPrivate && command.DisableDMs)
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

        public static Command FindCommand(string name)
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

        public async Task LoadHelp(Assembly asm, IServiceCollection services)
        {
            var provider = _provider;
            if (services != null)
                 provider = services.AddSingleton(_discord)
                    .AddSingleton(_logger)
                    .AddSingleton(_provider.GetRequiredService<IUNObotConfig>())
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
                var helpAtt = module.GetCustomAttribute<HelpAttribute>();
                var aliasAtt = module.GetCustomAttribute<AliasAttribute>();
                var disableDmsAtt = module.GetCustomAttribute<DisableDMsAttribute>();
                var ownerOnlyAtt = module.GetCustomAttribute<RequireOwnerAttribute>();

                var aliases = new List<string>();

                if (module.GetCustomAttribute(typeof(CommandAttribute)) is not CommandAttribute nameAtt) continue;

                var slashCommandAtt = module.GetCustomAttribute<SlashCommandAttribute>();
                if (slashCommandAtt != null)
                    CreateCommand(slashCommandAtt, ownerOnlyAtt);

                var disabledForDMs = disableDmsAtt != null;
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
                        switch (index)
                        {
                            case >= 0 when _loaded[index].Help == "No help is given for this command.":
                                _loaded[index] = c;
                                break;
                            case < 0:
                                _logger.Log(LogSeverity.Warning,
                                    "A command was added that isn't in UNObot's code. It will be added to the help list, but will not be active.");
                                c.Active = false;
                                _loaded.Add(c);
                                break;
                        }
                    }
                }

                _logger.Log(LogSeverity.Info, $"Loaded {_loaded.Count} commands including from help.json!");
            }
        }

        public async Task ClearHelp()
        {
            _loaded.Clear();
            await LoadHelp(Assembly.GetEntryAssembly(), _provider, true);
        }

        private readonly Dictionary<ulong, List<SlashCommandCreationProperties>> _slashCommands = new();

        private void CreateCommand(SlashCommandAttribute attribute, RequireOwnerAttribute owner)
        {
            if (attribute is not { RegisterSlashCommand: true }) return;
            var builder = attribute.Builder ?? new SlashCommandBuilder();

            if (builder.Name == null)
                builder.WithName(attribute.Text);

            var commands = !_slashCommands.ContainsKey(attribute.Guild) ?
                new List<SlashCommandCreationProperties>()
                : _slashCommands[attribute.Guild];

            builder.WithDefaultPermission(owner == null);
            
            commands.Add(builder.Build());
            
            // If it's new, it'll set it. Otherwise, it'll just place the same reference.
            _slashCommands[attribute.Guild] = commands;
        }

        public async Task RegisterCommands()
        {
            try
            {
                foreach (var guild in _slashCommands.Keys)
                {
                    if (guild == 0)
                        await _discord.Rest.BulkOverwriteGlobalCommands(_slashCommands[0].ToArray());
                    else
                        await _discord.Rest.BulkOverwriteGuildCommands(_slashCommands[guild].ToArray(), guild);
                }
            }
            catch (ApplicationCommandException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);
                _logger.Log(LogSeverity.Error, $"Error trying to create a slash command!\n{json}");
            }
            finally
            {
                _slashCommands.Clear();
            }
        }

        public void Dispose()
        {
            ((IDisposable) _commands)?.Dispose();
            _discord?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}