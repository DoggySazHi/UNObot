using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using UNObot.Plugins;

namespace UNObot.Services
{
    public partial class CommandHandlingService
    {
        static HttpClient _client = new ();
        private bool _waitRegister;
        private bool _ready;

        private void InitializeHelpers()
        {
            _client.BaseAddress = new Uri("https://discord.com/api/v9/");
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            var token = _provider.GetRequiredService<IUNObotConfig>().Token;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", $"Bot {token}");
            _discord.Ready += OnReady;
        }

        private async Task OnReady()
        {
            _ready = true;
            if (_waitRegister)
                await RegisterCommands();
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

        private async Task BulkOverwriteGlobalCommands(SlashCommandCreationProperties[] commandProperties)
        {
            // Replacement for _discord.Rest.BulkOverwriteGlobalCommands.
            var uri = $"applications/{_discord.CurrentUser.Id}/commands";
            var list = commandProperties.Select(o => new SlashCommandJSON(o));
            var data = JsonConvert.SerializeObject(list, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            // Apparently StringContent implements HttpContent, which implements IDisposable.
            // However, PutAsync seems to dispose it.
            await _client.PutAsync(uri, new StringContent(data));
        }
        
        private async Task BulkOverwriteGuildCommands(SlashCommandCreationProperties[] commandProperties, ulong server)
        {
            // Replacement for _discord.Rest.BulkOverwriteGuildCommands.
            var uri = $"applications/{_discord.CurrentUser.Id}/guilds/${server}/commands";
            var list = commandProperties.Select(o => new SlashCommandJSON(o, server));
            var data = JsonConvert.SerializeObject(list, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            await _client.PutAsync(uri, new StringContent(data));
        }
    }
    
    internal class SlashCommandJSON {
        [JsonProperty("name")] public string Name { get; }
        [JsonProperty("type")] public int Type { get; } = 1; // For slash command
        [JsonProperty("description")] public string Description { get; }
        [JsonProperty("options")] public OptionJSON[] Options { get; }
        [JsonProperty("default_permission")] public bool? DefaultPermission { get; }
        [JsonProperty("guild_id")] public ulong? Guild { get; }

        public SlashCommandJSON(SlashCommandCreationProperties command)
        {
            Name = command.Name;
            Description = command.Description;
            DefaultPermission = command.DefaultPermission.ToNullable();
            if (command.Options.IsSpecified)
                Options = command.Options.Value.Select(o => new OptionJSON(o)).ToArray();
        }
        
        public SlashCommandJSON(SlashCommandCreationProperties command, ulong guild) : this(command)
        {
            Guild = guild;
        }
    }
    
    internal class OptionJSON {
        [JsonIgnore] private ApplicationCommandOptionType Type { get; }
        [JsonProperty("type")] public int TypeInt => (int) Type;
        [JsonProperty("name")] public string Name { get; }
        [JsonProperty("description")] public string Description { get; }
        [JsonProperty("required")] public bool? Required { get; }
        [JsonProperty("choices")] public ChoiceJSON[] Choices { get; }

        public OptionJSON(ApplicationCommandOptionProperties option)
        {
            Type = option.Type;
            Name = option.Name;
            Description = option.Description;
            Required = option.Required;
            Choices = option.Choices.Select(o => new ChoiceJSON(o)).ToArray();
        }
    }

    internal class ChoiceJSON
    {
        [JsonProperty("name")] public string Name { get; }
        [JsonProperty("value")] public object Value { get; }

        public ChoiceJSON(ApplicationCommandOptionChoiceProperties choice)
        {
            Name = choice.Name;
            Value = choice.Value;
        }
    }
}