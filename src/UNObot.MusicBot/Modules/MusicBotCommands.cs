﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Plugins.Attributes;
using UNObot.MusicBot.Services;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;

namespace UNObot.MusicBot.Modules;

public class MusicBotCommands : UNObotModule<UNObotCommandContext>
{
    private readonly MusicBotService _music;
    private readonly IUNObotConfig _config;

    public MusicBotCommands(MusicBotService music, IUNObotConfig config)
    {
        _music = music;
        _config = config;
    }
        
    [Command("playerhelp", RunMode = RunMode.Async), Priority(100)]
    [Alias("playercommand", "playercommands", "playercmd", "playercmds")]
    public async Task PlayerHelp()
    {
        var builder = new EmbedBuilder()
            .WithTitle("Quick-start guide to UNObot-MusicBot")
            .WithColor(PluginHelper.RandomColor())
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter(footer =>
            {
                footer
                    .WithText($"UNObot {_config.Version} - By DoggySazHi")
                    .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
            })
            .WithAuthor(author =>
            {
                var guildName = $"{Context.User.Username}'s DMs";
                if (!Context.IsPrivate)
                    guildName = Context.Guild.Name;
                author
                    .WithName($"Playing in {guildName}")
                    .WithIconUrl("https://williamle.com/unobot/unobot.png");
            })
            .AddField("Usages", $"@{Context.Client.CurrentUser.Username}#{Context.Client.CurrentUser.Discriminator} *commandtorun*\n.*commandtorun*")
            .AddField(".playerplay (Link)", "Add a song to the queue, or continue if the player is paused.", true)
            .AddField(".playerpause", "Pause the player. Duh.", true)
            .AddField(".playershuffle", "Shuffle the contents of the queue.", true)
            .AddField(".playerskip", "Skip the current song playing to the next one in the queue.", true)
            .AddField(".playerqueue", "Display the contents of the queue.", true)
            .AddField(".playernp", "Find out what song is playing currently.", true)
            .AddField(".playerloop", "Loop the current song playing.", true)
            .AddField(".playerloopqueue", "Loop the contents of the queue.", true);
        var embed = builder.Build();
        await Context.ReplyAsync(
            "",
            embed: embed);
    }
        
