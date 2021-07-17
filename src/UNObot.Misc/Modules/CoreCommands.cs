using System.Threading.Tasks;
using Discord.Commands;
using UNObot.Misc.Services;

namespace UNObot.Misc.Modules
{
    public class CoreCommands : ModuleBase<SocketCommandContext>
    {
        private readonly DatabaseService _db;
        
        public CoreCommands(DatabaseService db)
        {
            _db = db;
        }
        
        [Command("migrateall", RunMode = RunMode.Async)]
        public async Task MigrateSettings()
        {
            var message = await ReplyAsync("Migrating...");
            await _db.Migrate();
            await message.ModifyAsync(o => o.Content = "Finished migration.");
        }
    }
}