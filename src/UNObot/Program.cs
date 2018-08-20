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

namespace UNObot
{
    class Program
    {
        static Modules.UNOdb db = new Modules.UNOdb();
        public static string version = "Unknown Version";
        public static List<Modules.Command> commands = new List<Modules.Command>();
        /*
        public static int currentPlayer;
        //1: Clockwise 2: Counter-Clockwise
        public static byte order = 1;
        public static bool gameStarted;
        public static Modules.Card currentcard;
        public static ulong onecardleft;
        */
        static void Main()
        {
            Console.WriteLine("UNObot Launcher 1.0");
            //TODO generate new config if it doesn't exist
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public static DiscordSocketClient _client;
        IConfiguration _config;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _config = BuildConfig();

            var services = ConfigureServices();
            services.GetRequiredService<LogService>();
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);

            await _client.LoginAsync(TokenType.Bot, _config["token"]);
            await _client.StartAsync();
            await db.CleanAll();
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
                .AddSingleton<CommandHandlingService>()
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

        static async Task LoadHelp()
        {
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