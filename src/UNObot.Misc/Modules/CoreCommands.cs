using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Misc.Services;
using UNObot.Plugins;
using UNObot.Plugins.Attributes;

namespace UNObot.Misc.Modules
{
    public class CoreCommands : UNObotModule<UNObotCommandContext>
    {
        private readonly DatabaseService _db;
        
        public CoreCommands(DatabaseService db)
        {
            _db = db;
        }
    }
}