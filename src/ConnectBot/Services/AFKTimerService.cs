using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Timers;
using ConnectBot.Templates;
using Discord;
using Discord.WebSocket;
using UNObot.Plugins;
using Timer = System.Timers.Timer;
using Game = ConnectBot.Templates.Game;

namespace ConnectBot.Services
{
    public class ServerTimer : IDisposable
    {
        public Timer AFKTrigger { get; set; }
        public ICommandContextEx Context { get; set; }

        public void Dispose()
        {
            AFKTrigger?.Dispose();
        }
    }
    
    public class AFKTimerService
    {
        private static readonly List<ServerTimer> PlayTimers = new List<ServerTimer>();
        private readonly ILogger _logger;
        private readonly DatabaseService _db;
        private readonly DiscordSocketClient _client;
        public delegate Task NextGame(ICommandContextEx context, Game game, bool newGame = false);

        private NextGame _callback;

        public AFKTimerService(ILogger logger, DatabaseService db, DiscordSocketClient client)
        {
            _logger = logger;
            _db = db;
            _client = client;
        }

        public void ResetTimer([NotNull] ICommandContextEx context)
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

        public void StartTimer(ICommandContextEx context, NextGame callback)
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
                await SendMessage($"<@{afkPlayer}>, you have been AFK removed. The game will be reset.\n", timer);
                await _callback(timer.Context, game);
            }
            else
            {
                var nextPlayer = queue.Next();
                await SendMessage($"<@{afkPlayer}>, you have been AFK removed. It is now <@{nextPlayer.Player}>'s turn.", timer);
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
        
        private async Task SendMessage(string text, ServerTimer timer)
        {
            await _client.GetGuild(timer.Context.Guild.Id).GetTextChannel(timer.Context.Channel.Id).SendMessageAsync(text);
        }

        private async Task SendDM(string text, ulong user)
        {
            await _client.GetUser(user).SendMessageAsync(text);
        }
    }
}