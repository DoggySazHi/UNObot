using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace DuplicateDetector.Templates
{
    public interface IAIConfig
    {
        public string DBConnStr { get; }
    }
    
    public class AIConfig : IAIConfig
    {
        public string DBConnStr { get; set; }

        public AIConfig(string file)
        {
            var reader = new StreamReader(file);
            var text = reader.ReadToEnd();
            var json = JObject.Parse(text);
            DBConnStr = json["connStr"]?.ToString();
            if(DBConnStr == null)
                throw new InvalidOperationException("Could not find a valid DB connection string.");
        }
    }
}