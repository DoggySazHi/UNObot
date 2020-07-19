using System.Threading.Tasks;
using ConnectBot.Services;
using Discord.Commands;

namespace ConnectBot.Modules
{
    public class MainCommands : ModuleBase<SocketCommandContext>
    {
        private readonly EmbedService _embed;
        
        public MainCommands(EmbedService embed)
        {
            _embed = embed;
        }
        
        [Command("cbot")]
        public async Task ConnectBot([Remainder] string input)
        {
            var args = input.Trim().ToLower().Split(' ');
            switch (args[0])
            {
                case "join":
                    await _embed.JoinGame(Context);
                    break;
                case "leave":
                    await _embed.LeaveGame(Context);
                    break;
                case "start":
                    await _embed.StartGame(Context);
                    break;
                case "game":
                    await _embed.DisplayGame(Context);
                    break;
                case "drop":
                    await _embed.DropPiece(Context, args);
                    break;
                case "help":
                case "":
                    await _embed.DisplayHelp(Context);
                    break;
            }
        }
    }
}