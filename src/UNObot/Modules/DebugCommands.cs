using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UNObot.Services;

namespace UNObot.Modules
{
    public class DebugCommands : ModuleBase<SocketCommandContext>
    {
        [Command("purge", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.Administrator), RequireBotPermission(ChannelPermission.ManageMessages)]
        [DisableDMs]
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

        [Command("getplayerdata", RunMode = RunMode.Async)]
        public async Task RCONLongPacketTest(string User)
        {
            QueryHandlerService.SendRCON(QueryHandlerService.PSurvival, 27286, $"data get entity {User}", "mukyumukyu", out var Data);
            if (Data.Data.Equals("No entity was found", StringComparison.CurrentCultureIgnoreCase))
            {
                await ReplyAsync("Mukyu... Cannot find user...");
                return;
            }

            if (Data.Status != MinecraftRCON.RCONStatus.SUCCESS)
            {
                await ReplyAsync(Data.Status.ToString());
                return;
            }
            
            try
            {
                // Ignore the (PLAYERNAME) has the following data: 
                var JSONString = Data.Data.Substring(Data.Data.IndexOf('{'));
                // For UUIDs, they start an array with I; to indicate all values are integers; ignore it.
                JSONString = JSONString.Replace("I;", "");
                // Regex to completely ignore the b, s, l, f, and d patterns. Probably the worst RegEx I ever wrote.
                JSONString = Regex.Replace(JSONString, @"([:|\[|\,]\s*\-?\d*.?\d+)[s|b|l|f|d]", "${1}");
                var JSON = JObject.Parse(JSONString);
                var Dimension = JSON["Dimension"];
                var Position = JSON["Pos"];
                if (Position != null)
                    await ReplyAsync($"Position: {Position[0]}, {Position[1]}, {Position[2]} In ${Dimension}");
                else
                    await ReplyAsync("Failed to process coordinates.");
            }
            catch (JsonReaderException ex)
            {
                LoggerService.Log(LogSeverity.Error, "Failed to process JSON!", ex);
                
                await ReplyAsync("JSON_FAIL");
            }
        }

        [DllImport(@"libRCON.dll")]
        public static extern void mukyu();

        [Command("cppinterop", RunMode = RunMode.Async)]
        public async Task CPPInteropTest()
        {
            mukyu();
            await ReplyAsync("Executed.").ConfigureAwait(false);
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