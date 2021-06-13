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
                return await connection.ExecuteScalarAsync<bool>(config.ConvertSql(commandText), new { Server = Convert.ToDecimal(server) });
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
                return await connection.ExecuteScalarAsync<ulong>(config.ConvertSql(commandText), new { Server = Convert.ToDecimal(server) });
            }
            catch (DbException)
            {
                return 0;
            }
        }
        
        public static string GetConnectionString(this IDBConfig config)
        {
            //ha, damn the limited encodings.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding.GetEncoding("windows-1254");
            return config.UseSqlServer ? config.SqlConnection : config.MySqlConnection;
        }
        
        public static DbConnection GetConnection(this IDBConfig config)
        {
            if (config.UseSqlServer)
                return new SqlConnection(GetConnectionString(config));
            return new MySqlConnection(GetConnectionString(config));
        }

        public static string ConvertSql(this IDBConfig config, string commandMySql)
        {
            if (!config.UseSqlServer) return commandMySql;
            
            var identity = commandMySql.Replace("LAST_INSERT_ID()", "SCOPE_IDENTITY()");
            var brackets = new Regex(@"`([^`]*)`", RegexOptions.Multiline).Replace(identity, @"[$1]");
            
            var dupKeyRegex = new Regex(
                @"INSERT INTO (\S*) \(([^,]*),\s([^)]*)\) VALUES[ ]?\(([^,]*),\s([^)]*)\) ON DUPLICATE KEY UPDATE [^;]*[;]?",
                RegexOptions.Multiline);
            var secondaries = dupKeyRegex.Match(brackets).Groups[3].Value.Split(",");
            for (var i = 0; i < secondaries.Length; ++i)
            {
                var param = secondaries[i].Trim();
                secondaries[i] = $"{param} = SOURCE.{param}";
            }
            var dupKeyParameters = string.Join(", ", secondaries);
            var dupKey = dupKeyRegex.Replace(brackets, 
                @"MERGE INTO $1 WITH (HOLDLOCK) AS Target USING (Values($5)) AS SOURCE($3) ON Target.$2 = $4 WHEN MATCHED THEN UPDATE SET <> WHEN NOT MATCHED THEN INSERT ($2, $3) VALUES ($4, $5)");
            if (dupKey.Contains("<>"))
            {
                dupKey = dupKey.Replace("<>", dupKeyParameters);
                if (!dupKey.EndsWith(';'))
                    dupKey += ";";
            }
            
        
            var insertIgnore = new Regex(
                    @"INSERT IGNORE INTO (\S*)[\s]*\((([^,)]*),?([^)]*))\) VALUES[ ]*\((([^,)]*),?([^)]*))\)[^;]*[;]?", RegexOptions.Multiline)
                .Replace(dupKey, @"MERGE INTO $1 WITH (HOLDLOCK) AS Target USING (Values($5)) AS SOURCE($2) ON Target.$3 = $6 WHEN NOT MATCHED THEN INSERT ($2) VALUES ($5);");

            return insertIgnore;
        }
    }
}