using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using UNObot;
using UNObot.Plugins.Helpers;
using Timer = System.Timers.Timer;

namespace ConnectBot.Services
{
    public class AFKTimerService
    {
        private static readonly Dictionary<ulong, Timer> PlayTimers = new Dictionary<ulong, Timer>();
        private readonly LoggerService _logger;
        private readonly DatabaseService _db;
        private readonly DiscordSocketClient _client;

        public AFKTimerService(LoggerService logger, DatabaseService db, DiscordSocketClient client)
        {
            _logger = logger;
            _db = db;
            _client = client;
        }

        public void ResetTimer(ulong server)
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

        internal void StartTimer(ulong server)
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
            PlayTimers.Keys.Fin
            foreach (var server in PlayTimers.Keys)
            {
                var timer = (Timer) source;
                if (timer.Equals(PlayTimers[server]))
                    serverId = server;
            }

            if (serverId == 0)
            {
                // Me when I'm looking back at my code after years
                // https://cdn.discordapp.com/attachments/466827186901614592/731673102559215636/CC2IYg5.png
                _logger.Log(LogSeverity.Error, "Couldn't figure out what server timer belonged to!");
                return;
            }

            var game = await _db.GetGame(serverId);
            if (game == null || !game.Queue.GameStarted())
            {
                DeleteTimer(serverId);
                return;
            }

            var queue = game.Queue;
            var afkPlayer = queue.CurrentPlayer();
            await SendMessage($"<@{afkPlayer}>, you have been AFK removed.\n", serverId);
            await SendDM("You have been AFK removed.", afkPlayer.Player);
            
            if (!queue.Start())
            {
                await _db.ResetGame(serverId);
                await SendMessage("Game has been reset, due to a lack of players in the queue.", serverId);
                DeleteTimer(serverId);
                return;
            }

            await _db.UpdateGame(game);
            
            ResetTimer(serverId);
            await SendMessage($"The next batch of players! It is now <@{queue.CurrentPlayer()}>'s turn. Players ",
                serverId);
        }

        private void DeleteTimer(ulong server)
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
            if (await DatabaseExtensions.HasDefaultChannel(_db.ConnString, server))
                channel = await DatabaseExtensions.GetDefaultChannel(_db.ConnString, server);
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