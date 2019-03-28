using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Services;
using Newtonsoft.Json;
using System.Reflection;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;
using UNObot.Modules;

#pragma warning disable CS1701 // Assuming assembly reference matches identity
#pragma warning disable CS1702 // Assuming assembly reference matches identity

namespace UNObot
{
    class Program
    {
        public static string version = "Unknown Version";
        public static string commit = "Unknown Commit";
        public static string build = "Unknown Build";
        public static List<Modules.Command> commands = new List<Modules.Command>();

        static async Task Main()
        {
            Console.WriteLine("UNObot Launcher 1.0");
            await new Program().MainAsync();
        }

        public static DiscordSocketClient _client;
        IConfiguration _config;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(
                new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = true,
                    DefaultRetryMode = RetryMode.AlwaysRetry,
                    MessageCacheSize = 50
                }
            );
            _config = BuildConfig();

            var services = ConfigureServices();
            services.GetRequiredService<LogService>();
            await services.GetRequiredService<Services.CommandHandlingService>().InitializeAsync(services);

            await _client.LoginAsync(TokenType.Bot, _config["token"]);
            await _client.StartAsync();
            //_client.ReactionAdded += Modules.InputHandler.ReactionAdded;
            await UNOdb.CleanAll();
            await _client.SetGameAsync($"UNObot {version}");
            await LoadHelp();
            await Task.Delay(-1);
        }

        IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                // Base
                .AddSingleton(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<Services.CommandHandlingService>()
                // Logging
                .AddLogging()
                .AddSingleton<LogService>()
                // Extra
                .AddSingleton(_config)
                // Add additional services here...
                .BuildServiceProvider();
        }

        IConfiguration BuildConfig()
        {
            Console.WriteLine($"Reading files in {Directory.GetCurrentDirectory()}");
            if (!File.Exists("config.json"))
            {
                Console.WriteLine("Config doesn't exist! The file has been created, please edit all fields to be correct. Exiting.");
                JObject obj = new JObject(new JProperty("token", "Replace With Private Key"),
                                          new JProperty("connStr", "server=127.0.0.1;user=UNObot;database=UNObot;port=3306;password=DBPassword"),
                                          new JProperty("version", "Unknown Version")
                                         );
                File.CreateText("config.json");
                using (StreamWriter sr = new StreamWriter("config.json", false))
                    sr.Write(obj);
                Environment.Exit(1);
                return null;
            }
            var json = JObject.Parse(File.ReadAllText("config.json"));
            int errors = 0;
            if (!json.ContainsKey("token") || json["token"].ToString() == "Replace With Private Key")
            {
                Console.WriteLine("Error: Config is missing Bot Token (token)! Please add the property, or update the property to have a token."); errors++;
            }
            if (!json.ContainsKey("connStr"))
            {
                Console.WriteLine("Error: Config is missing Database Connection String (connStr)!"); errors++;
            }
            if (!json.ContainsKey("version"))
            {
                Console.WriteLine("Error: Config is missing version (version)!"); errors++;
            }
            if (errors != 0)
            {
                Console.WriteLine("Please fix all of these errors. Exiting.");
                Environment.Exit(1);
                return null;
            }
            if (!File.Exists("commit"))
            {
                Console.WriteLine("The build information seems to be missing. Either this is a debug copy, or has been deleted.");
            }
            else
            {
                try
                {
                    using (StreamReader sr = new StreamReader("commit"))
                    {
                        if (sr.EndOfStream)
                            throw new Exception();
                        var input = sr.ReadLine();
                        if (input == null)
                            throw new Exception();
                        var words = input.Split(' ');
                        if (words.Length < 2 || words[0].Length < 7)
                            throw new Exception();
                        commit = words[0].Trim().Substring(0, 7);
                        build = words[1].Trim();
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Build information file has not been created properly.");
                }
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
                    var helpatt = module.GetCustomAttribute(typeof(Modules.Help)) as Modules.Help;
                    var aliasatt = module.GetCustomAttributes(typeof(AliasAttribute)) as AliasAttribute;
                    var owneronlyatt = module.GetCustomAttribute(typeof(RequireOwnerAttribute)) as RequireOwnerAttribute;
                    var userpermsatt = module.GetCustomAttributes(typeof(RequireUserPermissionAttribute)) as RequireUserPermissionAttribute;
                    var remainder = module.GetCustomAttribute(typeof(RemainderAttribute)) as RemainderAttribute;
                    foreach (var pinfo in module.GetParameters())
                    {
                        var name = pinfo.Name;
                    }
                    var aliases = new List<string>();
                    //check if it is a command
                    if (module.GetCustomAttribute(typeof(CommandAttribute)) is CommandAttribute nameatt)
                    {
                        string foundHelp = helpatt == null ? "Missing help." : "Found help.";
                        Console.WriteLine($"Loaded \"{nameatt.Text}\". {foundHelp}");
                        int positioncmd = commands.FindIndex(o => o.CommandName == nameatt.Text);
                        if (aliasatt != null && aliasatt.Aliases != null)
                            aliases = aliasatt.Aliases.ToList();
                        if (positioncmd < 0)
                        {
                            if (helpatt != null)
                                commands.Add(new Modules.Command(nameatt.Text, aliases, helpatt.Usages.ToList(), helpatt.HelpMsg, helpatt.Active, helpatt.Version));
                            else
                                commands.Add(new Modules.Command(nameatt.Text, aliases, new List<string> { $".{nameatt.Text}" }, "No help is given for this command.", true, "Unknown Version"));
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
                                commands[positioncmd].Aliases = commands[positioncmd].Aliases.Union(aliasatt.Aliases.ToList()).ToList();
                        }
                    }
                }
            }
            commands = commands.OrderBy(o => o.CommandName).ToList();
            Console.WriteLine($"Loaded {commands.Count} commands!");

            //Fallback to help.json
            if (File.Exists("help.json"))
            {
                Console.WriteLine("Loading help.json into memory...");

                using (StreamReader r = new StreamReader("help.json"))
                {
                    string json = await r.ReadToEndAsync();
                    foreach (Modules.Command c in JsonConvert.DeserializeObject<List<Modules.Command>>(json))
                    {
                        var index = commands.FindIndex(o => o.CommandName == c.CommandName);
                        if (index >= 0 && commands[index].Help == "No help is given for this command.")
                            commands[index] = c;
                        else if (index < 0)
                        {
                            Console.WriteLine("A command was added that isn't in UNObot's code. It will be added to the help list, but will not be active.");
                            var newcommand = c;
                            newcommand.Active = false;
                            commands.Add(newcommand);
                        }
                    }
                }
                Console.WriteLine($"Loaded {commands.Count} commands including from help.json!");
            }
        }
        public static async Task SendMessage(string text, ulong server)
        {
            ulong channel = 0;
            channel = _client.GetGuild(server).DefaultChannel.Id;
            Console.WriteLine($"Channel: {channel}");
            if (await UNOdb.HasDefaultChannel(server))
                channel = await UNOdb.GetDefaultChannel(server);
            Console.WriteLine($"Channel: {channel}");
            await _client.GetGuild(server).GetTextChannel(channel).SendMessageAsync(text);
        }
        public static async Task SendPM(string text, ulong user)
            => await UserExtensions.SendMessageAsync(_client.GetUser(user), text);
    }
}