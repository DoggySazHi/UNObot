using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Discord;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;

namespace UNObot.ServerQuery.Services
{
    public class DatabaseService
    {
        private readonly ILogger _logger;
        private readonly IUNObotConfig _config;
        
        public DatabaseService(ILogger logger, IUNObotConfig config)
        {
            _logger = logger;
            _config = config;
        }
        
        public async Task<string> GetMinecraftUser(ulong user)
        {
            await using var db = _config.GetConnection();

            const string commandText = "SELECT minecraftUsername FROM ServerQuery.Player WHERE userid = @UserID";

            try
            {
                return await db.ExecuteScalarAsync<string>(_config.ConvertSql(commandText), new { UserID = Convert.ToDecimal(user) } );
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
                return null;
            }
        }

        public async Task<bool> IsInternalHostname(string hostname)
        {
            await using var db = _config.GetConnection();

            const string commandText = "SELECT COUNT(1) FROM ServerQuery.InternalHostname WHERE hostname = @Hostname";

            try
            {
                return await db.ExecuteScalarAsync<bool>(_config.ConvertSql(commandText), new { Hostname = hostname } );
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
                return false;
            }
        }
        
        public async Task<string> GetMinecraftUsername(ulong user)
        {
            await using var db = _config.GetConnection();

            const string commandText = "SELECT minecraft_username FROM ServerQuery.Player WHERE userid = @ID";

            try
            {
                return await db.ExecuteScalarAsync<string>(_config.ConvertSql(commandText), new { ID = Convert.ToDecimal(user) } );
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
                return null;
            }
        }

        public async Task<bool> HasRCONPrivilege(ulong user)
        {
            await using var db = _config.GetConnection();

            const string commandText = "SELECT rcon_privilege FROM ServerQuery.Player WHERE userid = @ID";

            try
            {
                return await db.ExecuteScalarAsync<bool>(_config.ConvertSql(commandText), new { ID = Convert.ToDecimal(user) } );
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
                return false;
            }
        }
        
        public class RCONServer
        {
            public string Server { get; init; }
            public ushort RCONPort { get; init; }
            public string Password { get; init; }
        }
        
        public async Task<RCONServer> GetRCONServer(ushort port)
        {
            await using var db = _config.GetConnection();

            const string commandText = "SELECT ip, port, password FROM ServerQuery.RCONServer WHERE port = @Port";

            try
            {
                var data = await db.QueryFirstOrDefaultAsync(_config.ConvertSql(commandText), new { Port = Convert.ToInt32(port) } );
                return new RCONServer
                    {Server = data.ip, RCONPort = Convert.ToUInt16(data.port), Password = data.password};
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
                return null;
            }
        }
    }
}