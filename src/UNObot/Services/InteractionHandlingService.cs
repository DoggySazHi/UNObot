using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace UNObot.Services
{
    public class InteractionHandlingService
    {
        public InteractionHandlingService(BaseSocketClient discord)
        {
            discord.InteractionCreated += OnInteractionCreated;
        }

        private async Task OnInteractionCreated(SocketInteraction arg)
        {
            if (arg is SocketSlashCommand command)
            {
                await command.RespondAsync(
                    $"Interaction received! {command.Data.Name}\n" +
                    $"{command.Data.Options.Aggregate("", (a, b) => $"{a} ({b.Name}, {b.Value})")}");
            }
        }
    }
}