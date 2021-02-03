using System.Collections.Generic;

namespace UNObot.Services
{
    public class SettingsManager
    {
        private static Dictionary<string, Setting> allSettings;
        public static IReadOnlyDictionary<string, Setting> AllSettings => allSettings;

        static SettingsManager()
        {
            allSettings = new Dictionary<string, Setting>();
        }

        internal SettingsManager() {}

        public static bool RegisterSettings(string identifier, Setting setting)
        {
            if (allSettings.ContainsKey(identifier))
                return false;
            allSettings.Add(identifier, setting);
            return true;
        }
    }
    
    public class Setting
    {
        public string Category { get; set; }
        public Dictionary<string, string> KeyValuePairs { get; }

        public Setting()
        {
            KeyValuePairs = new Dictionary<string, string>();
        }
    }
}