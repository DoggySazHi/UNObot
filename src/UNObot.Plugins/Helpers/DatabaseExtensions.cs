using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;

namespace UNObot.Plugins.Helpers
{
    public static class DatabaseExtensions
    {
        public static bool HasDBValue(this object item) => !DBNull.Value.Equals(item) && item != null;
        
        public static async Task<bool> HasDefaultChannel(IUNObotConfig config, ulong server)
        {
            await using var connection = config.GetConnection();

            const string commandText = "SELECT hasDefaultChannel FROM UNObot.Games WHERE server = @Server";
            
            try
            {
                return await connection.ExecuteScalarAsync<bool>(commandText, new { Server = server });
            }
            catch (DbException)
            {
                return false;
            }
        }
        
        public static async Task<ulong> GetDefaultChannel(IUNObotConfig config, ulong server)
        {
            await using var connection = config.GetConnection();

            const string commandText = "SELECT playChannel FROM UNObot.Games WHERE server = @Server";
            
            try
            {
                return await connection.ExecuteScalarAsync<ulong>(commandText, new { Server = server });
            }
            catch (DbException)
            {
                return 0;
            }
        }
        
        public static string GetConnectionString(this IUNObotConfig config)
        {
            //ha, damn the limited encodings.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding.GetEncoding("windows-1254");
            return config.UseSqlServer ? config.SqlConnection : config.MySqlConnection;
        }
        
        public static DbConnection GetConnection(this IUNObotConfig config)
        {
            if (config.UseSqlServer)
                return new SqlConnection(GetConnectionString(config));
            return new MySqlConnection(GetConnectionString(config));
        }

        public static string ConvertSql(IUNObotConfig config, string commandMySql)
        {
            var identity = commandMySql.Replace("LAST_INSERT_ID()", "SCOPE_IDENTITY()");
            var brackets = new Regex(@"`([^`]*)`", RegexOptions.Multiline).Replace(identity, @"[$1]");
            return brackets;
        }
    }
}