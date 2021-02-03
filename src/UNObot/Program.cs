using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using UNObot.Plugins;
using UNObot.Services;

namespace UNObot
{
    internal class Program
    {
        private ServiceProvider _services;
        private DiscordSocketClient _client;
        private static readonly ManualResetEvent ExitEvent = new(false);
        private IConfiguration _config;
        private ILogger _logger;
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
            
            var logger = new LoggerService();
            
            _logger = logger;
            
            _config = BuildConfig();
            
            _services = ConfigureServices();

            _logger.Log(LogSeverity.Info, "UNObot Launcher 3.0");

            _client.Log += logger.LogDiscord;

            await _client.LoginAsync(TokenType.Bot, _config["token"]);
            await _client.StartAsync();
            await _services.GetRequiredService<CommandHandlingService>().InitializeAsync(_services, logger);
            _services.GetRequiredService<WebhookListenerService>();
            _services.GetRequiredService<WatchdogService>().InitializeAsync(logger);

            await _client.SetGameAsync($"UNObot {_version}");
            Console.Title = $"UNObot {_version}";
            SafeExitHandler();
            ExitEvent.WaitOne();
            ExitEvent.Dispose();
            OnExit();
        }
        
        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(_config)
                .AddSingleton(_logger)
                .AddSingleton(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<PluginLoaderService>()
                .AddSingleton<EmbedDisplayService>()
                .AddSingleton<DatabaseService>()
                .AddSingleton<ShellService>()
                .AddSingleton<WebhookListenerService>()
                .AddSingleton<GoogleTranslateService>()
                .AddSingleton<WatchdogService>()
#if DEBUG
                .AddSingleton<DebugService>()
#endif
                .BuildServiceProvider();
        }

        private void SafeExitHandler()
        {
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
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

            Console.CancelKeyPress += (_, eventArgs) =>
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

        internal static void Exit()
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

        private void OnExit()
        {
            _logger.Log(LogSeverity.Info, "Quitting...");
            foreach (var service in _services.GetServices<object>())
            {
                switch (service)
                {
                    case IAsyncDisposable objDisposableAsync:
                        objDisposableAsync.DisposeAsync().GetAwaiter().GetResult();
                        break;
                    case IDisposable objDisposable:
                        objDisposable.Dispose();
                        break;
                }
            }
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
                using var sr = File.CreateText("config.json");
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