using System;
using System.Timers;
using Discord;
using Discord.WebSocket;
using static Discord.ConnectionState;

namespace UNObot.Services
{
    public class WatchdogService : IDisposable
    {
        private readonly DiscordSocketClient _client;
        private LoggerService _logger;
        private readonly Timer _watchdog;

        private const int Timeout = 2 * 60 * 1000;
        
        public WatchdogService(DiscordSocketClient client)
        {
            _client = client;
            _watchdog = new Timer
            {
                AutoReset = true,
                Interval = Timeout,
                Enabled = true
            };
            
            _watchdog.Elapsed += WatchdogCheck;
        }
        
        public void InitializeAsync(LoggerService logger)
        {
            _logger = logger;
            _logger.Log(LogSeverity.Info, $"Watchdog woke up! Check delay: {Timeout / 1000} seconds.");
        }

        private void WatchdogCheck(object sender, ElapsedEventArgs e)
        {
            if (_client.ConnectionState == Disconnected || _client.ConnectionState == Disconnecting)
            {
                _logger.Log(LogSeverity.Critical, $"Watchdog not fed in the last {Timeout / 1000} seconds! Quitting!");
                Program.Exit();
            }
        }

        public void Dispose()
        {
            _watchdog?.Dispose();
        }
    }
}