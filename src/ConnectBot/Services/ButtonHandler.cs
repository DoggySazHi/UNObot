using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using UNObot.Plugins;

namespace ConnectBot.Services
{
    public class ButtonHandler
    {
        private static bool _init;
        private readonly DiscordSocketClient _client;
        private readonly DatabaseService _db;
        public DropPiece Callback;

        public delegate Task DropPiece(IUNObotCommandContext context, string[] args);

        public ButtonHandler(DiscordSocketClient client, DatabaseService db)
        {
            _client = client;
            _db = db;
            if (_init) return;
            _init = true;
            client.InteractionCreated += InteractionCreated;
        }

        private async Task InteractionCreated(SocketInteraction arg)
        {
            if (arg is not SocketMessageComponent interaction) return;
            var message = await interaction.GetOriginalResponseAsync();
            if (message.Author.Id != _client.CurrentUser.Id) return;
            if (interaction.Channel is not ITextChannel) return;
            var context =
                new UNObotCommandContext(_client, interaction);
            var game = await _db.GetGame(context.Guild.Id);
            if (Callback != null && game.Queue.GameStarted() && game.Queue.CurrentPlayer().Player == context.User.Id)
            {
                _ = Callback(context, new[] {"", interaction.Data.Values.FirstOrDefault()}).ConfigureAwait(false);
            }
        }
        
        public async Task AddNumbers(IUserMessage message, int columns)
        {
            columns = Math.Min(columns, SlashCommandOptionBuilder.MaxChoiceCount);
            await message.ModifyAsync(o => o.Components = new ComponentBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithPlaceholder("Pick a column to drop to.")
                    .WithCustomId("unobot")
                    .WithOptions(Enumerable.Range(1, columns)
                        .Select(p => new SelectMenuOptionBuilder("Column " + p, $"{p - 1}"))
                        .ToList()
                    ))
                .Build());
        }
    }
}