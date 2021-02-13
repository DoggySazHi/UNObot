using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using UNObot.Google.Services;
using UNObot.Plugins.Attributes;
using UNObot.Plugins.TerminalCore;

namespace UNObot.Google.Modules
{
    public class CoreCommands : ModuleBase<SocketCommandContext>
    {
        private readonly GoogleSearchService _google;
        private readonly IConfiguration _config;
        
        public CoreCommands(GoogleSearchService google, IConfiguration config)
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

            var r = ThreadSafeRandom.ThisThreadsRandom;
            var embed = new EmbedBuilder()
                .WithTitle($"Search results for \"{query}\"")
                .WithDescription(search.Preview)
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
}