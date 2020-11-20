using System;
using System.Threading.Tasks;
using ConnectBot.Templates;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using UNObot.Plugins.TerminalCore;

namespace ConnectBot.Services
{
    public abstract class EmbedService
    {
        private readonly IConfiguration _config;

        public EmbedService(IConfiguration config)
        {
            _config = config;
        }
        
        internal async Task<IUserMessage> GhostMessage(ICommandContext context, string text = null, string fallback = null, Embed embed = null, int time = 5000)
        {
            if (text == null && embed == null)
                return null;
            IUserMessage message;
            try
            {
                message = await context.Channel.SendMessageAsync(text, embed: embed);
            }
            catch (CommandException)
            {
                fallback ??= text;
                message = await context.Channel.SendMessageAsync(fallback);
            }

            if (time <= 0)
                return message;
            await Task.Delay(time);
            await message.DeleteAsync();
            return null;
        }

        internal async Task<IUserMessage> ErrorEmbed(ICommandContextEx context, string message, bool ghost = false)
        {
            var error = new EmbedBuilder()
                .WithTitle("Error!!")
                .WithDescription(message)
                .WithColor(Color.Red);
            var embed = Build(error, context, false);
            return await GhostMessage(context,
                text: null,
                fallback:
                "**Warning: the bot has no embed permissions, and ConnectBot will not display a board without embeds!**\n" +
                $"Error!! - {message}",
                embed: embed, ghost ? 5000 : -1);
        }

        internal async Task<IUserMessage> SuccessEmbed(ICommandContextEx context, string message, bool ghost = false)
        {
            var error = new EmbedBuilder()
                .WithTitle("Success!!")
                .WithDescription(message)
                .WithColor(Color.Green);
            var embed = Build(error, context, false);
            return await GhostMessage(context,
                text: null,
                fallback:
                "**Warning: the bot has no embed permissions, and ConnectBot will not display a board without embeds!**\n" +
                $"Success!! - {message}",
                embed: embed, ghost ? 5000 : -1);
        }

        internal Embed Build(EmbedBuilder embed, ICommandContextEx context, bool addColor = true)
        {
            var r = ThreadSafeRandom.ThisThreadsRandom;
            if (addColor)
                embed.WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)));
            
            return embed
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"ConnectBot {_config["version"]} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    var guildName = $"{context.User.Username}'s DMs";
                    if (!context.IsPrivate)
                        guildName = context.Guild.Name;
                    author
                        .WithName($"Playing in {guildName}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                }).Build();
        }
    }
}