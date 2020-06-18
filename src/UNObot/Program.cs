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

namespace UNObot
{
    internal class Program
    {
        public static string Version = "Unknown Version";
        public static string Commit = "Unknown Commit";
        public static string Build = "???";
        public static List<Command> Commands = new List<Command>();

        public static IServiceProvider Services;
        public static DiscordSocketClient Client;
        private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
        private IConfiguration _config;

        private static async Task Main()
        {
            LoggerService.GetSingleton();
            LoggerService.Log(LogSeverity.Info, "UNObot Launcher 2.1");
            await new Program().MainAsync();
        }

        private async Task MainAsync()
        {
            Client = new DiscordSocketClient(
                new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = true,
                    DefaultRetryMode = RetryMode.AlwaysRetry,
                    MessageCacheSize = 50,
                    ExclusiveBulkDelete = true
                }
            );
            _config = BuildConfig();

            Client.Log += LoggerService.GetSingleton().LogDiscord;

            Services = ConfigureServices();
            await Services.GetRequiredService<CommandHandlingService>().InitializeAsync(Services);
            await Client.LoginAsync(TokenType.Bot, _config["token"]);
            await Client.StartAsync();
            //_client.ReactionAdded += Modules.InputHandler.ReactionAdded;
#if DEBUG
            DebugService.GetSingleton();
#endif
            PluginLoaderService.GetSingleton();
            UBOWServerLoggerService.GetSingleton();
            WebhookListenerService.GetSingleton();
            await UNODatabaseService.CleanAll();
            await Client.SetGameAsync($"UNObot {Version}");
            Console.Title = $"UNObot {Version}";
            await LoadHelp();
            SafeExitHandler();
            ExitEvent.WaitOne();
            ExitEvent.Dispose();
            await OnExit();
        }

        private static void SafeExitHandler()
        {
            AppDomain.CurrentDomain.ProcessExit += (o, a) =>
            {
                try
                {
                    ExitEvent.Set();
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
                    ExitEvent.Set();
                }
                catch (Exception)
                {
                    /* ignored */
                }
            };
        }

        public static void Exit()
        {
            try
            {
                ExitEvent.Set();
            }
            catch (Exception)
            {
                /* ignored */
            }
        }

        private static async Task OnExit()
        {
            LoggerService.Log(LogSeverity.Info, "Quitting...");
            await MusicBotService.GetSingleton().DisposeAsync();
            LoggerService.Log(LogSeverity.Info, "Music Bot service disabled.");
            WebhookListenerService.GetSingleton().Dispose();
            LoggerService.Log(LogSeverity.Info, "Webhook Listener service disabled.");
            RCONManager.GetSingleton().Dispose();
            LoggerService.Log(LogSeverity.Info, "RCON service disabled.");
            await Client.StopAsync().ConfigureAwait(false);
            Client.Dispose();
            LoggerService.Log(LogSeverity.Info, "Quit successfully.");
            LoggerService.GetSingleton().Dispose();
            Environment.Exit(0);
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                // Base
                .AddSingleton(Client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<GoogleTranslateService>()
                // Extra
                .AddSingleton(_config)
                // Add additional services here...
                .BuildServiceProvider();
        }

        private static IConfiguration BuildConfig()
        {
            LoggerService.Log(LogSeverity.Info, $"Reading files in {Directory.GetCurrentDirectory()}");
            if (!File.Exists("config.json"))
            {
                LoggerService.Log(LogSeverity.Info,
                    "Config doesn't exist! The file has been created, please edit all fields to be correct. Exiting.");
                var obj = new JObject(new JProperty("token", "yourPrivateKey"),
                    new JProperty("connStr",
                        "server=127.0.0.1;user=UNObot;database=UNObot;port=3306;password=DBPassword"),
                    new JProperty("version", "Unknown Version")
                );
                File.CreateText("config.json").Dispose();
                using (var sr = new StreamWriter("config.json", false))
                {
                    sr.Write(obj);
                }

                Environment.Exit(1);
                return null;
            }

            var json = JObject.Parse(File.ReadAllText("config.json"));
            var errors = 0;
            if (!json.ContainsKey("token") || json["token"]?.ToString() == "Replace With Private Key")
            {
                LoggerService.Log(LogSeverity.Error,
                    "Error: Config is missing Bot Token (token)! Please add the property, or update the property to have a token.");
                errors++;
            }

            if (!json.ContainsKey("connStr"))
            {
                LoggerService.Log(LogSeverity.Error, "Error: Config is missing Database Connection String (connStr)!");
                errors++;
            }

            if (!json.ContainsKey("version"))
            {
                LoggerService.Log(LogSeverity.Error, "Error: Config is missing version (version)!");
                errors++;
            }

            if (errors != 0)
            {
                LoggerService.Log(LogSeverity.Error, "Please fix all of these errors. Exiting.");
                Environment.Exit(1);
                return null;
            }

            if (!File.Exists("commit"))
            {
                LoggerService.Log(LogSeverity.Warning,
                    "The build information seems to be missing. Either this is a debug copy, or has been deleted.");
            }
            else
            {
                var (commit, build) = ReadCommitBuild();
                Commit = commit;
                Build = build;
            }

            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build();
        }

        private static async Task LoadHelp()
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
                LoggerService.Log(LogSeverity.Verbose, $"Loaded \"{nameAtt.Text}\". {foundHelp}");
                var positionCmd = Commands.FindIndex(o => o.CommandName == nameAtt.Text);
                if (aliasAtt?.Aliases != null)
                    aliases = aliasAtt.Aliases.ToList();
                if (positionCmd < 0)
                {
                    Commands.Add(helpAtt != null
                        ? new Command(nameAtt.Text, aliases, helpAtt.Usages.ToList(), helpAtt.HelpMsg,
                            helpAtt.Active, helpAtt.Version)
                        : new Command(nameAtt.Text, aliases, new List<string> {$".{nameAtt.Text}"},
                            "No help is given for this command.", true, "Unknown Version", disabledForDMs));
                }
                else
                {
                    Commands[positionCmd].DisableDMs = disabledForDMs;
                    if (helpAtt != null)
                    {
                        if (Commands[positionCmd].Help == "No help is given for this command.")
                            Commands[positionCmd].Help = helpAtt.HelpMsg;
                        Commands[positionCmd].Usages =
                            Commands[positionCmd].Usages.Union(helpAtt.Usages.ToList()).ToList();
                        Commands[positionCmd].Active |= helpAtt.Active;
                        if (Commands[positionCmd].Version == "Unknown Version")
                            Commands[positionCmd].Version = helpAtt.Version;
                    }

                    if (aliasAtt != null)
                        Commands[positionCmd].Aliases = Commands[positionCmd].Aliases
                            .Union((aliasAtt.Aliases ?? throw new InvalidOperationException()).ToList()).ToList();
                }
            }

