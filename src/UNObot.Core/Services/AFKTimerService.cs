using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;
using Timer = System.Timers.Timer;

namespace UNObot.Core.Services;

public class AFKTimerService
{
    private static readonly Dictionary<ulong, Timer> PlayTimers = new();
    private readonly IUNObotConfig _config;
    private readonly ILogger _logger;
    private readonly DatabaseService _db;
    private readonly QueueHandlerService _queue;
    private readonly DiscordSocketClient _client;

    public AFKTimerService(IUNObotConfig config, ILogger logger, DatabaseService db, QueueHandlerService queue, DiscordSocketClient client)
    {
        _config = config;
        _logger = logger;
        _db = db;
        _queue = queue;
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

    public void StartTimer(ulong server)
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
            // Me when I'm looking back at my code after years
            // https://cdn.discordapp.com/attachments/466827186901614592/731673102559215636/CC2IYg5.png
            _logger.Log(LogSeverity.Error, "Couldn't figure out what server timer belonged to!");
            return;
        }

        if (!await _db.IsServerInGame(serverId))
        {
            DeleteTimer(serverId);
            return;
        }
        var currentPlayer = await _queue.GetCurrentPlayer(serverId);
        await _db.RemoveUser(currentPlayer);
        await _queue.DropFrontPlayer(serverId);
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
        if (await DatabaseExtensions.HasDefaultChannel(_config, server))
            channel = await DatabaseExtensions.GetDefaultChannel(_config, server);
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