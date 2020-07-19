using System;
using System.Linq;
using System.Threading.Tasks;
using ConnectBot.Templates;
using Discord;
using Discord.Commands;
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

        private async Task DisplayGame(SocketCommandContext context, Game game)
        {
            var queue = game.Queue;
            var board = game.Board;

            var builder = new EmbedBuilder()
                .WithTitle("Current Game")
                .WithColor(Board.Colors[queue.CurrentPlayer().Color].Value)
                .AddField(game.Description, board.GenerateField());

            if (game.LastChannel != null && game.LastMessage != null)
                try
                {
                    await context.Channel.DeleteMessageAsync(game.LastMessage.Value);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            
            var message = await context.Channel.SendMessageAsync(embed: Build(builder, context));
            game.LastChannel = context.Channel.Id;
            game.LastMessage = message.Id;
            
            await _db.UpdateGame(game);
        }

        public async Task DisplayHelp(SocketCommandContext context)
        {
            var help = new EmbedBuilder()
                .WithTitle("Quick-start guide to ConnectBot")
                .AddField("Usages", "@UNOBot#4308 cbot *commandtorun*\n.cbot *commandtorun*")
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

            queue.AddPlayer(context.User.Id);
            await _db.UpdateGame(game);
        }

        public async Task LeaveGame(SocketCommandContext context)
        {
            var game = await _db.GetGame(context.Guild.Id);
            var queue = game.Queue;
            var board = game.Board;

            if (queue.Players.Contains(context.User.Id))
            {
                queue.RemovePlayer(context.User.Id);
                await SuccessEmbed(context, "You have left the queue.");
                return;
            }

            if (queue.InGame.ContainsKey(context.User.Id))
            {
                queue.RemovePlayer(context.User.Id);
                if (queue.InGame.Count <= 1)
                {
                    await SuccessEmbed(context, "You have left the game. The current game will be reset.");
                    board.Reset();
                    if (queue.Start())
                        await StartGame(context);
                    //TODO Reset game?
                }
                else
                {
                    queue.Next();
                    await SuccessEmbed(context, $"You have left the game. It is now <@{queue.CurrentPlayer()}>'s turn.");
                }
            }

            await _db.UpdateGame(game);
        }

        public async Task StartGame(SocketCommandContext context)
        {
            throw new NotImplementedException();
        }

        public async Task DropPiece(SocketCommandContext context, string[] args)
        {
            //TODO AFK Timer?
            var game = await _db.GetGame(context.Guild.Id);
            var board = game.Board;
            var queue = game.Queue;

            if (!queue.InGame.ContainsKey(context.User.Id))
            {
                await ErrorEmbed(context, "You are currently not playing!");
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
                    break;
                case BoardStatus.Full:
                    break;
                case BoardStatus.Success:
                    await DisplayGame(context);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            await _db.UpdateGame(game);
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