            Commands = Commands.OrderBy(o => o.CommandName).ToList();
            LoggerService.Log(LogSeverity.Info, $"Loaded {Commands.Count} commands!");

            //Fallback to help.json, ex; Updates, Custom help messages, or temporary troll "fixes"
            if (File.Exists("help.json"))
            {
                LoggerService.Log(LogSeverity.Info, "Loading help.json into memory...");

                using (var r = new StreamReader("help.json"))
                {
                    var json = await r.ReadToEndAsync();
                    foreach (var c in JsonConvert.DeserializeObject<List<Command>>(json))
                    {
                        var index = Commands.FindIndex(o => o.CommandName == c.CommandName);
                        if (index >= 0 && Commands[index].Help == "No help is given for this command.")
                        {
                            Commands[index] = c;
                        }
                        else if (index < 0)
                        {
                            LoggerService.Log(LogSeverity.Warning,
                                "A command was added that isn't in UNObot's code. It will be added to the help list, but will not be active.");
                            var newCommand = c;
                            newCommand.Active = false;
                            Commands.Add(newCommand);
                        }
                    }
                }

                LoggerService.Log(LogSeverity.Info, $"Loaded {Commands.Count} commands including from help.json!");
            }
        }

        public static (string Commit, string Build) ReadCommitBuild()
        {
            var output = ("Unknown Commit", "???");
            
            if (!File.Exists("commit")) return output;
            
            try
            {
                using var sr = new StreamReader("commit");
                if (sr.EndOfStream)
                    throw new Exception("");
                var input = sr.ReadLine();
                if (input == null)
                    throw new Exception();
                var words = input.Split(' ');
                if (words.Length < 2 || words[0].Length < 7)
                    throw new Exception();
                output = (words[0].Trim().Substring(0, 7), words[1].Trim());
            }
            catch (Exception)
            {
                LoggerService.Log(LogSeverity.Error, "Build information file has not been created properly.");
            }

            return output;
        }

        public static async Task SendMessage(string text, ulong server)
        {
            var channel = Client.GetGuild(server).DefaultChannel.Id;
            LoggerService.Log(LogSeverity.Info, $"Channel: {channel}");
            if (await UNODatabaseService.HasDefaultChannel(server))
                channel = await UNODatabaseService.GetDefaultChannel(server);
            LoggerService.Log(LogSeverity.Info, $"Channel: {channel}");

            try
            {
                await Client.GetGuild(server).GetTextChannel(channel).SendMessageAsync(text);
            }
            catch (Exception)
            {
                try
                {
                    await Client.GetGuild(server).GetTextChannel(Client.GetGuild(server).DefaultChannel.Id)
                        .SendMessageAsync(text);
                }
                catch (Exception)
                {
                    LoggerService.Log(LogSeverity.Error,
                        "Ok what the heck is this? Can't post in the default OR secondary channel?");
                }
            }
        }

        public static async Task SendPM(string text, ulong user)
        {
            await Client.GetUser(user).SendMessageAsync(text);
        }
    }
}