using System.Threading.Tasks;
using ConnectBot.Services;
using ConnectBot.Templates;
using Discord.Commands;
using UNObot.Plugins.Attributes;

namespace ConnectBot.Modules
{
    public class MainCommands : ModuleBase<SocketCommandContext>
    {
        private readonly EmbedService _embed;

        public MainCommands(EmbedService embed, ButtonHandler button)
        {
            _embed = embed;
            // Required to set up the handler, since modules are always pre-loaded.
            button.Callback = embed.DropPiece;
        }
        
        [Command("cbot", RunMode = RunMode.Async)]
        public async Task ConnectBot()
        {
            await _embed.DisplayHelp(new FakeContext(Context));
        }
        
        [DisableDMs]
        [Command("cbot", RunMode = RunMode.Async)]
        public async Task ConnectBot([Remainder] string input)
        {
            var args = input.Trim().ToLower().Split(' ');
            switch (args[0])
            {
                case "join":
                    await _embed.JoinGame(new FakeContext(Context));
                    break;
                case "leave":
                    await _embed.LeaveGame(new FakeContext(Context));
                    break;
                case "start":
                    await _embed.StartGame(new FakeContext(Context));
                    break;
                case "game":
                    await _embed.DisplayGame(new FakeContext(Context));
                    break;
                case "drop":
                    await _embed.DropPiece(new FakeContext(Context), args);
                    break;
                case "help":
                case "":
                    await _embed.DisplayHelp(new FakeContext(Context));
                    break;
            }
        }
    }
}