using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Plugins.Attributes;

namespace UNObot.Core.Modules
{
    public class CoreCommands : ModuleBase<SocketCommandContext>
    {
        private LoggerService _logger;
        public CoreCommands(LoggerService logger)
        {
            _logger = logger;
        }
        
        [Command("testperms", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        [DisableDMs]
        [Help(new[] {".testperms"}, "Show all permissions that UNObot has. Added for security reasons.", true,
            "UNObot 1.4")]
        public async Task TestPerms()
        {
            var response = "Permissions:\n";
            var user = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var perms = user.GetPermissions(Context.Channel as IGuildChannel);
            foreach (var c in perms.ToList()) response += $"- {c.ToString()} | \n";
            await ReplyAsync(response);
        }

        [Command("dogtestperms", RunMode = RunMode.Async)]
        [RequireOwner]
        [DisableDMs]
        [Help(new[] {".dogtestperms"}, "Show all permissions that UNObot has. Added for security reasons.", false,
            "UNObot 1.4")]
        public async Task TestPerms2()
        {
            var response = "Permissions:\n";
            var user = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var perms = user.GetPermissions(Context.Channel as IGuildChannel);
            foreach (var c in perms.ToList()) response += $"- {c.ToString()} | \n";
            await ReplyAsync(response);
        }
        
        [Command("welcome", RunMode = RunMode.Async)]
        public async Task Welcome()
        {
            var response = "Permissions:\n";
            var user = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var perms = user.GetPermissions(Context.Channel as IGuildChannel);
            foreach (var c in perms.ToList()) response += $"- {c.ToString()} | \n";
            _logger.Log(LogSeverity.Debug, response);
            await ReplyAsync("UNObot was already succcessfully initialized in this server. But thank you.");
        }

        [Command("nick", RunMode = RunMode.Async)]
        [RequireOwner]
        [DisableDMs]
        [Help(new[] {".nick (nickname)"}, "Change the nickname of UNObot.", false, "UNObot 2.0")]
        public async Task ChangeNick(string newnick)
        {
            var user = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            await user.ModifyAsync(x => { x.Nickname = newnick; });
        }

        [Command("credits", RunMode = RunMode.Async)]
        [Alias("asdf")]
        [Help(new[] {".credits"}, "Wow, look at all of the victims during the making of this bot.", true, "UNObot 1.0")]
        public async Task Credits()
        {
            await ReplyAsync("UNObot: Programmed by DoggySazHi\n" +
                             "Tested by dabadcuber5, Aragami and Fm (ish)\n" +
                             "UNO card images from Wikipedia\n" +
                             "Created for the UBOWS server\n\n" +
                             "Stickerz was here.\n" +
                             "Blame LocalDisk and Harvest for any bugs.");
        }

        [Command("invite", RunMode = RunMode.Async)]
        [Help(new[] {".invite"}, "You actually want the bot? Wow.", true, "UNObot 3.1.4")]
        public async Task Invite()
        {
            await ReplyAsync("If you want to add this bot to your server, use this link: \n" +
                             "https://discordapp.com/api/oauth2/authorize?client_id=477616287997231105&permissions=8192&scope=bot");
        }
    }
}