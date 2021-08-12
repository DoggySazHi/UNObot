using System;
using System.Net.Http;
using System.Net.Http.Headers;
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
    }
}