using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Google.Services;
using UNObot.Plugins;
using UNObot.Plugins.Attributes;
using UNObot.Plugins.Helpers;

namespace UNObot.Google.Modules;

public class CoreCommands : UNObotModule<UNObotCommandContext>
{
    private readonly GoogleSearchService _google;
    private readonly IUNObotConfig _config;
        
    public CoreCommands(GoogleSearchService google, IUNObotConfig config)
    {
        _google = google;
        _config = config;
    }
        
    [Command("search", RunMode = RunMode.Async)]
    [Help(new[] {".search"}, "Search using Google.", true,
        "UNObot 4.2.10")]
    public async Task TestPerms([Remainder] string query)
    {
        if (query.Length > 50)
        {
            await ReplyAsync("Your query is too long... it should be less than 50 characters!");
            return;
        }

        var message = await ReplyAsync("I am now searching, please wait warmly...");
        var search = await _google.Search(query);
        if (search == null)
        {
            await message.ModifyAsync(o =>
            {
                o.Content = $"No results could be found for \"{query}\"!";
            });
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"Search results for \"{query}\"")
            .WithDescription(search.Preview)
            .WithColor(PluginHelper.RandomColor())
            .WithTimestamp(DateTimeOffset.Now)
            .WithFooter(footer =>
            {
                footer
                    .WithText($"UNObot {_config.Version} - By DoggySazHi")
                    .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
            })
            .WithAuthor(author =>
            {
                author
                    .WithName($"Searching for {Context.Guild.Name}")
                    .WithIconUrl("https://williamle.com/unobot/unobot.png");
            }).Build();
        await message.ModifyAsync(o =>
        {
            o.Content = search.URL;
            o.Embed = embed;
        });
    }
}