﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using UNObot.Services;

namespace UNObot
{
    internal class Program
    {
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
            
            _logger = new LoggerService();
            
            _config = BuildConfig();
            
            _services = ConfigureServices();

            _logger.Log(LogSeverity.Info, "UNObot Launcher 3.0");

            _client.Log += _logger.LogDiscord;

            await _client.LoginAsync(TokenType.Bot, _config["token"]);
            await _client.StartAsync();
            //_client.ReactionAdded += _services.GetRequiredService<InputHandler>().ReactionAdded;
            await _services.GetRequiredService<CommandHandlingService>().InitializeAsync(_services);

            await _client.SetGameAsync($"UNObot {_version}");
            Console.Title = $"UNObot {_version}";
            SafeExitHandler();
            _exitEvent.WaitOne();
            _exitEvent.Dispose();
            OnExit();
        }
        
        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(_config)
                .AddSingleton(_logger)
                .AddSingleton(_client)
                .AddSingleton<UNODatabaseService>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<PluginLoaderService>()
                .AddSingleton<EmbedDisplayService>()
                .AddSingleton<MusicBotService>()
                .AddSingleton<MinecraftProcessorService>()
                .AddSingleton<ShellService>()
                .AddSingleton<UBOWServerLoggerService>()
                .AddSingleton<WebhookListenerService>()
                .AddSingleton<GoogleTranslateService>()
                .AddSingleton<QueueHandlerService>()
                .AddSingleton<UNOPlayCardService>()
                .AddSingleton<AFKTimerService>()
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

        internal void Exit()
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
                _logger.Log(LogSeverity.Error, "Error: Config is missing a Database Connection String (connStr)!");
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
    }
}