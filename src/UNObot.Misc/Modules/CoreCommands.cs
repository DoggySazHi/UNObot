using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord.Commands;

namespace UNObot.Misc.Modules
{
    public class CoreCommands : ModuleBase<SocketCommandContext>
    {
        [Command("autovf", RunMode = RunMode.Async)]
        public async Task TestPerms()
        {
            var message = await ReplyAsync("Loading...");
            using var timer = new Timer {
                Interval = 5000,
                Enabled = true
            };

            var current = 0;
            var total = Context.Guild.Users.Count;

            timer.Elapsed += (_, _) =>
            {
                // ReSharper disable once AccessToModifiedClosure
                message.ModifyAsync(o => o.Content = $"Loading... {current}/{total}");
            };
            
            foreach (var user in Context.Guild.Users)
            {
                var role = Context.Guild.Roles.First(o =>
                    o.Name.Contains("delinquent", StringComparison.InvariantCultureIgnoreCase));
                await user.AddRoleAsync(role);
                current++;
            }

            timer.Stop();
            await message.ModifyAsync(o => o.Content = $"Finished updating {total} users!");
        }
    }
}