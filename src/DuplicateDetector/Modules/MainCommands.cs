using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using DuplicateDetector.Services;

namespace DuplicateDetector.Modules
{
    public class MainCommands : ModuleBase<SocketCommandContext>
    {
        private readonly IndexerService _indexer;
        
        public MainCommands(IndexerService indexer)
        {
            _indexer = indexer;
        }
        
        [RequireOwner]
        [Command("dd", RunMode = RunMode.Async)]
        public async Task DDCore([Remainder] string command)
        {
            var args = command.Trim().ToLower().Split(" ");
            switch (args[0])
            {
                case "index":
                    await Index(args);
                    break;
                case "download":
                    await Download();
                    break;
            }
        }

        private async Task Index(IReadOnlyList<string> args)
        {
#pragma warning disable 4014
            if (args.Count > 2 &&
                ulong.TryParse(args[1], out var guild) &&
                ulong.TryParse(args[2], out var channel))
                _indexer.Index(guild, channel);
            else
                _indexer.Index(Context.Channel);
#pragma warning restore 4014
            await ReplyAsync("Started indexer!");
        }

        private async Task Download()
        {
#pragma warning disable 4014
            _indexer.Download();
#pragma warning restore 4014
            await ReplyAsync("Started downloader!");
        }
    }
}