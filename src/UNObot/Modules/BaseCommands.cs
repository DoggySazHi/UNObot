using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using UNObot.Plugins.Attributes;
using UNObot.Plugins.TerminalCore;
using UNObot.Services;

namespace UNObot.Modules
{
    public class BaseCommands : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _config;
        private readonly CommandHandlingService _commands;
        
        internal BaseCommands(IConfiguration config, CommandHandlingService commands)
        {
            _config = config;
            _commands = commands;
        }
        
        [Command("info", RunMode = RunMode.Async), Alias("version")]
        [Help(new[] {".info"}, "Get the current version of UNObot.", true, "UNObot 1.0")]
        internal async Task Info()
        {
            var output =
                $"{Context.Client.CurrentUser.Username} - Created by DoggySazHi\nVersion {_config["version"]}\nCurrent Time (PST): {DateTime.Now.ToString(CultureInfo.InvariantCulture)}" +
                $"\n\nCommit {_config["commit"]}\nBuild #{_config["build"]}";
            await ReplyAsync(output);
        }
        
        [Command("fullhelp", RunMode = RunMode.Async)]
        [Help(new[] {".fullhelp"}, "If you need help using help, you're truly lost.", true, "UNObot 1.0")]
        internal async Task FullHelp()
        {
            if (!Context.IsPrivate)
                await ReplyAsync("Help has been sent. Or, I think it has.");
            var response = "```Commands: @UNOBot#4308 command/ .command\n (Required) {May be required} [Optional]\n \n";
            foreach (var cmd in _commands.Commands)
            {
                var oldResponse = response;
                if (cmd.Active)
                {
                    oldResponse = response;
                    response += $"- {cmd.CommandName}: {cmd.Help}\n";
                    if (cmd.Usages.Count > 0)
                        response += $"Usage(s): {string.Join(", ", cmd.Usages.ToArray())}\n";
                    if (cmd.Aliases.Count > 0)
                        response += $"Aliases: {string.Join(", ", cmd.Aliases.ToArray())}\n";
                    if (response.Length > 1996)
                    {
                        oldResponse += "```";
                        await Context.Message.Author.SendMessageAsync(oldResponse);
                        response = "```";
                        response += $"- {cmd.CommandName}: {cmd.Help}\n";
                        if (cmd.Usages.Count > 0)
                            response += $"Usage(s): {string.Join(", ", cmd.Usages.ToArray())}\n";
                        if (cmd.Aliases.Count > 0)
                            response += $"Aliases: {string.Join(", ", cmd.Aliases.ToArray())}\n";
                    }

                    response += "\n";
                }

                if (response.Length > 1996)
                {
                    response = oldResponse;
                    response += "```";
                    await Context.Message.Author.SendMessageAsync(response);
                    response = "```\n";
                }
            }

            response += "```";
            await Context.Message.Author.SendMessageAsync(response);
        }

        [Command("help", RunMode = RunMode.Async)]
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
                .AddField("Usages", "@UNOBot#4308 *commandtorun*\n.*commandtorun*")
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

        [Command("playerhelp", RunMode = RunMode.Async)]
        [Alias("playercommand", "playercommands", "playercmd", "playercmds")]
        internal async Task PlayerHelp()
        {
            var r = ThreadSafeRandom.ThisThreadsRandom;
            var builder = new EmbedBuilder()
                .WithTitle("Quick-start guide to UNObot-MusicBot")
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
                .AddField("Usages", "@UNOBot#4308 *commandtorun*\n.*commandtorun*")
                .AddField(".playerplay (Link)", "Add a song to the queue, or continue if the player is paused.", true)
                .AddField(".playerpause", "Pause the player. Duh.", true)
                .AddField(".playershuffle", "Shuffle the contents of the queue.", true)
                .AddField(".playerskip", "Skip the current song playing to the next one in the queue.", true)
                .AddField(".playerqueue", "Display the contents of the queue.", true)
                .AddField(".playernp", "Find out what song is playing currently.", true)
                .AddField(".playerloop", "Loop the current song playing.", true)
                .AddField(".playerloopqueue", "Loop the contents of the queue.", true);
            var embed = builder.Build();
            await Context.Channel.SendMessageAsync(
                "",
                embed: embed);
        }

        [Command("help", RunMode = RunMode.Async)]
        [Alias("ahh", "ahhh", "ahhhh")]
        [Help(new[] {".help (command)"}, "If you need help using help, you're truly lost.", true, "UNObot 1.0")]
        internal async Task Help(string cmdSearch)
        {
            var response = "";
            var cmd = _commands.FindCommand(cmdSearch);
            if(cmd == null)
            {
                await ReplyAsync("Command was not found in the help list.");
                return;
            }

            response += "```";
            if (!cmd.Active)
                response += "Note: This command might be hidden or deprecated.\n";
            response += $"- {cmd.CommandName}: {cmd.Help}\n";
            if (cmd.Usages.Count > 0)
                response += $"Usage(s): {string.Join(", ", cmd.Usages.ToArray())}\n";
            response += $"Introduced in {cmd.Version}.\n";
            if (cmd.Aliases.Count > 0)
                response += $"Aliases: {string.Join(", ", cmd.Aliases.ToArray())}\n";
            response += "```";
            await ReplyAsync(response);
        }
    }
}