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

namespace UNObot
{
    class Program
    {
        static Modules.UNOdb db = new Modules.UNOdb();
        public static string version = "Unknown Version";
        public static List<Modules.Command> commands = new List<Modules.Command>();

        static async Task Main()
        {
            Console.WriteLine("UNObot Launcher 1.0");
            //TODO generate new config if it doesn't exist
            await new Program().MainAsync();
        }

        public static DiscordSocketClient _client;
        IConfiguration _config;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _config = BuildConfig();

            var services = ConfigureServices();
            services.GetRequiredService<LogService>();
            await services.GetRequiredService<Services.CommandHandlingService>().InitializeAsync(services);

            await _client.LoginAsync(TokenType.Bot, _config["token"]);
            await _client.StartAsync();
            await db.CleanAll();
            await _client.SetGameAsync($"UNObot {version}");
            LoadHelp();
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
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build();
        }
        static void LoadHelp()
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
            Console.WriteLine($"Loaded {commands.Count} commands!");
            /* Old help.json method
            if (File.Exists("help.json"))
            {
                Console.WriteLine("Loading help.json into memory...");

                using (StreamReader r = new StreamReader("help.json"))
                {
                    string json = await r.ReadToEndAsync();
                    commands = JsonConvert.DeserializeObject<List<Modules.Command>>(json);
                }
                Console.WriteLine($"Loaded {commands.Count} commands!");
            }
            else
            {
                Console.WriteLine("WARNING: help.json didn't exist! Creating help.json...");
                using (StreamWriter sw = File.CreateText("help.json"))
                    await sw.WriteLineAsync("[]");
                Console.WriteLine("File created! Please generate your own via the help tool.");
            }
            */
        }
        public static async Task SendMessage(string text, ulong server)
        {
            ulong channel = 0;
            channel = _client.GetGuild(server).DefaultChannel.Id;
            Console.WriteLine($"Channel: {channel}");
            if (await db.HasDefaultChannel(server))
                channel = await db.GetDefaultChannel(server);
            Console.WriteLine($"Channel: {channel}");
            await _client.GetGuild(server).GetTextChannel(channel).SendMessageAsync(text);
        }
        public static async Task SendPM(string text, ulong user)
            => await UserExtensions.SendMessageAsync(_client.GetUser(user), text);
    }
}