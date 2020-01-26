using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using UNObot.Services;

namespace UNObot.Modules
{
    public class DebugCommands : ModuleBase<SocketCommandContext>
    {
        [Command("purge", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.Administrator), RequireBotPermission(ChannelPermission.ManageMessages)]
        [Help(new[] { ".purge (number of messages)" }, "Delete messages via a range. Testing command; do not rely on forever.", false, "UNObot 1.4")]
        public async Task Purge(int length)
        {
            var messages = await Context.Channel.GetMessagesAsync(length + 1).FlattenAsync();

            if (!(Context.Channel is ITextChannel textchannel))
            {
                LoggerService.Log(LogSeverity.Warning, "error cast");
                return;
            }
            await textchannel.DeleteMessagesAsync(messages);
        }

        [Command("helpmeplz", RunMode = RunMode.Async), RequireOwner]
        public async Task HelpmePlz(int length)
        {
            var messages = await Context.Channel.GetMessagesAsync(length + 1).FlattenAsync();

            if (!(Context.Channel is ITextChannel textchannel))
            {
                LoggerService.Log(LogSeverity.Warning, "error cast");
                return;
            }
            await textchannel.DeleteMessagesAsync(messages);
        }

        [Command("exit", RunMode = RunMode.Async)]
        public async Task Exit()
        {
            if (Context.User.Id == 278524552462598145)
                await ReplyAsync("Error: <:patchythink:592817853313581067>");
            else if (Context.User.Id == 191397590946807809)
            {
                await ReplyAsync("Resetting!");
                Program.Exit();
            }
        }

#if DEBUG
        [Command("translate", RunMode = RunMode.Async)]
        public async Task Translate(string From, string To, [Remainder] string Message)
        {
            await ReplyAsync(GoogleTranslateService.GetSingleton().Translate(Message, From, To)).ConfigureAwait(false);
        }

        [Command("debugstatus", RunMode = RunMode.Async)]
        public async Task DebugStatus()
        {
            await ShellService.GitFetch().ConfigureAwait(false);
            await ReplyAsync(await ShellService.GitStatus().ConfigureAwait(false)).ConfigureAwait(false);
        }
#endif

        /*
        [Command("getbuttons", RunMode = RunMode.Async)]
        [Help(new string[] { "yes." }, "", false, "no")]
        public async Task AddButtons()
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
        public async Task StartSpam()
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