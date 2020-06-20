using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UNObot.Plugins.Attributes;
using UNObot.Services;
using UNObot.UNOCore;

namespace UNObot
{
    internal class Program
    {
        //TODO remove???
        private static List<Command> _commands = new List<Command>();

        private ServiceProvider _services;
        private DiscordSocketClient _client;
        private readonly ManualResetEvent _exitEvent = new ManualResetEvent(false);
        private IConfiguration _config;
        private LoggerService _logger;
        private string _version;

        private static async Task Main()
        {
            await new Program().MainAsync();
        }

        private async Task MainAsync()
        {
            _client = new DiscordSocketClient(
                new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = true,
                    DefaultRetryMode = RetryMode.AlwaysRetry,
                    MessageCacheSize = 50,
                    ExclusiveBulkDelete = true
                }
            );
            _config = BuildConfig();
            
            _services = ConfigureServices();

            _logger = _services.GetRequiredService<LoggerService>();
            _logger.Log(LogSeverity.Info, "UNObot Launcher 2.1");

            _client.Log += _logger.LogDiscord;

            await _client.LoginAsync(TokenType.Bot, _config["token"]);
            await _client.StartAsync();
            //_client.ReactionAdded += Modules.InputHandler.ReactionAdded;
            
            await _services.GetRequiredService<CommandHandlingService>().InitializeAsync(_services);

            await _client.SetGameAsync($"UNObot {_version}");
            Console.Title = $"UNObot {_version}";
            await LoadHelp();
            SafeExitHandler();
            _exitEvent.WaitOne();
            _exitEvent.Dispose();
            OnExit();
        }
        
        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(_config)
                .AddSingleton<LoggerService>()
                .AddSingleton(_client)
                .AddSingleton<UNODatabaseService>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<PluginLoaderService>()
                .AddSingleton<EmbedDisplayService>()

