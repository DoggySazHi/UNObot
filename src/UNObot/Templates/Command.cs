using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UNObot.Templates
{
    internal class Command
    {
        [JsonConstructor]
        internal Command(string commandName, List<string> aliases, List<string> usages, string help, bool active,
            string version)
        {
            CommandName = commandName;
            Aliases = aliases;
            Usages = usages;
            Help = help;
            Active = active;
            Version = version;
        }

        [JsonConstructor]
        internal Command(string commandName, List<string> aliases, List<string> usages, string help, bool active,
            string version, bool disableDMs) : this(commandName, aliases, usages, help, active, version)
        {
            DisableDMs = disableDMs;
        }

        internal string CommandName { get; set; }
        internal List<string> Usages { get; set; }
        internal List<string> Aliases { get; set; }
        internal string Help { get; set; }
        internal bool Active { get; set; }
        internal string Version { get; set; }
        internal bool DisableDMs { get; set; }
        internal IServiceProvider Services { get; set; }
        internal bool Original { get; set; }
    }
}