using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace UNObot.Modules
{
    public class Basecmds : ModuleBase<SocketCommandContext>
    {
        [Command("info")]
        public async Task Info()
        {
            await ReplyAsync(
                $"{Context.Client.CurrentUser.Username} - Created by DoggySazHi\nVersion {Program.version}\nblame Aragami and FM for the existance of this");
            #if DEBUG
            await ReplyAsync("UNObot is currently running in DEBUG mode. Errors are prone to occur.");
            #else
            await ReplyAsync("UNObot is currently running in RELEASE/DEPLOY mode. Everything SHOULD be fine.")
            #endif
        }
        [Command("gulag")]
        public async Task Gulag()
        {
            await ReplyAsync($"<{@Context.User.Id}> has been sent to gulag and has all of his cards converted to red blyats.");
        }
        [Command("nepnep")]
        public async Task Nep()
        {
            await ReplyAsync($"You got me there at \"nep\".");
        }
        [Command("ugay")]
        public async Task Ugay()
            => await ReplyAsync(
                $"<@{Context.User.Id}> no u\n");

        [Command("u gay")]
        public async Task Ugay2()
            => await ReplyAsync(
                $"<@{Context.User.Id}> no u\n");

        [Command("you gay")]
        public async Task Ugay3()
            => await ReplyAsync(
                $"<@{Context.User.Id}> no u\n");

        [Command("no u")]
        public async Task Easteregg1()
        {
            await ReplyAsync($"I claim that <@{Context.User.Id}> is triple gay. Say \"No U\" again, u ded m8.");
        }
        [Command("doggyisthebest")]
        public async Task Easteregg2(string response)
        {
            var messages = await this.Context.Channel.GetMessagesAsync(1).Flatten();

            await this.Context.Channel.DeleteMessagesAsync(messages);
            await ReplyAsync(response);
        }
        [Command("upupdowndownleftrightleftrightbastart")]
        public async Task OldEasterEgg(string asdf)
            => await ReplyAsync("lol, that's outdated");
       [Command("upupdowndownleftrightleftrightbastart")]
        public async Task OldEasterEgg()
            => await ReplyAsync("lol, that's outdated");
        [Command("help")]
        public async Task Help()
        {
            await ReplyAsync("Help has been sent. Or, I think it has.");
            await UserExtensions.SendMessageAsync(Context.Message.Author, "Commands: @UNOBot#4308 (Required) {Required in certain conditions} [Optional] " +
                               "- Join\n" +
                               "Join the queue.\n" +
                               "- Leave" +
                               "Leave the queue.\n" +
                               "- Start\n" +
                               "Start the game. Game only starts when 2 or more players are available.\n" +
                               "- Draw\n" +
                               "Get a card. This is randomized. Does not follow the 108 deck, but uses the probablity instead.\n" +
                               "- Play (Color/Wild) (#/Reverse/Skip/+2/+4/Color) {Wild color change}\n" +
                               "Play a card. You must have the card in your deck. Also, if you are placing a wildcard, type in the color as the next parameter.\n" +
                               "- Card\n" +
                               "See the last placed card.\n" +
                               "- Deck See the cards you have currently.\n" +
                               "- Uno\n" +
                               "Don't forget to say this when you end up with one card left!\n" +
                               "- Help\n" +
                               "Get a help list. But you probably knew this.\n" +
                              "- Seed (seed)\n" +
                              "Possibly increases your chance of winning.\n" +
                              "- Players\n" +
                              "See who is playing and who's turn is it.\n" +
                              "- Stats [player by mention]\n" +
                              "See if you or somebody else is a pro or a noob at UNO. It's probably the former.\n" +
                              "- Info\n" +
                              "See the current version and other stuff about UNObot.");
        }

        [Command("credits")]
        public async Task Credits()
        {
            await ReplyAsync("UNObot: Programmed by DoggySazHi\n" +
                "Tested by Aragami and Fm\n" +
                "Created for the UBOWS server\n\n" +
                "Stickerz was here.\n" +
                "Blame LocalDisk and Harvest for any bugs.");
        }
    }
}