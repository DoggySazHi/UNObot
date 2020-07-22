using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using UNObot.Plugins;
using UNObot.Plugins.Attributes;
using UNObot.Plugins.TerminalCore;

namespace UNObot.Core.Modules
{
    public class CoreCommands : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        
        public CoreCommands(ILogger logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }
        
                [Command("help", RunMode = RunMode.Async), Priority(100)]
        [Alias("ahh", "ahhh", "ahhhh", "commands", "command")]
        internal async Task Help()
        {
            var r = ThreadSafeRandom.ThisThreadsRandom;
            var builder = new EmbedBuilder()
                .WithTitle("Quick-start guide to UNObot")
                .WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)))
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {_config["version"]} - By DoggySazHi")
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
                .AddField(".join", "Join a game in the current server.", true)
                .AddField(".start", "Start a game in the current server.\nYou must have joined beforehand.", true)
                .AddField(".play (color) (value) [new color]",
                    "Play a card, assuming if it's your turn.\nIf you are playing a Wild card, also\nadd the color to change to.",
                    true)
                .AddField(".hand", "See which cards you have, as well as\nwhich ones you can play.", true)
                .AddField(".game", "See everything about the current game.\nThis also shows the current card.", true)
                .AddField(".draw", "Draw a card. Duh. Can be used indefinitely.", true)
                .AddField(".quickplay", "Auto-magically draw and play the first valid card that comes out.", true)
                .AddField(".fullhelp", "See an extended listing of commands.\nNice!", true);
            var embed = builder.Build();
            await Context.Channel.SendMessageAsync(
                ":+1: got cha fam",
                embed: embed);
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