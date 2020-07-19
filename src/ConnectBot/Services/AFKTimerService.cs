using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using UNObot;
using UNObot.Plugins.Helpers;
using Timer = System.Timers.Timer;
using Game = ConnectBot.Templates.Game;

namespace ConnectBot.Services
{
    public class ServerTimer : IDisposable
    {
        public Timer AFKTrigger { get; set; }
        public SocketCommandContext Context { get; set; }

        public void Dispose()
        {
            AFKTrigger?.Dispose();
        }
    }
    
    public class AFKTimerService
    {
        private static readonly List<ServerTimer> PlayTimers = new List<ServerTimer>();
        private readonly LoggerService _logger;
        private readonly DatabaseService _db;
        private readonly DiscordSocketClient _client;
        internal delegate Task NextGame(SocketCommandContext context, Game game, bool newGame = false);

        private NextGame _callback;

        public AFKTimerService(LoggerService logger, DatabaseService db, DiscordSocketClient client)
        {
            _logger = logger;
            _db = db;
            _client = client;
        }

        public void ResetTimer([NotNull] SocketCommandContext context)
        {
            var timer = PlayTimers.Find(o => o.Context.Guild.Id == context.Guild.Id);
            if (timer == null)
            {
                _logger.Log(LogSeverity.Error, "Attempted to reset timer that doesn't exist!");
            }
            else
            {
                timer.AFKTrigger.Stop();
                timer.AFKTrigger.Start();
                timer.Context = context;
            }
        }

        internal void StartTimer(SocketCommandContext context, NextGame callback)
        {
            _callback ??= callback;
            _logger.Log(LogSeverity.Debug, "Starting timer!");
            var timer = PlayTimers.Find(o => o.Context.Guild.Id == context.Guild.Id);
            if (timer != null)
            {
                _logger.Log(LogSeverity.Warning, "Attempted to start timer that already existed!");
                ResetTimer(context);
            }
            else
            {
                var newTimer = new ServerTimer
                {
                    AFKTrigger = new Timer
                    {
                        Interval = 90000,
                        AutoReset = false
                    },
                    Context = context
                };
                newTimer.AFKTrigger.Elapsed += TimerOver;
                newTimer.AFKTrigger.Start();
                PlayTimers.Add(newTimer);
            }
        }

        private async void TimerOver(object source, ElapsedEventArgs e)
        {
            _logger.Log(LogSeverity.Debug, "Timer over!");

            var timer = PlayTimers.Find(o => o.AFKTrigger == source);

            if (timer == null)
            {
                // Me when I'm looking back at my code after years
                // https://cdn.discordapp.com/attachments/466827186901614592/731673102559215636/CC2IYg5.png
                _logger.Log(LogSeverity.Error, "Couldn't figure out what server timer belonged to!");
                return;
            }

            var serverId = timer.Context.Guild.Id;
            var game = await _db.GetGame(serverId);
            if (game == null || !game.Queue.GameStarted())
            {
                DeleteTimer(timer);
                return;
            }

            var queue = game.Queue;
            var afkPlayer = queue.CurrentPlayer().Player;
            queue.RemovePlayer(afkPlayer);
            await SendDM("You have been AFK removed.", afkPlayer);

            if (queue.InGame.Count <= 1)
            {
                await SendMessage($"<@{afkPlayer}>, you have been AFK removed. The game will be reset.\n", game);
                await _callback(timer.Context, game);
            }
            else
            {
                var nextPlayer = queue.Next();
                await SendMessage($"<@{afkPlayer}>, you have been AFK removed. It is now <@{nextPlayer.Player}>'s turn.", game);
            }

            await _db.UpdateGame(game);
        }
        
        private void DeleteTimer(ServerTimer timer)
        {
            timer.Dispose();
            if (PlayTimers.Contains(timer))
            {
                PlayTimers.Remove(timer);
            }
        }
        
        private async Task SendMessage(string text, Game game)
        {
            if (game.LastChannel != null)
            {
                await _client.GetGuild(game.Server).GetTextChannel(game.LastChannel.Value).SendMessageAsync(text);
                return;
            }
            
            var channel = _client.GetGuild(game.Server).DefaultChannel.Id;
            _logger.Log(LogSeverity.Info, $"Channel: {channel}");
            if (await DatabaseExtensions.HasDefaultChannel(_db.ConnString, game.Server))
                channel = await DatabaseExtensions.GetDefaultChannel(_db.ConnString, game.Server);
            _logger.Log(LogSeverity.Info, $"Channel: {channel}");

            try
            {
                await _client.GetGuild(game.Server).GetTextChannel(channel).SendMessageAsync(text);
            }
            catch (Exception)
            {
                try
                {
                    await _client.GetGuild(game.Server).GetTextChannel(_client.GetGuild(game.Server).DefaultChannel.Id)
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