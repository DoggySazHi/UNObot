using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace UNObot.Plugins.Helpers
{
    public static class DatabaseExtensions
    {
        public static bool HasDBValue(this object item) => !DBNull.Value.Equals(item) && item != null;
        
        public static async Task<bool> HasDefaultChannel(string connString, ulong server)
        {
            var yesOrNo = false;
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT hasDefaultChannel FROM UNObot.Games WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    yesOrNo = (byte) result == 1;
            }
            catch (MySqlException)
            {
                yesOrNo = false;
            }

            return yesOrNo;
        }
        
        public static async Task<ulong> GetDefaultChannel(string connString, ulong server)
        {
            ulong channel = 0;
            var parameters = new List<MySqlParameter>();

            const string commandText = "SELECT playChannel FROM UNObot.Games WHERE server = ?";

            var p1 = new MySqlParameter
            {
                Value = server
            };
            parameters.Add(p1);
            try
            {
                var result = await MySqlHelper.ExecuteScalarAsync(connString, commandText, parameters.ToArray());
                if (result.HasDBValue())
                    channel = (ulong) result;
            }
            catch (MySqlException)
            {
                
            }

            return channel;
        }
        
        public static string GetConnectionString(this IConfig config)
        {
            //ha, damn the limited encodings.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding.GetEncoding("windows-1254");
            return config.UseSqlServer ? config.SqlConnection : config.MySqlConnection;
        }

        public static string ConvertSql(IConfig config, string commandMySql)
        {
            var identity = commandMySql.Replace("LAST_INSERT_ID()", "SCOPE_IDENTITY()");
            var brackets = new Regex(@"`([^`]*)`", RegexOptions.Multiline).Replace(identity, @"[$1]");
            return brackets;
        }
    }
}