using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Misc.Services;
using UNObot.Plugins;

namespace UNObot.Misc.Modules
{
    public class CoreCommands : ModuleBase<UNObotCommandContext>
    {
        private readonly DatabaseService _db;
        
        public CoreCommands(DatabaseService db)
        {
            _db = db;
        }
        
        [Command("migrateall", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task MigrateSettings()
        {
            var message = await ReplyAsync("Migrating...");
            await _db.Migrate();
            await message.ModifyAsync(o => o.Content = "Finished migration.");
        }

        [Command("components", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task ComponentTest()
        {
            var c1 = new ComponentBuilder()
                .WithButton("Button A", null, ButtonStyle.Link, url: "https://www.youtube.com/watch?v=dQw4w9WgXcQ")
                .WithButton("Button B", "b")
                .WithButton("Button C", "c", ButtonStyle.Danger)
                .WithButton("Button D", "d", ButtonStyle.Success, Emote.Parse("<:reimudab:613573307710701608>"))
                .Build();
            var c2 = new ComponentBuilder()
                .WithSelectMenu(new SelectMenuBuilder().WithCustomId("character-select")
                .WithOptions(new List<SelectMenuOptionBuilder>
                {
                    new SelectMenuOptionBuilder("Reimu", "reimu").WithDescription("poor").WithEmote(Emote.Parse("<:reimuthink:629869106006327303>")),
                    new SelectMenuOptionBuilder("Marisa", "marisa").WithDescription("ordinary magician").WithEmote(Emote.Parse("<:marisapout:806912521503375391>")),
                    new SelectMenuOptionBuilder("Sanae", "sanae").WithDescription("snek").WithEmote(Emote.Parse("<:sanaepout:732061262539915338>")),
                    new SelectMenuOptionBuilder("Sakuya", "succuya").WithDescription("knives and pads").WithEmote(new Emoji("🔪")),
                    new SelectMenuOptionBuilder("Momiji", "momizi").WithDescription("awoo").WithEmote(Emote.Parse("<:momijithink:584209739978899466>")),
                    new SelectMenuOptionBuilder("Cute", "bruh").WithDescription("It's true").WithEmote(new Emoji("❤️"))
                })
                .WithPlaceholder("Pick some characters")
                .WithMaxValues(6)
            );
            await ReplyAsync("Test Message 1", component: c1);
            await ReplyAsync("Test Message 2", component: c2.Build());
        }

        [Command("mukyu", RunMode = RunMode.Async)]
        public async Task Mukyu(bool thing, bool otherThing = false)
        {
            await ReplyAsync($"Received command! {thing} {otherThing}");
        }
    }
}