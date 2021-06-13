using System.Threading.Tasks;
using ConnectBot.Services;
using ConnectBot.Templates;
using Discord.Commands;
using UNObot.Plugins.Attributes;

namespace ConnectBot.Modules
{
    public class MainCommands : ModuleBase<SocketCommandContext>
    {
        private readonly GameService _game;
        private readonly ConfigService _cs;
        private readonly DatabaseService _db;

        public MainCommands(GameService game, ConfigService cs, ButtonHandler button, DatabaseService db)
        {
            _game = game;
            _cs = cs;
            _db = db;
            // Required to set up the handler, since modules are always pre-loaded.
            button.Callback = game.DropPiece;
        }
        
        [Command("cbot", RunMode = RunMode.Async)]
        public async Task ConnectBot()
        {
            await _db.AddUser(Context.User.Id);
            await _cs.DisplayHelp(new FakeContext(Context));
        }
        
        [DisableDMs]
        [Command("cbot", RunMode = RunMode.Async)]
        [Help(new [] {".cbot help"}, "The base command for ConnectBot. Use .cbot help for more info.", true, "UNObot 4.2.0")]
        public async Task ConnectBot([Remainder] string input)
        {
            await _db.AddUser(Context.User.Id);
            var args = input.Trim().ToLower().Split(' ');
            switch (args[0])
            {
                case "join":
                    await _game.JoinGame(new FakeContext(Context));
                    break;
                case "leave":
                    await _game.LeaveGame(new FakeContext(Context));
                    break;
                case "start":
                    await _game.StartGame(new FakeContext(Context), args);
                    break;
                case "game":
                    await _game.DisplayGame(new FakeContext(Context));
                    break;
                case "drop":
                    await _game.DropPiece(new FakeContext(Context), args);
                    break;
                case "board":
                    await _cs.SetUserBoardDefaults(new FakeContext(Context), args);
                    break;
                case "queue":
                    await _game.GetQueue(new FakeContext(Context));
                    break;
                case "userinfo":
                case "stats":
                    await _cs.GetStats(new FakeContext(Context), args);
                    break;
                case "help":
                case "":
                    await _cs.DisplayHelp(new FakeContext(Context));
                    break;
            }
        }
    }
}