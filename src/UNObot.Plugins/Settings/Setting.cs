using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UNObot.Plugins.Settings;

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
        => (T) Convert.ChangeType(KeyValuePairs[key], typeof(T));
        
    public ISetting GetSetting(string key)
        => KeyValuePairs[key];
}