    [Command("playerplay", RunMode = RunMode.Async)]
    [Alias("playmusic", "pm")]
    [DisableDMs]
    [Help(new[] {".playerplay", ".playerplay (link)", ".playerplay (search)"},
        "Wait, MusicBot functionality in UNObot?", true, "UNObot 3.2 Beta 1")]
    public async Task PlayMusic([Remainder] string link)
    {
        var audioChannel = (Context.User as IGuildUser)?.VoiceChannel;
        if (audioChannel == null)
        {
            await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
            return;
        }

        var loading = await ReplyAsync("Loading music... (this may take a while, but not more than 10 seconds)");

        var result = await _music.AddList(Context.User.Id, Context.Guild.Id, link, audioChannel, Context.Channel);
        if (result.Item1 == null)
            result = await _music.Add(Context.User.Id, Context.Guild.Id, link, audioChannel, Context.Channel);
        if (result.Item1 == null)
            result = await _music.Search(Context.User.Id, Context.Guild.Id, link, audioChannel, Context.Channel);
        if (result.Item1 == null)
        {
            await loading.ModifyAsync(o => o.Content = $"Error: {result.Item2 ?? "I... don't know."}");
            return;
        }

        var message = result.Item2 ?? "";
        if (result.Item1.Url.Contains("ixMHG0DIAK4") && Context.User.Id == 278524552462598145)
            message += "Aw crap, here we go again...";

        await loading.ModifyAsync(o =>
        {
            o.Content = message;
            o.Embed = result.Item1;
        });
    }

    [Command("playerplay", RunMode = RunMode.Async)]
    [Alias("playmusic", "pm")]
    [DisableDMs]
    public async Task Play()
    {
        var audioChannel = (Context.User as IGuildUser)?.VoiceChannel;
        if (audioChannel == null)
        {
            await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
            return;
        }

        var result = await _music.Play(Context.User.Id, Context.Guild.Id, audioChannel);
        await ReplyAsync(result);
    }

    [Command("playerplaytop", RunMode = RunMode.Async)]
    [Alias("playmusictop", "pmt")]
    [DisableDMs]
    [Help(new[] {".playerplay", ".playerplay (link)", ".playerplay (search)"},
        "Wait, MusicBot functionality in UNObot?", true, "UNObot 3.2 Beta 1")]
    public async Task PlayMusicTop([Remainder] string link)
    {
        var audioChannel = (Context.User as IGuildUser)?.VoiceChannel;
        if (audioChannel == null)
        {
            await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
            return;
        }

        var loading = await ReplyAsync("Loading music... (this may take a while, but not more than 10 seconds)");

        var result = await _music.AddList(Context.User.Id, Context.Guild.Id, link,
            audioChannel, Context.Channel, true);
        if (result.Item1 == null)
            result = await _music.Add(Context.User.Id, Context.Guild.Id, link, audioChannel,
                Context.Channel, true);
        if (result.Item1 == null)
            result = await _music.Search(Context.User.Id, Context.Guild.Id, link,
                audioChannel, Context.Channel, true);
        if (result.Item1 == null)
        {
            await loading.ModifyAsync(o => o.Content = $"Error: {result.Item2 ?? "I... don't know."}");
            return;
        }

        var message = result.Item2 ?? "";
        if (result.Item1.Url.Contains("ixMHG0DIAK4") && Context.User.Id == 278524552462598145)
            message += "Aw crap, here we go again...";

        await loading.ModifyAsync(o =>
        {
            o.Content = message;
            o.Embed = result.Item1;
        });
    }

    [Command("playerpause", RunMode = RunMode.Async)]
    [Alias("pause", "pauseplayer")]
    [DisableDMs]
    [Help(new[] {".playerpause"}, "Pause the player.", true, "UNObot 3.2 Beta 2")]
    public async Task Pause()
    {
        var audioChannel = (Context.User as IGuildUser)?.VoiceChannel;
        if (audioChannel == null)
        {
            await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
            return;
        }

        var result = await _music.Pause(Context.User.Id, Context.Guild.Id, audioChannel);
        await ReplyAsync(result);
    }

    [Command("playershuffle", RunMode = RunMode.Async)]
    [Alias("shuffle")]
    [DisableDMs]
    [Help(new[] {".playershuffle"}, "Shuffle the player.", true, "UNObot 3.2 Beta 3")]
    public async Task Shuffle()
    {
        var audioChannel = (Context.User as IGuildUser)?.VoiceChannel;
        if (audioChannel == null)
        {
            await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
            return;
        }

        var result = await _music.Shuffle(Context.User.Id, Context.Guild.Id, audioChannel);
        await ReplyAsync(result);
    }

    [Command("playerskip", RunMode = RunMode.Async)]
    [DisableDMs]
    [Help(new[] {".playerskip"}, "Skip the current song.", true, "UNObot 3.2 Beta 3")]
    public async Task Skip()
    {
        var audioChannel = (Context.User as IGuildUser)?.VoiceChannel;
        if (audioChannel == null)
        {
            await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
            return;
        }

        var result = await _music.Skip(Context.User.Id, Context.Guild.Id, audioChannel);
        if (!string.IsNullOrWhiteSpace(result))
            await ReplyAsync(result);
    }

    [Command("playerloop", RunMode = RunMode.Async)]
    [DisableDMs]
    [Help(new[] {".playerloop"}, "Loop the current song.", true, "UNObot 3.2 Beta 4")]
    public async Task Loop()
    {
        var audioChannel = (Context.User as IGuildUser)?.VoiceChannel;
        if (audioChannel == null)
        {
            await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
            return;
        }

        var result = await _music
            .ToggleLoop(Context.User.Id, Context.Guild.Id, audioChannel);
        await ReplyAsync(result);
    }

    [Command("playerloopqueue", RunMode = RunMode.Async)]
    [DisableDMs]
    [Help(new[] {".playerloopqueue"}, "Loop the entire queue.", true, "UNObot 3.2 Beta 4")]
    public async Task LoopQueue()
    {
        var audioChannel = (Context.User as IGuildUser)?.VoiceChannel;
        if (audioChannel == null)
        {
            await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
            return;
        }

        var result = await _music
            .ToggleLoopQueue(Context.User.Id, Context.Guild.Id, audioChannel);
        await ReplyAsync(result);
    }

    [Command("playerdc", RunMode = RunMode.Async)]
    [Alias("dc", "playerdisconnect")]
    [DisableDMs]
    [Help(new[] {".playerdc"}, "Disconnect the bot from the channel.", true, "UNObot 3.2 Beta 4")]
    public async Task Disconnect()
    {
        var audioChannel = (Context.User as IGuildUser)?.VoiceChannel;
        if (audioChannel == null)
        {
            await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(false);
            return;
        }

        var result = await _music
            .Disconnect(Context.User.Id, Context.Guild.Id, audioChannel).ConfigureAwait(true);
        await ReplyAsync(result).ConfigureAwait(false);
    }

    [Command("playernp", RunMode = RunMode.Async)]
    [Alias("playernowplaying", "np")]
    [DisableDMs]
    [Help(new[] {".playernp"}, "Get the song playing.", true, "UNObot 3.2 Beta 2")]
    public async Task NowPlaying()
    {
        var result = _music.GetNowPlaying(Context.Guild.Id);
        if (!string.IsNullOrWhiteSpace(result.Item2))
            await ReplyAsync($"Error: {result.Item2}");
        else
            await ReplyAsync("", false, result.Item1);
    }

    [Command("playerqueue", RunMode = RunMode.Async)]
    [Alias("pq")]
    [DisableDMs]
    [Help(new[] {".playerqueue", ".playerqueue (page)"}, "Get the songs in the player's queue.", true,
        "UNObot 3.2 Beta 2")]
    public async Task Queue()
    {
        var result = _music.GetMusicQueue(Context.Guild.Id, 1);
        if (!string.IsNullOrWhiteSpace(result.Item2))
            await ReplyAsync($"Error: {result.Item2}").ConfigureAwait(false);
        else
            await ReplyAsync("", false, result.Item1).ConfigureAwait(false);
    }

    [Command("playerqueue", RunMode = RunMode.Async)]
    [Alias("pq")]
    [DisableDMs]
    public async Task Queue(int page)
    {
        var result = _music.GetMusicQueue(Context.Guild.Id, page);
        if (!string.IsNullOrWhiteSpace(result.Item2))
            await ReplyAsync($"Error: {result.Item2}").ConfigureAwait(false);
        else
            await ReplyAsync("", false, result.Item1).ConfigureAwait(false);
    }

    [Command("playerremove", RunMode = RunMode.Async)]
    [Alias("prm, rm")]
    [DisableDMs]
    [Help(new[] {".playerremove"}, "Remove the song.", true, "UNObot 3.2 Beta 3")]
    public async Task Remove(int index)
    {
        var audioChannel = (Context.User as IGuildUser)?.VoiceChannel;
        if (audioChannel == null)
        {
            await ReplyAsync("Please join a VC that I can connect to!").ConfigureAwait(true);
            return;
        }

        var result = await _music
            .Remove(Context.User.Id, Context.Guild.Id, audioChannel, index);
        await ReplyAsync(result);
    }
}