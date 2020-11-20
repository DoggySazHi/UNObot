using System;
using System.Threading.Tasks;
using ConnectBot.Templates;
using Discord;
using Microsoft.Extensions.Configuration;
using UNObot.Plugins.Helpers;

namespace ConnectBot.Services
{
    public class ConfigService : EmbedService
    {
        private readonly DatabaseService _db;
        
        public ConfigService(IConfiguration config, DatabaseService db) : base(config)
        {
            _db = db;
        }
        
        public async Task DisplayHelp(ICommandContextEx context)
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

        public async Task SetUserBoardDefaults(ICommandContextEx fakeContext, string[] args)
        {
            if (args.Length != 4)
            {
                (await ErrorEmbed(fakeContext, "The parameters should be .cbot board [width] [height] [connect].")).MakeDeletable();
                return;
            }

            if (!int.TryParse(args[1], out var width) || width <= 0)
            {
                (await ErrorEmbed(fakeContext, "The width of the board should be an integer greater than 1!")).MakeDeletable();
                return;
            }
            
            if (!int.TryParse(args[2], out var height) || height <= 0)
            {
                (await ErrorEmbed(fakeContext, "The height of the board should be an integer greater than 1!")).MakeDeletable();
                return;
            }
            
            if (!int.TryParse(args[3], out var connect) || connect <= 0)
            {
                (await ErrorEmbed(fakeContext, "The connect value of the board should be an integer greater than 1!")).MakeDeletable();
                return;
            }

            if (connect > width || connect > height || connect > Math.Sqrt(Math.Pow(width, 2) + Math.Pow(height, 2)))
            {
                (await ErrorEmbed(fakeContext, "The connect length is too long, and cannot fit in the board!")).MakeDeletable();
                return;
            }
            
            await _db.SetDefaultBoardDimensions(fakeContext.User.Id, width, height, connect);
            await SuccessEmbed(fakeContext, $"Updated your board dimensions to a {width}x{height} with a required connection of {connect}!");
        }

        public async Task GetStats(FakeContext context)
        {
            var (gamesJoined, gamesPlayed, gamesWon) = await _db.GetStats(context.User.Id);
            var (defaultWidth, defaultHeight, defaultConnect) = await _db.GetDefaultBoardDimensions(context.User.Id);
            
            var embed = new EmbedBuilder()
                .WithTitle($"User Info for {context.User.Username}")
                .AddField("**Game Stats**", 
                    $"Games joined: {gamesJoined}\n" +
                    $"Games played: {gamesPlayed}\n" +
                    $"Games won: {gamesWon}", true)
                .AddField("**Game Config**", 
                    $"Board width: {defaultWidth}\n" +
                    $"Board height: {defaultHeight}\n" +
                    $"Connect length: {defaultConnect}", true);

            await context.Channel.SendMessageAsync(embed: Build(embed, context));
        }
    }
}