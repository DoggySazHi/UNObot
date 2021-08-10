using System;
using Discord;
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
        public SlashCommandBuilder Builder { get; }
        public ulong Guild { get; }
        
        /// <summary>
        /// Create a slash command with the following parameters.
        /// </summary>
        /// <param name="text">The command's name.</param>
        /// <param name="ignoreExtraArgs">Whether a command match can be inexact.</param>
        /// <param name="registerSlashCommand">Whether to create the slash command or not.</param>
        /// <param name="builder">The <see cref="SlashCommandBuilder"/> that generates the slash command.</param>
        /// <param name="guild">The ID of the guild the command belongs to. A 0 will make it a global command.</param>
        public SlashCommandAttribute(string text, bool ignoreExtraArgs = false, bool registerSlashCommand = true, SlashCommandBuilder builder = null, ulong guild = 0) : base(text, ignoreExtraArgs)
        {
            RegisterSlashCommand = registerSlashCommand;
            Builder = builder;
            Guild = guild;
        }
    }
}