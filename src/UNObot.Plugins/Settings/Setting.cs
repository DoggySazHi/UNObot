using System.Collections.Generic;
using Newtonsoft.Json;

namespace UNObot.Plugins.Settings
{
    public class Setting
    {
        [JsonProperty]
        public string Category { get; }
        [JsonProperty]
        private Dictionary<string, ISetting> KeyValuePairs { get; }

        [JsonIgnore] public IReadOnlyDictionary<string, ISetting> Relation => KeyValuePairs;

        public Setting(string category) : this(category, new Dictionary<string, ISetting>()) {}
        
        [JsonConstructor]
        public Setting(string category, Dictionary<string, ISetting> keyValuePairs)
        {
            Category = category;
            KeyValuePairs = keyValuePairs;
        }
        
        public void UpdateSetting(string key, ISetting value)
            => KeyValuePairs[key] = value;

        public T GetSetting<T>(string key)
            => JsonConvert.DeserializeObject<T>(KeyValuePairs[key].JSON);
        
        public object GetSetting(string key)
            => JsonConvert.DeserializeObject(KeyValuePairs[key].JSON);
    }
}