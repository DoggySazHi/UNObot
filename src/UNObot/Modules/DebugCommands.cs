using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UNObot.Interop;
using UNObot.Plugins.Attributes;
using UNObot.Services;
using static UNObot.Services.IRCON;

namespace UNObot.Modules
{
    public class DebugCommands : ModuleBase<SocketCommandContext>
    {
        private readonly LoggerService _logger;
        private readonly GoogleTranslateService _gts;
        private readonly ShellService _shell;
        private readonly QueryHandlerService _query;
        
        internal DebugCommands(LoggerService logger, GoogleTranslateService gts, ShellService shell, QueryHandlerService query)
        {
            _logger = logger;
            _gts = gts;
            _shell = shell;
            _query = query;
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
                await ReplyAsync("Error: You deprecated this command. Nice job.");
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

        [Command("interop", RunMode = RunMode.Async)]
        internal async Task Interop()
        {
            RCONHelper.MukyuN();
            await ReplyAsync("Successfully interop local!");
            var ptr = RCONHelper.Say("Mukyu to me!");
            var text = Marshal.PtrToStringAnsi(ptr);
            await ReplyAsync($"Successfully interop string! {text}");
            RCONHelper.SayDelete(ptr);
            await ReplyAsync("Successfully deleted string!");
            var server = new IPEndPoint(IPAddress.Parse("192.168.2.6"), 27286);
            var rconFromCpp = new RCONHelper(server, "mukyumukyu", "list");
            await ReplyAsync(
                $"Response from {rconFromCpp.Server} {rconFromCpp.Status} {rconFromCpp.Connected()}: {rconFromCpp.Data}");
            rconFromCpp.Dispose();
        }

        [Command("getplayerdata", RunMode = RunMode.Async)]
        internal async Task RCONLongPacketTest(string user)
        {
            var server = _query.SpecialServers[27285];
            _query.SendRCON(server.Server, server.RCONPort, $"data get entity {user}", "mukyumukyu",
                out var data);
            if (data.Data.Equals("No entity was found", StringComparison.CurrentCultureIgnoreCase))
            {
                await ReplyAsync("Mukyu... Cannot find user...");
                return;
            }

            if (data.Status != RCONStatus.Success)
            {
                await ReplyAsync(data.Status.ToString());
                return;
            }

            try
            {
                // Ignore the (PLAYERNAME) has the following data: 
                var jsonString = data.Data.Substring(data.Data.IndexOf('{'));
                // For UUIDs, they start an array with I; to indicate all values are integers; ignore it.
                jsonString = jsonString.Replace("I;", "");
                // Regex to completely ignore the b, s, l, f, and d patterns. Probably the worst RegEx I ever wrote.
                jsonString = Regex.Replace(jsonString, @"([:|\[|\,]\s*\-?\d*.?\d+)[s|b|l|f|d]", "${1}");
                var json = JObject.Parse(jsonString);
                var dimension = json["Dimension"];
                var position = json["Pos"];
                if (position != null)
                    await ReplyAsync($"Position: {position[0]}, {position[1]}, {position[2]} In ${dimension}");
                else
                    await ReplyAsync("Failed to process coordinates.");
            }
            catch (JsonReaderException ex)
            {
                _logger.Log(LogSeverity.Error, "Failed to process JSON!", ex);

                await ReplyAsync("JSON_FAIL");
            }
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