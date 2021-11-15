using System;
using System.Data.SqlClient;
using System.IO;
using Discord;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using UNObot.Plugins;

namespace DuplicateDetector.Templates
{
    public class DuplicateDetectorConfig : IDBConfig
    {
        [JsonProperty] public string Version { get; private set; }
        [JsonProperty] public string SqlUser { get; private set; }
        [JsonProperty] public string SqlServer { get; private set; }
        [JsonProperty] public int SqlPort { get; private set; }
        [JsonProperty] public string SqlPassword { get; private set; }
        [JsonProperty] public bool UseSqlServer { get; private set; }

        [JsonIgnore]
        public string MySqlConnection
        {
            get
            {
                var output = new MySqlConnectionStringBuilder {
                    Server = SqlServer,
                    UserID = SqlUser,
                    Port = Convert.ToUInt32(SqlPort),
                    Password = SqlPassword,
                    Database = "DuplicateDetector"
                };
                return output.ConnectionString;
            }
        }

        [JsonIgnore]
        public string SqlConnection
        {
            get
            {
                var output = new SqlConnectionStringBuilder
                {
                    DataSource = $"{SqlServer},{SqlPort}",
                    UserID = SqlUser,
                    Password = SqlPassword,
                    InitialCatalog = "UNObot",
                    IntegratedSecurity = false
                };
                return output.ConnectionString;
            }
        }
        
        [JsonIgnore] private ILogger _logger;

        /// <summary>
        /// Generate a default config for UNObot.
        /// </summary>
        public DuplicateDetectorConfig()
        {
            GenerateDefaultConfig();
        }
        
        /// <summary>
        /// Load configuration from a file.
        /// </summary>
        /// <param name="logger">A logger to output any errors.</param>
        /// <param name="file">The file to start reading from.</param>
        public DuplicateDetectorConfig(ILogger logger, string file = "config.json")
        {
            GenerateDefaultConfig();
            
            var json = File.ReadAllText(file);
            JsonConvert.PopulateObject(json, this);
            Write(file);
            _logger = logger;
        }
        
        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public bool VerifyConfig()
        {
            var verificationFlag = true;
            var defaultConfig = new DuplicateDetectorConfig();
            if (SqlPort is not (>= 1 and <= 65535))
            {
                _logger.Log(LogSeverity.Error, "Configuration SQL port number out of allowed port range!");
                SqlPort = UseSqlServer ? 3306 : 1443;
            }
            
            if (Version == null || Version == defaultConfig.Version)
            {
                _logger.Log(LogSeverity.Warning, "Plugin version is unknown...");
                Version = defaultConfig.Version;
            }
            
            if (SqlUser == null || SqlServer == null || SqlPassword == null)
            {
                _logger.Log(LogSeverity.Critical, "\"SqlUser\", \"SqlServer\", or \"SqlPassword\" has not been set!");
                verificationFlag = false;
            }
            
            if (!verificationFlag)
                _logger.Log(LogSeverity.Error, "Configuration errors have been found. Refusing to load DuplicateDetector.");

            return verificationFlag;
        }

        private void GenerateDefaultConfig()
        {
            Version = "Unknown Version";
            SqlUser = "DuplicateDetector";
            SqlPassword = "SqlPassword";
            SqlPort = 3306;
            SqlServer = "localhost";
            UseSqlServer = false;
        }
    }
}