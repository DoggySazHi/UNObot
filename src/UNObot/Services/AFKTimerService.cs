using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Timer = System.Timers.Timer;

namespace UNObot.Services
{
    internal class AFKTimerService
    {
        private static Dictionary<ulong, Timer> PlayTimers = new Dictionary<ulong, Timer>();
        private LoggerService _logger;
        private UNODatabaseService _db;
        private QueueHandlerService _queue;
        private DiscordSocketClient _client;

        internal AFKTimerService(LoggerService logger, UNODatabaseService db, QueueHandlerService queue, DiscordSocketClient client)
        {
            _logger = logger;
            _db = db;
            _queue = queue;
            _client = client;
        }

        void ResetTimer(ulong server)
        {
            if (!PlayTimers.ContainsKey(server))
            {
                _logger.Log(LogSeverity.Error, "Attempted to reset timer that doesn't exist!");
            }
            else
            {
                PlayTimers[server].Stop();
                PlayTimers[server].Start();
            }
        }

        void StartTimer(ulong server)
        {
            _logger.Log(LogSeverity.Debug, "Starting timer!");
            if (PlayTimers.ContainsKey(server))
            {
                _logger.Log(LogSeverity.Warning, "Attempted to start timer that already existed!");
            }
            else
            {
                PlayTimers[server] = new Timer
                {
                    Interval = 90000,
                    AutoReset = false
                };
                PlayTimers[server].Elapsed += TimerOver;
                PlayTimers[server].Start();
            }
        }

        private async void TimerOver(object source, ElapsedEventArgs e)
        {
            _logger.Log(LogSeverity.Debug, "Timer over!");
            ulong serverId = 0;
            foreach (var server in PlayTimers.Keys)
            {
                var timer = (Timer) source;
                if (timer.Equals(PlayTimers[server]))
                    serverId = server;
            }

            if (serverId == 0)
            {
                _logger.Log(LogSeverity.Error, "Couldn't figure out what server timer belonged to!");
                return;
            }

            var currentPlayer = await _queue.GetCurrentPlayer(serverId);
            await _db.RemoveUser(currentPlayer);
            await _queue.DropFrontPlayer(serverId);
            _logger.Log(LogSeverity.Debug, "SayPlayer");
            await SendMessage($"<@{currentPlayer}>, you have been AFK removed.\n", serverId);
            await SendDM("You have been AFK removed.", currentPlayer);
            if (await _queue.PlayerCount(serverId) == 0)
            {
                await _db.ResetGame(serverId);
                await SendMessage("Game has been reset, due to nobody in-game.", serverId);
                DeleteTimer(serverId);
                return;
            }

            ResetTimer(serverId);
            await SendMessage($"It is now <@{await _queue.GetCurrentPlayer(serverId)}> turn.\n",
                serverId);
        }

        public void DeleteTimer(ulong server)
        {
            if (PlayTimers.ContainsKey(server))
            {
                if (PlayTimers[server] == null)
                    _logger.Log(LogSeverity.Warning, "Attempted to dispose a timer that was already disposed!");
                else
                    PlayTimers[server].Dispose();
            }

            PlayTimers.Remove(server);
        }
        
        private async Task SendMessage(string text, ulong server)
        {
            var channel = _client.GetGuild(server).DefaultChannel.Id;
            _logger.Log(LogSeverity.Info, $"Channel: {channel}");
            if (await _db.HasDefaultChannel(server))
                channel = await _db.GetDefaultChannel(server);
            _logger.Log(LogSeverity.Info, $"Channel: {channel}");

            try
            {
                await _client.GetGuild(server).GetTextChannel(channel).SendMessageAsync(text);
            }
            catch (Exception)
            {
                try
                {
                    await _client.GetGuild(server).GetTextChannel(_client.GetGuild(server).DefaultChannel.Id)
                        .SendMessageAsync(text);
                }
                catch (Exception)
                {
                    _logger.Log(LogSeverity.Error,
                        "Ok what the heck is this? Can't post in the default OR secondary channel?");
                }
            }
        }

        private async Task SendDM(string text, ulong user)
        {
            await _client.GetUser(user).SendMessageAsync(text);
        }
    }
}