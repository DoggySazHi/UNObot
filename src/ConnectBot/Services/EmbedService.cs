using System;
using System.Linq;
using System.Threading.Tasks;
using ConnectBot.Templates;
using Discord;
using Discord.Commands;
using Discord.Net;
using Microsoft.Extensions.Configuration;
using UNObot.Plugins.TerminalCore;
using Game = ConnectBot.Templates.Game;

namespace ConnectBot.Services
{
    public class EmbedService
    {
        private readonly IConfiguration _config;
        private readonly DatabaseService _db;
        private readonly AFKTimerService _afk;
        
        public EmbedService(IConfiguration config, DatabaseService db, AFKTimerService afk)
        {
            _config = config;
            _db = db;
            _afk = afk;
        }

        public async Task DisplayGame(SocketCommandContext context)
        {
            var game = await _db.GetGame(context.Guild.Id);

            if (game == null || !game.Queue.GameStarted())
            {
                await ErrorEmbed(context, "There is no game active on this server!");
                return;
            }

            await DisplayGame(context, game);
        }

        private async Task DisplayGame(SocketCommandContext context, Game game, string text = null)
        {
            var queue = game.Queue;
            var board = game.Board;
            game.Description = $"It is now {context.Client.GetUser(queue.CurrentPlayer().Player).Username}'s turn.";

            var builder = new EmbedBuilder()
                .WithTitle("Current Game")
                .WithColor(Board.Colors[queue.CurrentPlayer().Color].Value)
                .AddField(game.Description, board.GenerateField());

            if (game.LastChannel != null && game.LastMessage != null && game.LastMessage != 0)
                try
                {
                    await context.Channel.DeleteMessageAsync(game.LastMessage.Value);
                }
                catch (HttpException) { /* ignore */ }
            
            var message = await context.Channel.SendMessageAsync(text, embed: Build(builder, context));
            game.LastChannel = context.Channel.Id;
            game.LastMessage = message.Id;

            try
            {
                var winnerColor = board.Winner();
                if (winnerColor != 0)
                {
                    if (winnerColor == -1)
                    {
                        await context.Channel.SendMessageAsync($"It's a draw... the board is full!");
                    }
                    else
                    {
                        var index = queue.InGame.Values.ToList().FindIndex(o => o == winnerColor);
                        await context.Channel.SendMessageAsync($"<@{queue.InGame[index].Key}> won the game!");
                    }
                    await NextGame(context, game);
                }
            }
            catch (IndexOutOfRangeException)
            {
                await ErrorEmbed(context, ">:[ There was an internal error with the table scanning algorithm.");
                await _db.UpdateGame(game);
                throw;
            }

            await _db.UpdateGame(game);
        }

        public async Task DisplayHelp(SocketCommandContext context)
        {
            var help = new EmbedBuilder()
                .WithTitle("Quick-start guide to ConnectBot")
                .AddField("Usages", $"@{context.Client.CurrentUser.Username}#{context.Client.CurrentUser.Discriminator} cbot *commandtorun*\n.cbot *commandtorun*")
                .AddField(".cbot join", "Join a game in the current server.", true)
                .AddField(".cbot leave", "Leave a game in the current server.", true)
                .AddField(".cbot start", "Start a game in the current server.\nYou must have joined beforehand.", true)
                .AddField(".cbot drop", "Drop a piece in the specified (1-indexed) column.", true)
                .AddField(".cbot game", "See the board.", true)
                .AddField(".fullhelp", "See an extended listing of commands.\nNice!", true);
            await context.Channel.SendMessageAsync(embed: Build(help, context));
        }
        
        public async Task JoinGame(SocketCommandContext context)
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

        public async Task LeaveGame(SocketCommandContext context)
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
        
        public async Task StartGame(SocketCommandContext context)
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

            await NextGame(context, game, true);
        }

        
        
        private async Task NextGame(SocketCommandContext context, Game game, bool newGame = false)
        {
            var board = game.Board;
            var queue = game.Queue;
            
            board.Reset();
            if (queue.Start())
            {
                if(newGame)
                    _afk.StartTimer(context, NextGame);
                else
                    _afk.ResetTimer(context);
                var players = queue.InGame.Keys.Aggregate("", (current, id) => current + $"- <@{id}>\n");
                players = players.Remove(players.Length - 1);
                await DisplayGame(context, game,
                    "The next batch of players are up!\n" +
                    $"Players for this round: {players}\n" +
                    $"It is now <@{queue.CurrentPlayer().Player}>'s turn.");
            }
            else
            {
                await ErrorEmbed(context, "There are not enough players to start another game!");
                await _db.UpdateGame(game);
            }
        }

        public async Task DropPiece(SocketCommandContext context, string[] args)
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

        private async Task ErrorEmbed(SocketCommandContext context, string message)
        {
            var error = new EmbedBuilder()
                .WithTitle("Error!!")
                .WithDescription(message)
                .WithColor(Color.Red);
            await context.Channel.SendMessageAsync(embed: Build(error, context, false));
        }
        
        private async Task SuccessEmbed(SocketCommandContext context, string message)
        {
            var error = new EmbedBuilder()
                .WithTitle("Success!!")
                .WithDescription(message)
                .WithColor(Color.Green);
            await context.Channel.SendMessageAsync(embed: Build(error, context, false));
        }

        private Embed Build(EmbedBuilder embed, SocketCommandContext context, bool addColor = true)
        {
            var r = ThreadSafeRandom.ThisThreadsRandom;
            if (addColor)
                embed.WithColor(new Color(r.Next(0, 256), r.Next(0, 256), r.Next(0, 256)));
            
            return embed
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"ConnectBot {_config["version"]} - By DoggySazHi")
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
}