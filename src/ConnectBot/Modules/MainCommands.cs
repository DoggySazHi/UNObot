using System.Threading.Tasks;
using ConnectBot.Services;
using Discord.Commands;
using UNObot.Plugins;
using UNObot.Plugins.Attributes;

namespace ConnectBot.Modules
{
    public class MainCommands : UNObotModule<UNObotCommandContext>
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
            await _cs.DisplayHelp(Context);
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
                    await _game.JoinGame(Context);
                    break;
                case "leave":
                    await _game.LeaveGame(Context);
                    break;
                case "start":
                    await _game.StartGame(Context, args);
                    break;
                case "game":
                    await _game.DisplayGame(Context);
                    break;
                case "drop":
                    await _game.DropPiece(Context, args);
                    break;
                case "board":
                    await _cs.SetUserBoardDefaults(Context, args);
                    break;
                case "queue":
                    await _game.GetQueue(Context);
                    break;
                case "userinfo":
                case "stats":
                    await _cs.GetStats(Context, args);
                    break;
                case "help":
                case "":
                    await _cs.DisplayHelp(Context);
                    break;
            }
        }
    }
}