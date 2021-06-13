using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConnectBot.Templates;
using Discord;
using Discord.Net;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;
using UNObot.Plugins.Settings;
using Game = ConnectBot.Templates.Game;

namespace ConnectBot.Services
{
    public class GameService : EmbedService
    {
        private readonly DatabaseService _db;
        private readonly AFKTimerService _afk;
        private readonly ButtonHandler _button;
        private readonly ILogger _logger;
        
        public GameService(IUNObotConfig config, DatabaseService db, AFKTimerService afk, ButtonHandler button, ILogger logger) : base(config)
        {
            _db = db;
            _afk = afk;
            _button = button;
            _logger = logger;
            
            var settings = new Setting("ConnectBot Settings");
            settings.UpdateSetting("Default Channel", new ChannelID(0));
            SettingsManager.RegisterSettings("ConnectBot", settings);
        }

        public async Task DisplayGame(ICommandContextEx context)
        {
            var game = await _db.GetGame(context.Guild.Id);

            if (game == null || !game.Queue.GameStarted())
            {
                await ErrorEmbed(context, "There is no game active on this server!");
                return;
            }

            await DisplayGame(context, game, force: true);
        }

        private static bool _working;

        private async Task DisplayGame(ICommandContextEx context, Game game, string text = null, bool force = false)
        {
            var queue = game.Queue;
            var board = game.Board;
            var currentPlayer =
                await (await context.Client.GetGuildAsync(game.Server)).GetUserAsync(queue.CurrentPlayer().Player);
            
            game.Description = $"It is now {currentPlayer.Nickname} ({currentPlayer.Username}#{currentPlayer.Discriminator})'s turn.";

            var boardDisplay = game.GameMode.HasFlag(GameMode.Blind)
                ? "The game is in blind mode!"
                : board.GenerateField();
            
            var builder = new EmbedBuilder()
                .WithTitle("Current Game")
                .WithColor(Board.Colors[queue.CurrentPlayer().Color].Value)
                .AddField(game.Description, boardDisplay);

            var embed = Build(builder, context);
            var modSuccess = false;

            if (game.LastChannel != null && game.LastMessage != null && game.LastMessage != 0 && !force)
            {
                try
                {
                    if (await context.Channel.GetMessageAsync(game.LastMessage.Value) is IUserMessage message)
                    {
                        if (_working)
                            throw new InvalidOperationException();
                        _working = true;
                        
                        await message.ModifyAsync(o =>
                        {
                            //TODO Check if embeds are not enabled...
                            o.Embed = embed;
                        });
                        PluginHelper.GhostMessage(context, text).ContinueWithoutAwait(_logger);
                        modSuccess = true;
                        await _button.ClearReactions(message, currentPlayer);
                    }
                }
                catch (HttpException ex)
                {
                    _logger.Log(LogSeverity.Error, "Failed to display ConnectBot game!", ex);
                }
                finally
                {
                    _working = false;
                }
            }

            if (!modSuccess)
            {
                var newMessage = await context.Channel.SendMessageAsync(embed: embed);
                PluginHelper.GhostMessage(context, text).ContinueWithoutAwait(_logger);
                _button.AddNumbers(newMessage, new Range(1, board.Width + 1)).ContinueWithoutAwait(_logger);
                game.LastChannel = context.Channel.Id;
                game.LastMessage = newMessage.Id;
            }

            try
            {
                var winnerColor = board.Winner();
                if (winnerColor != 0)
                {
                    if (winnerColor == -1)
                    {
                        await context.Channel.SendMessageAsync("It's a draw... the board is full!");
                    }
                    else
                    {
                        var index = queue.InGame.Values.ToList().FindIndex(o => o == winnerColor);
                        var winner = queue.InGame[index].Key;
                        await context.Channel.SendMessageAsync($"<@{winner}> won the game!");
                        _db.UpdateStats(winner, DatabaseService.ConnectBotStat.GamesWon).ContinueWithoutAwait(_logger);
                    }
                    
                    foreach(var id in queue.InGame.Keys)
                        _db.UpdateStats(id, DatabaseService.ConnectBotStat.GamesPlayed).ContinueWithoutAwait(_logger);
                    
                    await NextGame(context, game);
                }
            }
            catch (IndexOutOfRangeException)
            {
                await ErrorEmbed(context, ">:[ There was an public error with the table scanning algorithm.");
                await _db.UpdateGame(game);
                throw;
            }

            await _db.UpdateGame(game);
        }

        public async Task JoinGame(ICommandContextEx context)
        {
            var game = await _db.GetGame(context.Guild.Id);
            var queue = game.Queue;

            if (queue.Players.Contains(context.User.Id))
            {
                await ErrorEmbed(context, "You are already queued up to play!");
                return;
            }

            if (queue.InGame.ContainsKey(context.User.Id))
            {
                await ErrorEmbed(context, "You are... currently playing in a game.");
                return;
            }

            await SuccessEmbed(context, "You have joined the queue.");
            queue.AddPlayer(context.User.Id);
            await _db.UpdateGame(game);
        }

