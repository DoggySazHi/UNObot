using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Plugins;
using UNObot.Plugins.Attributes;
using UNObot.Services;

namespace UNObot.Modules
{
    public class DebugCommands : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger _logger;
        private readonly GoogleTranslateService _gts;
        private readonly ShellService _shell;
        
        internal DebugCommands(ILogger logger, GoogleTranslateService gts, ShellService shell)
        {
            _logger = logger;
            _gts = gts;
            _shell = shell;
        }
        
        [Command("purge", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [DisableDMs]
        [Help(new[] {".purge (number of messages)"},
            "Delete messages via a range. Testing command; do not rely on forever.", false, "UNObot 1.4")]
        internal async Task Purge(int length)
        {
            var messages = await Context.Channel.GetMessagesAsync(length + 1).FlattenAsync();

            if (!(Context.Channel is ITextChannel textchannel))
            {
                _logger.Log(LogSeverity.Warning, "error cast");
                return;
            }

            await textchannel.DeleteMessagesAsync(messages);
        }

        [Command("helpmeplz", RunMode = RunMode.Async)]
        [RequireOwner]
        [DisableDMs]
        internal async Task HelpmePlz(int length)
        {
            var messages = await Context.Channel.GetMessagesAsync(length + 1).FlattenAsync();

            if (!(Context.Channel is ITextChannel textchannel))
            {
                _logger.Log(LogSeverity.Warning, "error cast");
                return;
            }

            await textchannel.DeleteMessagesAsync(messages);
        }

        [Command("exit", RunMode = RunMode.Async)]
        internal async Task Exit()
        {
            if (Context.User.Id == 278524552462598145)
            {
                await ReplyAsync("Error: <:patchythink:592817853313581067>");
            }
            else if (Context.User.Id == 191397590946807809)
            {
                await ReplyAsync("Shutting down!");
                Program.Exit();
            }
        }

#if DEBUG
        [Command("translate", RunMode = RunMode.Async)]
        internal async Task Translate(string from, string to, [Remainder] string message)
        {
            await ReplyAsync(_gts.Translate(message, from, to)).ConfigureAwait(false);
        }

        [Command("debugstatus", RunMode = RunMode.Async)]
        internal async Task DebugStatus()
        {
            await _shell.GitFetch().ConfigureAwait(false);
            await ReplyAsync(await _shell.GitStatus().ConfigureAwait(false));
        }
        
#endif

        /*
        [Command("getbuttons", RunMode = RunMode.Async)]
        [HelpAttribute(new string[] { "yes." }, "", false, "no")]
        internal async Task AddButtons()
        {
            var message = await ReplyAsync("Loading buttons...");
            await InputHandler.AddReactions(message);
            await message.ModifyAsync(o => o.Content = "Finished loading buttons!");
        }
        */

        /*
        Timer spamTimer = new Timer();
        ulong server = 0;

        [Command("enablespam")]
        internal async Task StartSpam()
        {
            spamTimer = new Timer
            {
                Interval = 60000,
                AutoReset = true,
            };
            spamTimer.Elapsed += Spam;
            spamTimer.Start();
            server = Context.Guild.Id;
            await ReplyAsync("Started timer!");
        }

        async void Spam(object sender, ElapsedEventArgs e)
        {
            if (server == 0)
                spamTimer.Dispose();
            else
                await UNObot.Program.SendMessage("AHHHHHHH", server);
        }
        */
    }
}