                .AddSingleton<UBOWServerLoggerService>()
                .AddSingleton<WebhookListenerService>()
                .AddSingleton<GoogleTranslateService>()
                .AddSingleton<QueueHandlerService>()
                .AddSingleton<RCONManager>()
                .AddSingleton<QueryHandlerService>()
                .AddSingleton<YoutubeService>()
#if DEBUG
                .AddSingleton<DebugService>()
#endif
                .BuildServiceProvider();
        }

        private void SafeExitHandler()
        {
            AppDomain.CurrentDomain.ProcessExit += (o, a) =>
            {
                try
                {
                    _exitEvent.Set();
                }
                catch (Exception)
                {
                    /* ignored */
                }
            };

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                try
                {
                    _exitEvent.Set();
                }
                catch (Exception)
                {
                    /* ignored */
                }
            };
        }

        public void Exit()
        {
            try
            {
                _exitEvent.Set();
            }
            catch (Exception)
            {
                /* ignored */
            }
        }

        private void OnExit()
        {
            _logger.Log(LogSeverity.Info, "Quitting...");
            _services.Dispose();
            Environment.Exit(0);
        }

        private IConfiguration BuildConfig()
        {
            _logger.Log(LogSeverity.Info, $"Reading files in {Directory.GetCurrentDirectory()}");
            if (!File.Exists("config.json"))
            {
                _logger.Log(LogSeverity.Info,
                    "Config doesn't exist! The file has been created, please edit all fields to be correct. Exiting.");
                var obj = new JObject(
                    new JProperty("token", ""),
                    new JProperty("connStr",
                        "server=127.0.0.1;user=UNObot;database=UNObot;port=3306;password=DBPassword"),
                    new JProperty("version", "Unknown Version")
                );
                File.CreateText("config.json").Dispose();
                using (var sr = new StreamWriter("config.json", false))
                    sr.Write(obj);
                Environment.Exit(1);
                return null;
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .AddJsonFile("build.json", true)
                .Build();
            
            var success = true;
            if (string.IsNullOrWhiteSpace(config["token"]))
            {
                _logger.Log(LogSeverity.Error,
                    "Error: Config is missing Bot Token (token)! Please add the property, or update the property to have a token.");
                success = false;
            }

            if (config["connStr"] == null)
            {
                _logger.Log(LogSeverity.Error, "Error: Config is missing Database Connection String (connStr)!");
                success = false;
            }

            if (config["version"] == null)
            {
                _logger.Log(LogSeverity.Error, "Error: Config is missing version (version)!");
                success = false;
            }

            if (!success)
            {
                _logger.Log(LogSeverity.Error, "Please fix all of these errors. Exiting.");
                Environment.Exit(1);
                return null;
            }

            if (config["commit"] == null || config["build"] == null)
            {
                _logger.Log(LogSeverity.Warning,
                    "The build information seems to be missing. Either this is a debug copy, or has been deleted.");
            }

            _version = config["version"] ?? "Unknown Version";
            _logger.Log(LogSeverity.Info, $"Running {config["version"] ?? "an unknown version"}!");

            return config;
        }

        private async Task LoadHelp()
        {
            //TODO Help does not load assemblies loaded from plugins!
            var types = from c in Assembly.GetExecutingAssembly().GetTypes()
                where c.IsClass
                select c;
            foreach (var type in types)
            foreach (var module in type.GetMethods())
            {
                var helpAtt = module.GetCustomAttribute(typeof(HelpAttribute)) as HelpAttribute;
                var aliasAtt = module.GetCustomAttribute(typeof(AliasAttribute)) as AliasAttribute;
                var disableDmsAtt = module.GetCustomAttribute(typeof(DisableDMsAttribute)) as DisableDMsAttribute;

                /*

                    var ownerOnlyAtt = module.GetCustomAttribute(typeof(RequireOwnerAttribute)) as RequireOwnerAttribute;
                    var userPermsAtt = module.GetCustomAttribute(typeof(RequireUserPermissionAttribute)) as RequireUserPermissionAttribute;
                    var remainder = module.GetCustomAttribute(typeof(RemainderAttribute)) as RemainderAttribute;

                    foreach (var pInfo in module.GetParameters())
                    {
                        var name = pInfo.Name;
                    }

                    */

                var aliases = new List<string>();
                //check if it is a command
                if (!(module.GetCustomAttribute(typeof(CommandAttribute)) is CommandAttribute nameAtt)) continue;

                var foundHelp = helpAtt == null ? "Missing help." : "Found help.";
                var disabledForDMs = disableDmsAtt != null;
                _logger.Log(LogSeverity.Verbose, $"Loaded \"{nameAtt.Text}\". {foundHelp}");
                var positionCmd = _commands.FindIndex(o => o.CommandName == nameAtt.Text);
                if (aliasAtt?.Aliases != null)
                    aliases = aliasAtt.Aliases.ToList();
                if (positionCmd < 0)
                {
                    _commands.Add(helpAtt != null
                        ? new Command(nameAtt.Text, aliases, helpAtt.Usages.ToList(), helpAtt.HelpMsg,
                            helpAtt.Active, helpAtt.Version)
                        : new Command(nameAtt.Text, aliases, new List<string> {$".{nameAtt.Text}"},
                            "No help is given for this command.", true, "Unknown Version", disabledForDMs));
                }
                else
                {
                    _commands[positionCmd].DisableDMs = disabledForDMs;
                    if (helpAtt != null)
                    {
                        if (_commands[positionCmd].Help == "No help is given for this command.")
                            _commands[positionCmd].Help = helpAtt.HelpMsg;
                        _commands[positionCmd].Usages =
                            _commands[positionCmd].Usages.Union(helpAtt.Usages.ToList()).ToList();
                        _commands[positionCmd].Active |= helpAtt.Active;
                        if (_commands[positionCmd].Version == "Unknown Version")
                            _commands[positionCmd].Version = helpAtt.Version;
                    }

                    if (aliasAtt != null)
                        _commands[positionCmd].Aliases = _commands[positionCmd].Aliases
                            .Union((aliasAtt.Aliases ?? throw new InvalidOperationException()).ToList()).ToList();
                }
            }

            _commands = _commands.OrderBy(o => o.CommandName).ToList();
            _logger.Log(LogSeverity.Info, $"Loaded {_commands.Count} commands!");

            //Fallback to help.json, ex; Updates, Custom help messages, or temporary troll "fixes"
            if (File.Exists("help.json"))
            {
                _logger.Log(LogSeverity.Info, "Loading help.json into memory...");

                using (var r = new StreamReader("help.json"))
                {
                    var json = await r.ReadToEndAsync();
                    foreach (var c in JsonConvert.DeserializeObject<List<Command>>(json))
                    {
                        var index = _commands.FindIndex(o => o.CommandName == c.CommandName);
                        if (index >= 0 && _commands[index].Help == "No help is given for this command.")
                        {
                            _commands[index] = c;
                        }
                        else if (index < 0)
                        {
                            _logger.Log(LogSeverity.Warning,
                                "A command was added that isn't in UNObot's code. It will be added to the help list, but will not be active.");
                            var newCommand = c;
                            newCommand.Active = false;
                            _commands.Add(newCommand);
                        }
                    }
                }

                _logger.Log(LogSeverity.Info, $"Loaded {_commands.Count} commands including from help.json!");
            }
        }
    }
}