        public async Task LeaveGame(ICommandContextEx context)
        {
            var game = await _db.GetGame(context.Guild.Id);
            var queue = game.Queue;

            if (queue.Players.Contains(context.User.Id))
            {
                queue.RemovePlayer(context.User.Id);
                await _db.UpdateGame(game);
                await SuccessEmbed(context, "You have left the queue.");
                return;
            }

            if (queue.InGame.ContainsKey(context.User.Id))
            {
                queue.RemovePlayer(context.User.Id);
                if (queue.InGame.Count <= 1)
                {
                    await SuccessEmbed(context, "You have left the game. The current game will be reset.");
                    await NextGame(context, game);
                }
                else
                {
                    var nextPlayer = queue.Next();
                    await _db.UpdateGame(game);
                    await SuccessEmbed(context, $"You have left the game. It is now <@{nextPlayer.Player}>'s turn.");
                }
                return;
            }

            await ErrorEmbed(context, "You are not in the queue or a game!");
        }
        
        public async Task StartGame(ICommandContextEx context, string[] args)
        {
            var game = await _db.GetGame(context.Guild.Id);
            var queue = game.Queue;

            if (!queue.Players.Contains(context.User.Id))
            {
                await ErrorEmbed(context, "You did not join the queue!");
                return;
            }
            
            if (queue.GameStarted())
            {
                await ErrorEmbed(context, "The game has already started!");
                return;
            }
            
            game.GameMode = GameMode.Normal;
            foreach (var mode in args[1..])
                switch (mode.ToLower().Trim())
                {
                    case "blind":
                        game.GameMode |= GameMode.Blind;
                        break;
                    case "custom":
                        game.GameMode |= GameMode.Custom;
                        break;
                    case "normal":
                        game.GameMode |= GameMode.Normal;
                        break;
                    default:
                        await ErrorEmbed(context, $"\"{mode}\" is not a valid mode!");
                        return;
                }

            if (game.GameMode.HasFlag(GameMode.Custom))
            {
                var (defaultWidth, defaultHeight, defaultConnect) = await _db.GetDefaultBoardDimensions(context.User.Id);
                game.Board = new Board(defaultWidth, defaultHeight, defaultConnect);
            }
            else
                game.Board = new Board();

            await NextGame(context, game, true);
        }

        private async Task NextGame(ICommandContextEx context, Game game, bool newGame = false)
        {
            var board = game.Board;
            var queue = game.Queue;
            
            board.Reset();
            if (queue.Start())
            {
                game.LastChannel = null;
                game.LastMessage = null;
                if(newGame)
                    _afk.StartTimer(context, NextGame);
                else
                    _afk.ResetTimer(context);
                var players = queue.InGame.Keys.Aggregate("", (current, id) =>
                {
                    _db.UpdateStats(id, DatabaseService.ConnectBotStat.GamesJoined).ContinueWithoutAwait(_logger);
                    return current + $"- <@{id}>\n";
                });
                
                // Remove the newline char.
                players = players.Remove(players.Length - 1);
                
                await DisplayGame(context, game,
                    $"Playing in modes: {game.GameMode}!\n\n" +
                    "The next batch of players are up!\n" +
                    $"Players for this round: {players}\n" +
                    $"It is now <@{queue.CurrentPlayer().Player}>'s turn.");
            }
            else
            {
                if (newGame)
                    await ErrorEmbed(context, "There are not enough players to start a game!");
                else
                    await context.Channel.SendMessageAsync("There are no more players in the queue. Join with ``.cbot join``!");
            }
            
            await _db.UpdateGame(game);
        }

        public async Task DropPiece(ICommandContextEx context, string[] args)
        {
            var game = await _db.GetGame(context.Guild.Id);
            _afk.ResetTimer(context);
            var board = game.Board;
            var queue = game.Queue;

            if (!queue.InGame.ContainsKey(context.User.Id))
            {
                await ErrorEmbed(context, "You are currently not playing!");
                return;
            }
            
            if (queue.CurrentPlayer().Player != context.User.Id)
            {
                await ErrorEmbed(context, "It is not your turn!");
                return;
            }

            var parse = int.TryParse(args[1], out var position);

            if (args.Length == 1 || !parse)
            {
                await ErrorEmbed(context, "You need to provide a position to drop the piece in!");
                return;
            }

            var result = board.Drop(position, queue.CurrentPlayer().Color);

            switch (result)
            {
                case BoardStatus.Invalid:
                    await ErrorEmbed(context, "That is not a valid position to place a piece! " +
                                              $"Pick a position between 1 and {board.Width}, inclusive.");
                    return;
                case BoardStatus.Full:
                    await ErrorEmbed(context, "The column is full. Pick another column.");
                    return;
                case BoardStatus.Success:
                    var nextPlayer = queue.Next();
                    if(context.IsMessage)
                        try
                        {
                            await context.Message.DeleteAsync();
                        }
                        catch (HttpException) { /* ignore */ }
                    await DisplayGame(context, game, $"It is now <@{nextPlayer.Player}>'s turn.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task GetQueue(FakeContext context)
        {
            var game = await _db.GetGame(context.Guild.Id);
            
            var players = new StringBuilder();
            foreach (var id in game.Queue.Players)
                players.Append($"- <@{id}>\n");
            if (players.Length != 0)
                players.Remove(players.Length - 1, 1);
            else
                players.Append("There is nobody in the queue. Join with .cbot join!");
            var embed = new EmbedBuilder()
                .AddField($"Queue in {context.Guild.Name}", players);
            
            await context.Channel.SendMessageAsync(embed: Build(embed, context));
        }
    }
}