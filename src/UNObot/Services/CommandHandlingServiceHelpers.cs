using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
        
        private readonly Dictionary<ulong, List<SlashCommandBuilder>> _slashCommands = new();

        private void CreateCommand(MethodInfo method, HelpAttribute help, SlashCommandAttribute attribute, RequireOwnerAttribute owner)
        {
            if (attribute is not { RegisterSlashCommand: true }) return;

            SlashCommandBuilder builder;
            if (_slashCommands.ContainsKey(attribute.Guild))
                builder = _slashCommands[attribute.Guild]
                              .Find(o => o.Name.Equals(attribute.Text, StringComparison.OrdinalIgnoreCase)) ??
                          new SlashCommandBuilder();
            else
                builder = new SlashCommandBuilder();

            var subgroup = method.GetCustomAttribute<SlashCommandGroupAttribute>() ?? method.DeclaringType?.GetCustomAttribute<SlashCommandGroupAttribute>();
            var subcommand = method.GetCustomAttribute<SlashSubcommandAttribute>();

            if (subgroup != null && subcommand == null)
                throw new InvalidOperationException(
                    "A SlashSubcommandAttribute must be applied to a method if a SlashCommandGroupAttribute is added!\n" +
                    $"Command: {attribute.Text} Subcommand Group: {subgroup.Name}");
            
            // Fill in base command properties
            if (builder.Name == null)
                builder.WithName(attribute.Text);
            builder.Name = builder.Name.ToLower();

            if (help != null && !string.IsNullOrWhiteSpace(help.HelpMsg))
                builder.WithDescription(help.HelpMsg);
            else
                builder.WithDescription("No description is provided about this command.");

            builder.WithDefaultPermission(owner == null || attribute.DefaultPermission);
            
            // Create group and subcommand
            if (subgroup != null)
            {
                var groupName = subgroup.Name.ToLower();
                var group = builder.Options.Find(o => o.Name.Equals(groupName));
                if (group == null)
                {
                    group = new SlashCommandOptionBuilder()
                        .WithType(ApplicationCommandOptionType.SubCommandGroup)
                        .WithName(groupName.ToLower())
                        .WithDescription(subgroup.Description);
                }
                group.AddOption(BuildSubcommand(method, subcommand));
            }
            // Just create subcommand
            else if (subcommand != null)
            {
                builder.AddOption(BuildSubcommand(method, subcommand));
            }
            // Attach directly to base command
            else
            {
                var parameters = method.GetParameters();

                builder.Options = parameters.Length == 0 ? null : parameters.Select(GenerateOption).ToList();
            }

            var commands = !_slashCommands.ContainsKey(attribute.Guild) ?
                new List<SlashCommandBuilder>()
                : _slashCommands[attribute.Guild];
            
            if (!commands.Contains(builder))
                commands.Add(builder);
            
            // If it's new, it'll set it. Otherwise, it'll just place the same reference.
            _slashCommands[attribute.Guild] = commands;
        }

        private SlashCommandOptionBuilder BuildSubcommand(MethodInfo method, SlashSubcommandAttribute subcommand)
        {
            var command = new SlashCommandOptionBuilder()
                .WithName(subcommand.Name.ToLower())
                .WithDescription(subcommand.Description)
                .WithType(ApplicationCommandOptionType.SubCommand);

            var parameters = method.GetParameters();

            command.Options = parameters.Length == 0 ? null : parameters.Select(GenerateOption).ToList();
            return command;
        }

        private SlashCommandOptionBuilder GenerateOption(ParameterInfo o)
        {
            var optionAttribute = o.GetCustomAttribute<SlashCommandOptionAttribute>();
            
            // TODO so, Discord.NET is broken as options can range from 1-32 chars, however their RegEx requires 3-32 chars.
            // So I use reflection to bypass it.
            
            var builder = CreateBuilder(optionAttribute?.Name ?? o.Name?.ToLower() ?? "" + (char)(o.Position + 'a'))
                .WithDescription(optionAttribute?.Description ?? "A value.")
                .WithRequired(optionAttribute?.Required ?? !o.IsOptional);
            
            if (optionAttribute?.OptionType != null)
                builder.WithType(optionAttribute.OptionType);
            else {
                if (o.ParameterType == typeof(bool))
                    builder.WithType(ApplicationCommandOptionType.Boolean);
                else if (o.ParameterType == typeof(sbyte) || o.ParameterType == typeof(byte) ||
                         o.ParameterType == typeof(short) || o.ParameterType == typeof(ushort) ||
                         o.ParameterType == typeof(int) || o.ParameterType == typeof(uint) ||
                         o.ParameterType == typeof(long))
                    builder.WithType(ApplicationCommandOptionType.Integer);
                else if (o.ParameterType == typeof(float) || o.ParameterType == typeof(double) ||
                         o.ParameterType == typeof(decimal))
                    builder.WithType(ApplicationCommandOptionType.Number);
                else if (IsDerivedFrom(o.ParameterType, typeof(IUser)))
                    builder.WithType(ApplicationCommandOptionType.User);
                else if (IsDerivedFrom(o.ParameterType, typeof(IRole)))
                    builder.WithType(ApplicationCommandOptionType.Role);
                else if (IsDerivedFrom(o.ParameterType, typeof(IChannel)))
                    builder.WithType(ApplicationCommandOptionType.Channel);
                else if (IsDerivedFrom(o.ParameterType, typeof(IMentionable)))
                    builder.WithType(ApplicationCommandOptionType.Mentionable);
                else
                    builder.WithType(ApplicationCommandOptionType.String);
            }

            if (optionAttribute?.ChoiceValues != null)
            {
                for (var i = 0; i < optionAttribute.Choices.Length; ++i)
                    if (optionAttribute.ChoiceValues[i] is int)
                        builder.AddChoice(optionAttribute.Choices[i].ToString(), (int) optionAttribute.ChoiceValues[i]);
                    else
                        builder.AddChoice(optionAttribute.Choices[i].ToString(), optionAttribute.ChoiceValues[i].ToString());
            }

            return builder;
        }

        private static SlashCommandOptionBuilder CreateBuilder(string name)
        {
            var output = new SlashCommandOptionBuilder();
            
            if (name?.Length > SlashCommandBuilder.MaxNameLength)
                throw new ArgumentException("Name length must be less than or equal to 32");
            if (name?.Length < 1)
                throw new ArgumentException("Name length must at least 1 characters in length");
            if (name != null)
            {
                name = name.ToLower();
                // Bug origin: https://github.com/Discord-Net-Labs/Discord.Net-Labs/blob/eb271a9ccc59b45f6a70556ef065ff2fbc531fce/src/Discord.Net.Core/Entities/Interactions/SlashCommandBuilder.cs#L304
                if (!Regex.IsMatch(name, @"^[\w-]{1,32}$"))
                    throw new ArgumentException("Option name cannot contain any special characters or whitespaces!");
            }

            typeof(SlashCommandOptionBuilder).GetField("_name", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(output, name);
            return output;
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
                    // Mainly to avoid warnings from Rider, using LINQ to do it.
                    var commands = new ApplicationCommandProperties[_slashCommands[guild].Count];
                    for (var i = 0; i < _slashCommands[guild].Count; ++i)
                        commands[i] = _slashCommands[guild][i].Build();

                    if (guild == 0)
                    {
                        await _discord.Rest.BulkOverwriteGlobalCommands(commands);
                    }
                    else
                    {
                        await _discord.Rest.BulkOverwriteGuildCommands(commands, guild);
                    }
                }
            }
            catch (ApplicationCommandException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);
                _logger.Log(LogSeverity.Error, $"Error trying to create a slash command!\n{json}");
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);
                _logger.Log(LogSeverity.Error, $"Permissions were not granted for a server!\n{json}");
            }
            finally
            {
                _slashCommands.Clear();
            }
        }
    }
}