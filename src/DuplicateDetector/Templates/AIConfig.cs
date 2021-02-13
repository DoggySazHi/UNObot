using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UNObot.Plugins.Helpers;

namespace DuplicateDetector.Templates
{
    public interface IAIConfig
    {
        public string DBConnStr { get; }
    }
    
    public class AIConfig : IAIConfig
    {
        public string DBConnStr { get; }

        public AIConfig()
        {
            var file = Path.Combine(PluginHelper.Directory(), "config.json");
            if (!File.Exists(file))
            {
                GenerateConfig(file);
                throw new FileNotFoundException("Created a new configuration file, please fill in these parameters.");
            }
            var reader = new StreamReader(file);
            var text = reader.ReadToEnd();
            var json = JObject.Parse(text);
            DBConnStr = json["connStr"]?.ToString();
            if (DBConnStr == null)
            {
                GenerateConfig(file);
                throw new InvalidOperationException("Could not find a valid DB connection string.");
            }
        }

        private void GenerateConfig(string file)
        {
            var obj = new JObject(
                new JProperty("connStr",
                    "server=127.0.0.1;user=DuplicateDetector;database=DuplicateDetector;port=3306;password=DBPassword")
            );
            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? string.Empty);
            using var sr = File.CreateText(file);
            sr.Write(obj);
        }

        public IAIConfig Build()
        {
            return this;
        }
    }
}