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
using UNObot.Services;
using static UNObot.Services.IRCON;

namespace UNObot.Modules
{
    public class DebugCommands : ModuleBase<SocketCommandContext>
    {
        [Command("purge", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [DisableDMs]
        [Help(new[] {".purge (number of messages)"},
            "Delete messages via a range. Testing command; do not rely on forever.", false, "UNObot 1.4")]
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

        [Command("helpmeplz", RunMode = RunMode.Async)]
        [RequireOwner]
        [DisableDMs]
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
            {
                await ReplyAsync("Error: <:patchythink:592817853313581067>");
            }
            else if (Context.User.Id == 191397590946807809)
            {
                await ReplyAsync("Resetting!");
                Program.Exit();
            }
        }

#if DEBUG
        [Command("translate", RunMode = RunMode.Async)]
        public async Task Translate(string @from, string to, [Remainder] string message)
        {
            await ReplyAsync(GoogleTranslateService.GetSingleton().Translate(message, @from, to)).ConfigureAwait(false);
        }

        [Command("debugstatus", RunMode = RunMode.Async)]
        public async Task DebugStatus()
        {
            await ShellService.GitFetch().ConfigureAwait(false);
            await ReplyAsync(await ShellService.GitStatus().ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Command("interop", RunMode = RunMode.Async)]
        public async Task Interop()
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
        public async Task RCONLongPacketTest(string user)
        {
            var server = QueryHandlerService.SpecialServers[27285];
            QueryHandlerService.SendRCON(server.Server, server.RCONPort, $"data get entity {user}", "mukyumukyu",
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
                LoggerService.Log(LogSeverity.Error, "Failed to process JSON!", ex);

                await ReplyAsync("JSON_FAIL");
            }
        }
#endif

        /*
        [Command("getbuttons", RunMode = RunMode.Async)]
        [HelpAttribute(new string[] { "yes." }, "", false, "no")]
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