using System;
using Discord.Commands;

namespace UNObot.Plugins.Attributes
{
    /// <summary>
    /// Identifies the method as both a normal command and a slash command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SlashCommandAttribute : CommandAttribute
    {
        public bool RegisterSlashCommand { get; }
        
        /// <inheritdoc/>
        public SlashCommandAttribute(string text, bool ignoreExtraArgs = false, bool registerSlashCommand = true) : base(text, ignoreExtraArgs)
        {
            RegisterSlashCommand = registerSlashCommand;
        }
    }
}