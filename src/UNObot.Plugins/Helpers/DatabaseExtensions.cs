using System;
using System.Collections.Generic;
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
    }
}