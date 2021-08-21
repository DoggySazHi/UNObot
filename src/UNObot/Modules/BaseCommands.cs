using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Plugins;
using UNObot.Plugins.Attributes;
using UNObot.Services;

namespace UNObot.Modules
{
    public class BaseCommands : UNObotModule<UNObotCommandContext>
    {
        private readonly IUNObotConfig _config;
        
        public BaseCommands(IUNObotConfig config)
        {
            _config = config;
        }
        
        [SlashCommand("info", RunMode = RunMode.Async), Alias("version")]
        [Help(new[] {".info"}, "Get the current version of UNObot.", true, "UNObot 1.0")]
        public async Task Info()
        {
            var output =
                $"{Context.Client.CurrentUser.Username} - Created by DoggySazHi\nVersion {_config.Version}\nCurrent Time (PST): {DateTime.Now.ToString(CultureInfo.InvariantCulture)}" +
                $"\n\nCommit {_config.Commit?[..Math.Min(_config.Commit.Length, 7)] ?? "???"}\nBuild #{_config.Build ?? "???"}";
            await ReplyAsync(output);
        }
        
        [SlashCommand("fullhelp", RunMode = RunMode.Async), Alias("help")]
        [Help(new[] {".fullhelp"}, "If you need help using help, you're truly lost.", true, "UNObot 1.0")]
        public async Task FullHelp()
        {
            if (!Context.IsPrivate)
                await ReplyAsync("Help has been sent. Or, I think it has.");
            var response = $"```Commands: @{Context.Client.CurrentUser.Username}#{Context.Client.CurrentUser.Discriminator} command/ .command\n (Required) {{May be required}} [Optional]\n \n";
            foreach (var cmd in CommandHandlingService.Commands)
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
                        await Context.User.SendMessageAsync(oldResponse);
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
                    await Context.User.SendMessageAsync(response);
                    response = "```\n";
                }
            }

            response += "```";
            await Context.User.SendMessageAsync(response);
        }

        [SlashCommand("help", RunMode = RunMode.Async)]
        [Alias("ahh", "ahhh", "ahhhh")]
        [Help(new[] {".help (command)"}, "If you need help using help, you're truly lost.", true, "UNObot 1.0")]
        public async Task Help(
            [SlashCommandOption("Get help about this specific command.")]
            string cmdSearch
            )
        {
            var response = "";
            var cmd = CommandHandlingService.FindCommand(cmdSearch);
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