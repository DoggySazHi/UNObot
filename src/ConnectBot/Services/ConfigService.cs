using System;
using System.Threading.Tasks;
using Discord;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;

namespace ConnectBot.Services;

public class ConfigService : EmbedService
{
    private readonly DatabaseService _db;
        
    public ConfigService(IUNObotConfig config, DatabaseService db) : base(config)
    {
        _db = db;
    }
        
    public async Task DisplayHelp(IUNObotCommandContext context)
    {
        var help = new EmbedBuilder()
            .WithTitle("Quick-start guide to ConnectBot")
            .AddField("Usages", $"@{context.Client.CurrentUser.Username}#{context.Client.CurrentUser.Discriminator} cbot *commandtorun*\n.cbot *commandtorun*")
            .AddField(".cbot join", "Join a game in the current server.", true)
            .AddField(".cbot leave", "Leave a game in the current server.", true)
            .AddField(".cbot start", "Start a game in the current server.\nYou must have joined beforehand.", true)
            .AddField(".cbot drop", "Drop a piece in the specified (1-indexed) column.", true)
            .AddField(".cbot game", "See the board.", true)
            .AddField(".cbot stats (user)", "See config options and stats of oneself or others.", true)
            .AddField(".fullhelp", "See an extended listing of commands.\nNice!", true);
        await context.ReplyAsync(embed: Build(help, context));
    }

    public async Task SetUserBoardDefaults(IUNObotCommandContext fakeContext, string[] args)
    {
        if (args.Length != 4)
        {
            (await ErrorEmbed(fakeContext, "The parameters should be .cbot board [width] [height] [connect].")).MakeDeletable(fakeContext.User.Id);
            return;
        }

        if (!int.TryParse(args[1], out var width) || width <= 0)
        {
            (await ErrorEmbed(fakeContext, "The width of the board should be an integer greater than 1!")).MakeDeletable(fakeContext.User.Id);
            return;
        }
            
        if (!int.TryParse(args[2], out var height) || height <= 0)
        {
            (await ErrorEmbed(fakeContext, "The height of the board should be an integer greater than 1!")).MakeDeletable(fakeContext.User.Id);
            return;
        }
            
        if (!int.TryParse(args[3], out var connect) || connect <= 0)
        {
            (await ErrorEmbed(fakeContext, "The connect value of the board should be an integer greater than 1!")).MakeDeletable(fakeContext.User.Id);
            return;
        }

        if (width > 8 || height > 8)
        {
            (await ErrorEmbed(fakeContext, "The width and height of the board is too large (max 8x8, due to embed restrictions)!")).MakeDeletable(fakeContext.User.Id);
            return;
        }
            
        if (connect > width || connect > height || connect > Math.Sqrt(Math.Pow(width, 2) + Math.Pow(height, 2)))
        {
            (await ErrorEmbed(fakeContext, "The connect length is too long, and cannot fit in the board!")).MakeDeletable(fakeContext.User.Id);
            return;
        }
            
        await _db.SetDefaultBoardDimensions(fakeContext.User.Id, width, height, connect);
        await SuccessEmbed(fakeContext, $"Updated your board dimensions to a {width}x{height} with a required connection of {connect}!");
    }

    public async Task GetStats(IUNObotCommandContext context, string[] args)
    {
        if (args.Length == 1)
        {
            await DisplayStats(context, context.User.Id);
            return;
        }
            
        var user = string.Join(' ', args[1..]).Trim();
            
        //Style of Username#XXXX or Username XXXX
        if ((user.Contains('#') || user.Contains(' ')) && user.Length >= 6 &&
            int.TryParse(user.Substring(user.Length - 4), out var discriminator))
        {
            var userObj = await context.Client.GetUserAsync(user[..^5], discriminator.ToString());
            //Negative one is only passed in because it cannot convert to ulong; it will fail the TryParse and give a "Mention the player..." error.
            user = userObj != null ? userObj.Id.ToString() : (-1).ToString();
        }

        user = user.Trim(' ', '<', '>', '!', '@');
        if (!ulong.TryParse(user, out var userid))
        {
            await ErrorEmbed(
                context, 
                "Mention the player with this command to see their stats. Or if you want to be polite, try using their ID.");
            return;
        }

        await DisplayStats(context, userid);
    }

    private async Task DisplayStats(IUNObotCommandContext context, ulong user)
    {
        var (gamesJoined, gamesPlayed, gamesWon) = await _db.GetStats(user);
        var (defaultWidth, defaultHeight, defaultConnect) = await _db.GetDefaultBoardDimensions(user);
            
        var embed = new EmbedBuilder()
            .WithTitle($"User Info for {(await context.Client.GetUserAsync(user)).Username}")
            .AddField("**Game Stats**", 
                $"Games joined: {gamesJoined}\n" +
                $"Games played: {gamesPlayed}\n" +
                $"Games won: {gamesWon}", true)
            .AddField("**Game Config**", 
                $"Board width: {defaultWidth}\n" +
                $"Board height: {defaultHeight}\n" +
                $"Connect length: {defaultConnect}", true);

        await context.ReplyAsync(embed: Build(embed, context));
    }
}