using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using UNObot.Plugins;
using UNObot.Services;
using UNObot.Templates;

namespace UNObot
{
    public class Program
    {
        private ServiceProvider _services;
        private DiscordSocketClient _client;
        private static readonly ManualResetEvent ExitEvent = new(false);
        private static int ExitCode;
        private IUNObotConfig _config;
        private ILogger _logger;

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
                    MessageCacheSize = 50
                }
            );
            
            var logger = new LoggerService();
            
            _logger = logger;
            
            _config = BuildConfig();
            
            _services = ConfigureServices();

            _logger.Log(LogSeverity.Info, "UNObot Launcher 3.0");

            _client.Log += logger.LogDiscord;

            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();
            
            await _services.GetRequiredService<CommandHandlingService>().InitializeAsync(_services, logger);
            _services.GetRequiredService<WebhookListenerService>();
            _services.GetRequiredService<WatchdogService>().Initialize(logger);
            _services.GetRequiredService<InteractionHandlingService>();

            await _client.SetGameAsync($"UNObot {_config.Version}");
            Console.Title = $"UNObot {_config.Version}";
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
                .AddSingleton<InteractionHandlingService>()
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

        public static void Exit(int code = 0)
        {
            ExitCode = code;
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
            Environment.Exit(ExitCode);
        }

        private IUNObotConfig BuildConfig()
        {
            _logger.Log(LogSeverity.Info, $"Reading files in {Directory.GetCurrentDirectory()}");
            if (!File.Exists("config.json"))
            {
                _logger.Log(LogSeverity.Info,
                    "Config doesn't exist! The file has been created, please edit all fields to be correct. Exiting.");
                new UNObotConfig().Write("config.json");
                Environment.Exit(1);
                return null;
            }

            var config = new UNObotConfig(_logger);
            if (!config.VerifyConfig())
                Environment.Exit(1);

            _logger.Log(LogSeverity.Info, $"Running {config.Version}!");

            return config;
        }
    }
}