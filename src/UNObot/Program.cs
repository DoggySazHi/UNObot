using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UNObot.Services;

namespace UNObot
{
    class Program
    {
        public static string version = "Unknown Version";
        public static string Commit = "Unknown Commit";
        public static string Build = "???";
        public static List<Command> commands = new List<Command>();

        static async Task Main()
        {
            LoggerService.GetSingleton();
            LoggerService.Log(LogSeverity.Info, "UNObot Launcher 2.0");
            await new Program().MainAsync();
        }

        public static DiscordSocketClient _client;
        IConfiguration _config;
        private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);

        public async Task MainAsync()
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

            _client.Log += LoggerService.GetSingleton().LogDiscord;

            var services = ConfigureServices();
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);

            await _client.LoginAsync(TokenType.Bot, _config["token"]);
            await _client.StartAsync();
            //_client.ReactionAdded += Modules.InputHandler.ReactionAdded;
#if DEBUG
            DebugService.GetSingleton();
#endif
            UBOWServerLoggerService.GetSingleton();
            await UNODatabaseService.CleanAll();
            await _client.SetGameAsync($"UNObot {version}");
            await LoadHelp();
            SafeExitHandler();
            ExitEvent.WaitOne();
            ExitEvent.Dispose();
            await OnExit();
        }

        private void SafeExitHandler()
        {
            AppDomain.CurrentDomain.ProcessExit += (o, a) => { try { ExitEvent.Set(); } catch (Exception) { /* ignored */ } };

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                try { ExitEvent.Set(); } catch (Exception) { /* ignored */ }
            };
        }

        public static void Exit()
        {
            try { ExitEvent.Set(); } catch (Exception) { /* ignored */ }
        }

        private static async Task OnExit()
        {
            LoggerService.Log(LogSeverity.Info, "Quitting...");
            await MusicBotService.GetSingleton().DisposeAsync();
            await _client.StopAsync().ConfigureAwait(false);
            _client.Dispose();
            LoggerService.Log(LogSeverity.Info, "Quit successfully.");
            LoggerService.GetSingleton().Dispose();
            Environment.Exit(0);
        }

        IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                // Base
                .AddSingleton(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                // Extra
                .AddSingleton(_config)
                // Add additional services here...
                .BuildServiceProvider();
        }

        IConfiguration BuildConfig()
        {
            LoggerService.Log(LogSeverity.Info, $"Reading files in {Directory.GetCurrentDirectory()}");
            if (!File.Exists("config.json"))
            {
                LoggerService.Log(LogSeverity.Info, "Config doesn't exist! The file has been created, please edit all fields to be correct. Exiting.");
                JObject obj = new JObject(new JProperty("token", "Replace With Private Key"),
                                          new JProperty("connStr", "server=127.0.0.1;user=UNObot;database=UNObot;port=3306;password=DBPassword"),
                                          new JProperty("version", "Unknown Version")
                                         );
                File.CreateText("config.json").Dispose();
                using (StreamWriter sr = new StreamWriter("config.json", false))
                    sr.Write(obj);
                Environment.Exit(1);
                return null;
            }
            var json = JObject.Parse(File.ReadAllText("config.json"));
            int errors = 0;
            if (!json.ContainsKey("token") || json["token"].ToString() == "Replace With Private Key")
            {
                LoggerService.Log(LogSeverity.Error, "Error: Config is missing Bot Token (token)! Please add the property, or update the property to have a token."); errors++;
            }
            if (!json.ContainsKey("connStr"))
            {
                LoggerService.Log(LogSeverity.Error, "Error: Config is missing Database Connection String (connStr)!"); errors++;
            }
            if (!json.ContainsKey("version"))
            {
                LoggerService.Log(LogSeverity.Error, "Error: Config is missing version (version)!"); errors++;
            }
            if (errors != 0)
            {
                LoggerService.Log(LogSeverity.Error, "Please fix all of these errors. Exiting.");
                Environment.Exit(1);
                return null;
            }
            if (!File.Exists("commit"))
            {
                LoggerService.Log(LogSeverity.Warning, "The build information seems to be missing. Either this is a debug copy, or has been deleted.");
            }
            else
            {
                var Result = ReadCommitBuild();
                Commit = Result.Commit;
                Build = Result.Build;
            }
            return new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("config.json")
                        .Build();
        }
        static async Task LoadHelp()
        {
            var types = from c in Assembly.GetExecutingAssembly().GetTypes()
                        where c.IsClass
                        select c;
            foreach (var type in types)
            {
                foreach (var module in type.GetMethods())
                {
                    var helpatt = module.GetCustomAttribute(typeof(Help)) as Help;
                    var aliasatt = module.GetCustomAttribute(typeof(AliasAttribute)) as AliasAttribute;

                    /*

                    var owneronlyatt = module.GetCustomAttribute(typeof(RequireOwnerAttribute)) as RequireOwnerAttribute;
                    var userpermsatt = module.GetCustomAttribute(typeof(RequireUserPermissionAttribute)) as RequireUserPermissionAttribute;
                    var remainder = module.GetCustomAttribute(typeof(RemainderAttribute)) as RemainderAttribute;

                    foreach (var pinfo in module.GetParameters())
                    {
                        var name = pinfo.Name;
                    }

                    */

                    var aliases = new List<string>();
                    //check if it is a command
                    if (!(module.GetCustomAttribute(typeof(CommandAttribute)) is CommandAttribute nameatt)) continue;

                    var foundHelp = helpatt == null ? "Missing help." : "Found help.";
                    LoggerService.Log(LogSeverity.Verbose, $"Loaded \"{nameatt.Text}\". {foundHelp}");
                    var positioncmd = commands.FindIndex(o => o.CommandName == nameatt.Text);
                    if (aliasatt?.Aliases != null)
                        aliases = aliasatt.Aliases.ToList();
                    if (positioncmd < 0)
                    {
                        commands.Add(helpatt != null
                            ? new Command(nameatt.Text, aliases, helpatt.Usages.ToList(), helpatt.HelpMsg,
                                helpatt.Active, helpatt.Version)
                            : new Command(nameatt.Text, aliases, new List<string> {$".{nameatt.Text}"},
                                "No help is given for this command.", true, "Unknown Version"));
                    }
                    else
                    {
                        if (helpatt != null)
                        {
                            if (commands[positioncmd].Help == "No help is given for this command.")
                                commands[positioncmd].Help = helpatt.HelpMsg;
                            commands[positioncmd].Usages = commands[positioncmd].Usages.Union(helpatt.Usages.ToList()).ToList();
                            commands[positioncmd].Active |= helpatt.Active;
                            if (commands[positioncmd].Version == "Unknown Version")
                                commands[positioncmd].Version = helpatt.Version;
                        }
                        if (aliasatt != null)
                            commands[positioncmd].Aliases = commands[positioncmd].Aliases.Union((aliasatt.Aliases ?? throw new InvalidOperationException()).ToList()).ToList();
                    }
                }
            }
            commands = commands.OrderBy(o => o.CommandName).ToList();
            LoggerService.Log(LogSeverity.Info, $"Loaded {commands.Count} commands!");

            //Fallback to help.json, ex; Updates, Custom help messages, or temporary troll "fixes"
            if (File.Exists("help.json"))
            {
                LoggerService.Log(LogSeverity.Info, "Loading help.json into memory...");

                using (StreamReader r = new StreamReader("help.json"))
                {
                    string json = await r.ReadToEndAsync();
                    foreach (Command c in JsonConvert.DeserializeObject<List<Command>>(json))
                    {
                        var index = commands.FindIndex(o => o.CommandName == c.CommandName);
                        if (index >= 0 && commands[index].Help == "No help is given for this command.")
                            commands[index] = c;
                        else if (index < 0)
                        {
                            LoggerService.Log(LogSeverity.Warning, "A command was added that isn't in UNObot's code. It will be added to the help list, but will not be active.");
                            var newcommand = c;
                            newcommand.Active = false;
                            commands.Add(newcommand);
                        }
                    }
                }
                LoggerService.Log(LogSeverity.Info, $"Loaded {commands.Count} commands including from help.json!");
            }
        }

        public static (string Commit, string Build) ReadCommitBuild()
        {
            try
            {
                using StreamReader sr = new StreamReader("commit");
                if (sr.EndOfStream)
                    throw new Exception("");
                var input = sr.ReadLine();
                if (input == null)
                    throw new Exception();
                var words = input.Split(' ');
                if (words.Length < 2 || words[0].Length < 7)
                    throw new Exception();
                return (words[0].Trim().Substring(0, 7), words[1].Trim());
            }
            catch (Exception)
            {
                LoggerService.Log(LogSeverity.Error, "Build information file has not been created properly.");
            }
            return ("Unknown Commit", "???");
        }

        public static async Task SendMessage(string text, ulong server)
        {
            ulong channel = _client.GetGuild(server).DefaultChannel.Id;
            LoggerService.Log(LogSeverity.Info, $"Channel: {channel}");
            if (await UNODatabaseService.HasDefaultChannel(server))
                channel = await UNODatabaseService.GetDefaultChannel(server);
            LoggerService.Log(LogSeverity.Info, $"Channel: {channel}");

            try
            {
                await _client.GetGuild(server).GetTextChannel(channel).SendMessageAsync(text);
            }
            catch (Exception)
            {
                try
                {
                   await _client.GetGuild(server).GetTextChannel(_client.GetGuild(server).DefaultChannel.Id).SendMessageAsync(text);
                }
                catch (Exception)
                {
                    LoggerService.Log(LogSeverity.Error, "Ok what the heck is this? Can't post in the default OR secondary channel?");
                }
            }
        }
        public static async Task SendPM(string text, ulong user)
            => await _client.GetUser(user).SendMessageAsync(text);
    }
}