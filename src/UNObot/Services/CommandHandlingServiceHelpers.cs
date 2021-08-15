using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Newtonsoft.Json;
using UNObot.Plugins.Attributes;
using ParameterInfo = System.Reflection.ParameterInfo;

namespace UNObot.Services
{
    public partial class CommandHandlingService
    {
        private bool _waitRegister;
        private bool _ready;

        private void InitializeHelpers()
        {
            _discord.Ready += OnReady;
        }

        private async Task OnReady()
        {
            _ready = true;
            if (_waitRegister)
                await RegisterCommands();
        }
        
        private readonly Dictionary<ulong, List<SlashCommandCreationProperties>> _slashCommands = new();

        private void CreateCommand(MethodInfo method, HelpAttribute help, SlashCommandAttribute attribute, RequireOwnerAttribute owner)
        {
            if (attribute is not { RegisterSlashCommand: true }) return;
            var builder = new SlashCommandBuilder();

            if (builder.Name == null)
                builder.WithName(attribute.Text);
            builder.Name = builder.Name.ToLower();

            if (help != null && !string.IsNullOrWhiteSpace(help.HelpMsg))
                builder.WithDescription(help.HelpMsg);
            else
                builder.WithDescription("No description is provided about this command.");
            
            builder.WithDefaultPermission(owner == null || attribute.DefaultPermission);

            var parameters = method.GetParameters();

            builder.Options = parameters.Length == 0 ? null : parameters.Select(GenerateOption).ToList();
            
            var commands = !_slashCommands.ContainsKey(attribute.Guild) ?
                new List<SlashCommandCreationProperties>()
                : _slashCommands[attribute.Guild];
            
            commands.Add(builder.Build());
            
            // If it's new, it'll set it. Otherwise, it'll just place the same reference.
            _slashCommands[attribute.Guild] = commands;
        }

        private SlashCommandOptionBuilder GenerateOption(ParameterInfo o)
        {
            var optionAttribute = o.GetCustomAttribute<SlashCommandOptionAttribute>();
            
            var temp = new SlashCommandOptionBuilder().WithName(optionAttribute?.Name ?? o.Name?.ToLower() ?? "" + (char)(o.Position + 'a'))
                .WithDescription(optionAttribute?.Description ?? "A value.")
                .WithRequired(optionAttribute?.Required ?? !o.IsOptional);

            if (optionAttribute?.OptionType != null)
                temp.WithType(optionAttribute.OptionType);
            else {
                if (o.ParameterType == typeof(bool))
                    temp.WithType(ApplicationCommandOptionType.Boolean);
                else if (o.ParameterType == typeof(sbyte) || o.ParameterType == typeof(byte) ||
                         o.ParameterType == typeof(short) || o.ParameterType == typeof(ushort) ||
                         o.ParameterType == typeof(int) || o.ParameterType == typeof(uint) ||
                         o.ParameterType == typeof(long))
                    temp.WithType(ApplicationCommandOptionType.Integer);
                else if (IsDerivedFrom(o.ParameterType, typeof(IUser)))
                    temp.WithType(ApplicationCommandOptionType.User);
                else if (IsDerivedFrom(o.ParameterType, typeof(IRole)))
                    temp.WithType(ApplicationCommandOptionType.Role);
                else if (IsDerivedFrom(o.ParameterType, typeof(IChannel)))
                    temp.WithType(ApplicationCommandOptionType.Channel);
                else if (IsDerivedFrom(o.ParameterType, typeof(IMentionable)))
                    temp.WithType(ApplicationCommandOptionType.Mentionable);
                else
                    temp.WithType(ApplicationCommandOptionType.String);
            }

            if (optionAttribute?.ChoiceValues != null)
            {
                for (var i = 0; i < optionAttribute.Choices.Length; ++i)
                    if (optionAttribute.ChoiceValues[i] is int)
                        temp.AddChoice(optionAttribute.Choices[i].ToString(), (int) optionAttribute.ChoiceValues[i]);
                    else
                        temp.AddChoice(optionAttribute.Choices[i].ToString(), optionAttribute.ChoiceValues[i].ToString());
            }

            return temp;
        }

        private static bool IsDerivedFrom(Type derivedType, Type baseType)
        {
            return derivedType.IsSubclassOf(baseType) || derivedType == baseType;
        }
        
        public async Task RegisterCommands()
        {
            if (!_ready)
            {
                _waitRegister = true;
                return;
            }
            
            try
            {
                foreach (var guild in _slashCommands.Keys)
                {
                    if (guild == 0)
                    {
                        await _discord.Rest.BulkOverwriteGlobalCommands(_slashCommands[0].ToArray());
                    }
                    else
                    {
                        await _discord.Rest.BulkOverwriteGuildCommands(_slashCommands[guild].ToArray(), guild);
                    }
                }
            }
            catch (ApplicationCommandException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);
                _logger.Log(LogSeverity.Error, $"Error trying to create a slash command!\n{json}");
            }
            finally
            {
                _slashCommands.Clear();
            }
        }
    }
}