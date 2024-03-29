﻿using System;
using System.Threading.Tasks;
using Discord;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;

namespace ConnectBot.Services;

public abstract class EmbedService
{
    private readonly IUNObotConfig _config;

    public EmbedService(IUNObotConfig config)
    {
        _config = config;
    }

    public async Task<IUserMessage> ErrorEmbed(IUNObotCommandContext context, string message, MessageComponent component = null, bool ghost = false)
    {
        var error = new EmbedBuilder()
            .WithTitle("Error!!")
            .WithDescription(message)
            .WithColor(Color.Red);
        var embed = Build(error, context, false);
        return await PluginHelper.GhostMessage(context,
            null,
            "**Warning: the bot has no embed permissions, and ConnectBot will not display a board without embeds!**\n" +
            $"Error!! - {message}",
            embed, 
            component,
            ghost ? 5000 : -1);
    }

    public async Task<IUserMessage> SuccessEmbed(IUNObotCommandContext context, string message, MessageComponent component = null, bool ghost = false)
    {
        var error = new EmbedBuilder()
            .WithTitle("Success!!")
            .WithDescription(message)
            .WithColor(Color.Green);
        var embed = Build(error, context, false);
        return await PluginHelper.GhostMessage(context,
            null,
            "**Warning: the bot has no embed permissions, and ConnectBot will not display a board without embeds!**\n" +
            $"Success!! - {message}",
            embed,
            component,
            ghost ? 5000 : -1);
    }

    public Embed Build(EmbedBuilder embed, IUNObotCommandContext context, bool addColor = true)
    {
        if (addColor)
            embed.WithColor(PluginHelper.RandomColor());
            
        return embed
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter(footer =>
            {
                footer
                    .WithText($"ConnectBot {_config.Version} - By DoggySazHi")
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