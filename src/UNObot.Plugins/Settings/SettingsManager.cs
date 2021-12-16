﻿using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace UNObot.Plugins.Settings;

public class SettingsManager
{
    public delegate void RefreshSetting(SettingsManager manager, ulong server);
    public static event RefreshSetting OnQuery;
    private static readonly object _settingsLock = new();
        
    [JsonIgnore]
    private static readonly Dictionary<string, Setting> defaultSettings;
    [JsonIgnore]
    public static IReadOnlyDictionary<string, Setting> DefaultSettings => defaultSettings;
    [JsonProperty]
    public ulong Server { get; }
    [JsonProperty]
    private Dictionary<string, Setting> _currentSettings;
    [JsonIgnore]
    public IReadOnlyDictionary<string, Setting> CurrentSettings => _currentSettings;

    static SettingsManager()
    {
        defaultSettings = new Dictionary<string, Setting>();
    }

    public SettingsManager(ulong server) : this(server, defaultSettings) {}

    [JsonConstructor]
    public SettingsManager(ulong server, Dictionary<string, Setting> data)
    {
        Server = server;
        _currentSettings = data == null ? new Dictionary<string, Setting>(defaultSettings) : new Dictionary<string, Setting>(data);
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

    public void UpdateSetting(string identifier, string key, ISetting value)
    {
        lock (_settingsLock)
        {
            _currentSettings[identifier].UpdateSetting(key, value);
        }
    }

    public T GetSetting<T>(string identifier, string key)
    {
        if (OnQuery != null)
        {
            foreach (var del in OnQuery.GetInvocationList())
            {
                var method = (RefreshSetting) del;
                method.Invoke(this, Server);
            }
        }

        return _currentSettings[identifier].GetSetting<T>(key);
    }
}