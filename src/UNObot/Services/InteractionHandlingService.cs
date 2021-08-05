using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace UNObot.Services
{
    public class InteractionHandlingService
    {
        public InteractionHandlingService(DiscordSocketClient discord)
        {
            discord.InteractionCreated += OnInteractionCreated;
        }

        private async Task OnInteractionCreated(SocketInteraction arg)
        {
            if (arg is SocketSlashCommand command)
            {
                await command.RespondAsync(
                    "Interaction received!" +
                    $"Command: {command.Data.Name}\n" +
                    $"Arguments: {command.Data.Options.Aggregate("", (a, b) => $"{a} ({b.Name}, {b.Value})")}",
                    ephemeral: true
                );
            }
            else if (arg is SocketMessageComponent interaction)
            {
                await interaction.RespondAsync(
                    $"{(interaction.Data.Values != null ? "Dropdown" : "Button")} interaction received!\n" +
                    $"Input Type: {interaction.Data.Type}\n" +
                    $"ID: {interaction.Data.CustomId}\n" +
                    $"Selected: {(interaction.Data.Values != null ? interaction.Data.Values.Aggregate("", (a, b) => $"{a} {b}") : "Nothing selectable")}",
                    ephemeral: true);
            }
        }
    }
}