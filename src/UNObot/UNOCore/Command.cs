using System.Collections.Generic;
using Newtonsoft.Json;

namespace UNObot.UNOCore
{
    internal class Command
    {
        [JsonConstructor]
        public Command(string commandName, List<string> aliases, List<string> usages, string help, bool active,
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
        public Command(string commandName, List<string> aliases, List<string> usages, string help, bool active,
            string version, bool disableDMs) : this(commandName, aliases, usages, help, active, version)
        {
            DisableDMs = disableDMs;
        }

        public string CommandName { get; set; }
        public List<string> Usages { get; set; }
        public List<string> Aliases { get; set; }
        public string Help { get; set; }
        public bool Active { get; set; }
        public string Version { get; set; }
        public bool DisableDMs { get; set; }
    }
}