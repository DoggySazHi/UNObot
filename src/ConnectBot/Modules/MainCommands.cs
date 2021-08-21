using System.Threading.Tasks;
using ConnectBot.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
        [SlashCommand("cbot", RunMode = RunMode.Async)]
        [Help(new [] {".cbot help"}, "The base command for ConnectBot. Use .cbot help for more info.", true, "UNObot 4.2.0")]
        public async Task ConnectBot(
            [Remainder]
            [SlashCommandOption("A subcommand for UNObot. See .cbot help for more info.",
                new object[] { "join", "leave", "start", "game", "drop", "board", "queue", "stats", "help" })]
            string input)
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
        
        [DisableDMs]
        [SlashCommand("cbotdebug", RunMode = RunMode.Async, Guild = 420005591155605535)]
        [SlashSubcommand("join", "Join the ConnectBot queue in the current server.")]
        [Help(new [] {".cbot help"}, "The base command for ConnectBot. Use .cbot help for more info.", true, "UNObot 4.3 Beta 9")]
        public async Task ConnectBotDebugA()
        {
            await _db.AddUser(Context.User.Id);
            await _game.JoinGame(Context);
        }
        
        [DisableDMs]
        [SlashCommand("cbotdebug", RunMode = RunMode.Async, Guild = 420005591155605535)]
        [SlashSubcommand("leave", "Leave the ConnectBot queue in the current server.")]
        [Help(new [] {".cbot help"}, "The base command for ConnectBot. Use .cbot help for more info.", true, "UNObot 4.3 Beta 9")]
        public async Task ConnectBotDebugB()
        {
            await _db.AddUser(Context.User.Id);
            await _game.LeaveGame(Context);
        }
        
        [DisableDMs]
        [SlashCommand("cbotdebug", RunMode = RunMode.Async, Guild = 420005591155605535)]
        [SlashSubcommand("start", "Start the queue for ConnectBot.")]
        [Help(new [] {".cbot help"}, "The base command for ConnectBot. Use .cbot help for more info.", true, "UNObot 4.3 Beta 9")]
        public async Task ConnectBotDebugC(
            [SlashCommandOption("A start option.", new object[] { "blind", "custom", "normal" }, Required = false)] string optionA,
            [SlashCommandOption("A start option.", new object[] { "blind", "custom", "normal" }, Required = false)] string optionB)
        {
            await _db.AddUser(Context.User.Id);
            await _game.StartGame(Context, new[] { null, optionA, optionB });
        }
        
        [DisableDMs]
        [SlashCommand("cbotdebug", RunMode = RunMode.Async, Guild = 420005591155605535)]
        [SlashSubcommand("game", "Show the current game status.")]
        [Help(new [] {".cbot help"}, "The base command for ConnectBot. Use .cbot help for more info.", true, "UNObot 4.3 Beta 9")]
        public async Task ConnectBotDebugD()
        {
            await _db.AddUser(Context.User.Id);
            await _game.DisplayGame(Context);
        }
        
        [DisableDMs]
        [SlashCommand("cbotdebug", RunMode = RunMode.Async, Guild = 420005591155605535)]
        [SlashSubcommand("drop", "Drop a piece.")]
        [Help(new [] {".cbot help"}, "The base command for ConnectBot. Use .cbot help for more info.", true, "UNObot 4.3 Beta 9")]
        public async Task ConnectBotDebugE(
            [SlashCommandOption("Position of your next piece.", OptionType = ApplicationCommandOptionType.Integer, Required = true)] int position
            )
        {
            await _db.AddUser(Context.User.Id);
            await _game.DropPiece(Context, new [] { null, "" + position });
        }
        
        [DisableDMs]
        [SlashCommand("cbotdebug", RunMode = RunMode.Async, Guild = 420005591155605535)]
        [SlashSubcommand("board", "Set your board config.")]
        [Help(new [] {".cbot help"}, "The base command for ConnectBot. Use .cbot help for more info.", true, "UNObot 4.3 Beta 9")]
        public async Task ConnectBotDebugF(
            [SlashCommandOption("A start option.", OptionType = ApplicationCommandOptionType.Integer, Required = true)] string rows,
            [SlashCommandOption("A start option.", OptionType = ApplicationCommandOptionType.Integer, Required = true)] string columns,
            [SlashCommandOption("A start option.", OptionType = ApplicationCommandOptionType.Integer, Required = true)] string connect)
        {
            await _db.AddUser(Context.User.Id);
            await _cs.SetUserBoardDefaults(Context, new [] { null, "" + rows, "" + columns, "" + connect });
        }

        [DisableDMs]
        [SlashCommand("cbotdebug", RunMode = RunMode.Async, Guild = 420005591155605535)]
        [SlashSubcommand("queue", "Get the current queue of players waiting to play.")]
        [Help(new[] { ".cbot help" }, "The base command for ConnectBot. Use .cbot help for more info.", true,
            "UNObot 4.3 Beta 9")]
        public async Task ConnectBotDebugG()
        {
            await _db.AddUser(Context.User.Id);
            await _game.GetQueue(Context);
        }

        [DisableDMs]
        [SlashCommand("cbotdebug", RunMode = RunMode.Async, Guild = 420005591155605535)]
        [SlashSubcommand("stats", "Get your (or someone else's) stats.")]
        [Help(new [] {".cbot help"}, "The base command for ConnectBot. Use .cbot help for more info.", true, "UNObot 4.3 Beta 9")]
        public async Task ConnectBotDebugH(
            [SlashCommandOption("A user to get their stats.", Required = false)] SocketUser user)
        {
            await _db.AddUser(Context.User.Id);
            await _cs.GetStats(Context, new [] { null, "" + user.Id });
        }
        
        [DisableDMs]
        [SlashCommand("cbotdebug", RunMode = RunMode.Async, Guild = 420005591155605535)]
        [SlashSubcommand("help", "Show a help menu for ConnectBot.")]
        [Help(new [] {".cbot help"}, "The base command for ConnectBot. Use .cbot help for more info.", true, "UNObot 4.3 Beta 9")]
        public async Task ConnectBotDebugI()
        {
            await _db.AddUser(Context.User.Id);
            await _cs.DisplayHelp(Context);
        }
    }
}