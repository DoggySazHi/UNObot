using System;
using System.Diagnostics.CodeAnalysis;
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
        /// <summary>
        /// Whether to create the slash command or not.
        /// </summary>
        public bool RegisterSlashCommand { get; set; }
        
        /// <inheritdoc cref="SlashCommandBuilder.DefaultPermission"/>
        public bool DefaultPermission { get; set; } = true;
        
        /// <summary>
        /// The ID of the guild the command belongs to. A 0 will make it a global command.
        /// </summary>
        public ulong Guild { get; set; }

        /// <summary>
        /// Create a slash command with the following parameters.
        /// </summary>
        /// <param name="text">The command's name.</param>
        /// <param name="ignoreExtraArgs">Whether a command match can be inexact.</param>
        public SlashCommandAttribute(
            string text,
            bool ignoreExtraArgs = false) : base(text, ignoreExtraArgs)
        {
            
        }
    }
    
    /// <summary>
    /// Provide information about a parameter, used in combination with <see cref="SlashCommandAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SlashCommandOptionAttribute : Attribute
    {
        public string Parameter { get; }
        public string Name { get; }
        public string Description { get; }
        public ApplicationCommandOptionType OptionType { get; set; } = ApplicationCommandOptionType.String;
        
        /// <inheritdoc cref="SlashCommandBuilder.DefaultPermission"/>
        public bool Required { get; set; } = true;
        
        private object[] Choices { get; }
        private object[] ChoiceValues { get; }

        public SlashCommandOptionAttribute(
            [NotNull] string parameter,
            [NotNull] string name,
            [NotNull] string description,
            object[] choices = null, object[] values = null)
        {
            Parameter = parameter;
            Name = name;
            Description = description;
            Choices = choices;
            ChoiceValues = values;

            if (Choices != null && ChoiceValues == null)
                ChoiceValues = Choices;
            else if (Choices != null)
            {
                if (Choices.Length != ChoiceValues.Length)
                    throw new InvalidOperationException("Choices and value count do not match!");
                int intChoices = 0, stringChoices = 0;
                foreach (var choice in ChoiceValues)
                    switch (choice)
                    {
                        case int:
                            ++intChoices;
                            break;
                        case string:
                            ++stringChoices;
                            break;
                        default:
                            throw new InvalidOperationException("Values must be either of type int or string!");
                    }
                if (intChoices != Choices.Length && stringChoices != Choices.Length)
                    throw new InvalidOperationException("Choices values must all be the same type!");
                OptionType = intChoices == 0 ? ApplicationCommandOptionType.String : ApplicationCommandOptionType.Integer;
            }
        }
    }
}