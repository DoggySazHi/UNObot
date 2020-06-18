using System;
using System.Collections.Generic;
using System.IO;
using Discord;
using Newtonsoft.Json;

namespace UNObot.Services
{
    public class LocalizationService
    {
        private static LocalizationService _instance;
        private static readonly string LocalizationFile = "translations_en.json";
        private Dictionary<string, string> _localizations;

        private LocalizationService()
        {
            if (File.Exists(LocalizationFile))
                try
                {
                    using var sr = new StreamReader(LocalizationFile);
                    var json = sr.ReadToEnd();
                    _localizations = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                }
                catch (Exception e)
                {
                    LoggerService.Log(LogSeverity.Warning, $"Failed to read localizations; generating a new one!\n{e}");
                    CreateNewLocalization();
                }
            else
                CreateNewLocalization();
        }

        public static LocalizationService GetSingleton()
        {
            return _instance ??= new LocalizationService();
        }

        private void CreateNewLocalization()
        {
            _localizations = new Dictionary<string, string>();
            using var sw = new StreamWriter(LocalizationFile);
            sw.Write(JsonConvert.SerializeObject(_localizations));
            LoggerService.Log(LogSeverity.Info, "Created empty localization file.");
        }
    }
}