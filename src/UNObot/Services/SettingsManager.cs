using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace UNObot.Services
{
    public class SettingsManager
    {
        [JsonIgnore]
        private static readonly Dictionary<string, Setting> defaultSettings;
        [JsonIgnore]
        public static IReadOnlyDictionary<string, Setting> DefaultSettings => defaultSettings;
        [JsonProperty]
        private Dictionary<string, Setting> _currentSettings;
        [JsonIgnore]
        public IReadOnlyDictionary<string, Setting> CurrentSettings => _currentSettings;

        static SettingsManager()
        {
            defaultSettings = new Dictionary<string, Setting>();
        }

        public SettingsManager() : this(defaultSettings) {}

        [JsonConstructor]
        public SettingsManager(Dictionary<string, Setting> data)
        {
            _currentSettings = new Dictionary<string, Setting>(data);
        }
        
        /// <summary>
        /// Add a default setting to the service. This should be called by all services.
        /// </summary>
        /// <param name="identifier">A unique ID that a service owns.</param>
        /// <param name="setting">The group of settings associated with the identifier.</param>
        /// <returns>Whether the record was added successfully.</returns>
        public static bool RegisterSettings(string identifier, Setting setting)
        {
            if (defaultSettings.ContainsKey(identifier))
            {
                var originalSettings = defaultSettings[identifier];
                var success = false;
                foreach (var key in setting.Relation.Keys.Where(key => !originalSettings.Relation.ContainsKey(key)))
                {
                    originalSettings.UpdateSetting(key, setting.GetSetting(key));
                    success = true;
                }

                return success;
            }
            defaultSettings.Add(identifier, setting);
            return true;
        }

        public void UpdateSetting(string identifier, string key, object value)
            => _currentSettings[identifier].UpdateSetting(key, value);

        public T GetSetting<T>(string identifier, string key)
            => _currentSettings[identifier].GetSetting<T>(key);

    }

    public class Setting
    {
        [JsonProperty]
        public string Category { get; }
        [JsonProperty]
        private Dictionary<string, string> KeyValuePairs { get; }

        [JsonIgnore] public IReadOnlyDictionary<string, string> Relation => KeyValuePairs;

        public Setting(string category) : this(category, new Dictionary<string, string>()) {}
        
        [JsonConstructor]
        public Setting(string category, Dictionary<string, string> keyValuePairs)
        {
            Category = category;
            KeyValuePairs = keyValuePairs;
        }
        
        public void UpdateSetting(string key, object value)
            => KeyValuePairs[key] = JsonConvert.SerializeObject(JsonConvert.SerializeObject(value));

        public T GetSetting<T>(string key)
            => JsonConvert.DeserializeObject<T>(KeyValuePairs[key]);
        
        public object GetSetting(string key)
            => JsonConvert.DeserializeObject(KeyValuePairs[key]);
    }
}