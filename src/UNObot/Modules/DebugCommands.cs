using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Plugins;
using UNObot.Plugins.Attributes;
using UNObot.Plugins.Helpers;
using UNObot.Services;

namespace UNObot.Modules;

public class DebugCommands : UNObotModule<UNObotCommandContext>
{
    private readonly ILogger _logger;
    private readonly ShellService _shell;
        
    public DebugCommands(ILogger logger, ShellService shell)
    {
        _logger = logger;
        _shell = shell;
    }
        
    [Command("purge", RunMode = RunMode.Async)]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(ChannelPermission.ManageMessages)]
    [DisableDMs]
    [Help(new[] {".purge (number of messages)"},
        "Delete messages via a range. Testing command; do not rely on forever.", false, "UNObot 1.4")]
    public async Task Purge(int length)
    {
        var messages = (await Context.Channel.GetMessagesAsync(length + 1).FlattenAsync()).ToList();
        var thing = messages.Where(o => DateTimeOffset.Now - o.Timestamp < TimeSpan.FromDays(14)).ToList();
        messages.RemoveAll(o => thing.Contains(o));

        if (!(Context.Channel is ITextChannel textChannel))
        {
            _logger.Log(LogSeverity.Warning, "Weird casting error?");
            return;
        }

        await textChannel.DeleteMessagesAsync(thing);
        if (messages.Count > 0)
        {
            PluginHelper.GhostMessage(Context, "WARNING: Because some messages are older than two weeks, UNObot might take longer to delete them.").ContinueWithoutAwait(_logger);
            foreach (var message in messages)
                await message.DeleteAsync();
        }
    }

    [Command("helpmeplz", RunMode = RunMode.Async)]
    [RequireOwner]
    [DisableDMs]
    public async Task HelpmePlz(int length)
        => await Purge(length);

    [Command("exit", RunMode = RunMode.Async)]
    public async Task Exit()
    {
        if (Context.User.Id == 191397590946807809)
        {
            await ReplyAsync("Shutting down!");
            Program.Exit();
        }
    }

#if DEBUG
    [Command("debugstatus", RunMode = RunMode.Async)]
    public async Task DebugStatus()
    {
        await _shell.GitFetch().ConfigureAwait(false);
        await ReplyAsync(await _shell.GitStatus().ConfigureAwait(false));
    }
#endif
}