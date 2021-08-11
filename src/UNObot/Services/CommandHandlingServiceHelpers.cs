using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using UNObot.Plugins;

namespace UNObot.Services
{
    public partial class CommandHandlingService
    {
        static HttpClient _client = new ();

        private void InitializeHelpers()
        {
            _client.BaseAddress = new Uri("https://discord.com/api/v9/");
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            var token = _provider.GetRequiredService<IUNObotConfig>().Token;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", $"Bot {token}");
        }

        private async Task BulkOverwriteGlobalCommands(SlashCommandCreationProperties[] commandProperties)
        {
            var uri = $"applications/{_discord.CurrentUser.Id}/commands";
            await _client.PutAsJsonAsync(uri, commandProperties);
        }
        
        private async Task BulkOverwriteGuildCommands(SlashCommandCreationProperties[] commandProperties, ulong server)
        {
            var uri = $"applications/{_discord.CurrentUser.Id}/guilds/${server}/commands";
            await _client.PutAsJsonAsync(uri, commandProperties);
        }
    }
}