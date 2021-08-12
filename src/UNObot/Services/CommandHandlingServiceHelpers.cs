using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Newtonsoft.Json;

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