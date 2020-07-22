using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;

namespace UNObot.ServerQuery.Services
{
    public class DatabaseService
    {
        private readonly ILogger _logger;

        internal string ConnString { get; }
        
        public DatabaseService(ILogger logger, IConfiguration config)
        {
            _logger = logger;
            ConnString = config.GetConnectionString();
        }
        
        internal async Task<string> GetMinecraftUser(ulong user)
        {
            string username = null;
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT minecraftUsername FROM UNObot.Players WHERE userid = ?";

            var p1 = new MySqlParameter
            {
                Value = user
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(ConnString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    username = (string) result;
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return username;
        }
    }
}