using System;
using System.Data.SqlClient;
using System.IO;
using Discord;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UNObot.Plugins;

namespace UNObot.Templates
{
    public class UNObotConfig : IUNObotConfig
    {
        [JsonProperty] public string Token { get; private set; }
        [JsonProperty] public string Version { get; private set; }
        [JsonProperty] public string SqlUser { get; private set; }
        [JsonProperty] public string SqlServer { get; private set; }
        [JsonProperty] public int SqlPort { get; private set; }
        [JsonProperty] public string SqlPassword { get; private set; }
        [JsonProperty] public bool UseSqlServer { get; private set; }
        [JsonIgnore] public string Build { get; }
        [JsonIgnore] public string Commit { get; }
        
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
                    Database = "UNObot"
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
                    InitialCatalog = "UNObot"
                };
                return output.ConnectionString;
            }
        }
        
        [JsonIgnore] public JObject RawData { get; }

        [JsonIgnore] private ILogger _logger;

        /// <summary>
        /// Generate a default config for UNObot.
        /// </summary>
        public UNObotConfig()
        {
            GenerateDefaultConfig();
        }
        
        /// <summary>
        /// Load configuration from a file.
        /// </summary>
        /// <param name="logger">A logger to output any errors.</param>
        /// <param name="file">The file to start reading from.</param>
        public UNObotConfig(ILogger logger, string file = "config.json")
        {
            GenerateDefaultConfig();
            
            var json = File.ReadAllText(file);
            JsonConvert.PopulateObject(json, this);
            RawData = JObject.Parse(json);
            File.WriteAllText(file, JsonConvert.SerializeObject(this, Formatting.Indented));
            _logger = logger;

            try
            {
                var buildInfo = JObject.Parse(File.ReadAllText("build.json"));
                Build = buildInfo["build"]?.ToString();
                Commit = buildInfo["commit"]?.ToString();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public bool VerifyConfig()
        {
            var verificationFlag = true;
            var defaultConfig = new UNObotConfig();
            if (SqlPort is not (>= 1 and <= 65535))
            {
                _logger.Log(LogSeverity.Error, "Configuration SQL port number out of range!");
                SqlPort = UseSqlServer ? 3306 : 1443;
            }

            if (Token == null || Token == defaultConfig.Token)
            {
                _logger.Log(LogSeverity.Critical, "Bot token has not been set, or is invalid!");
                verificationFlag = false;
            }
            
            if (Version == null || Version == defaultConfig.Version)
            {
                _logger.Log(LogSeverity.Warning, "Bot version is unknown...");
                Version = defaultConfig.Version;
                // We can leave the bot running, so no need to invalidate the verification flag.
            }
            
            if (SqlUser == null || SqlServer == null || SqlPassword == null)
            {
                _logger.Log(LogSeverity.Critical, "\"SqlUser\", \"SqlServer\", or \"SqlPassword\" has not been set!");
                verificationFlag = false;
            }
            
            if (Build == null || Commit == null)
                _logger.Log(LogSeverity.Warning, "The build information seems to be missing. Either this is a debug copy, or the build info has been deleted.");
            
            if (!verificationFlag)
                _logger.Log(LogSeverity.Error, "Configuration errors have been found. Exiting.");

            return verificationFlag;
        }

        private void GenerateDefaultConfig()
        {
            Token = "YourBotToken";
            Version = "Unknown Version";
            SqlUser = "UNObot";
            SqlPassword = "SqlPassword";
            SqlPort = 3306;
            SqlServer = "localhost";
            UseSqlServer = false;
        }
    }
}