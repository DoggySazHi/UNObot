using System;
using System.Collections.Generic;
using System.IO;
using Discord;
using Newtonsoft.Json;

namespace UNObot.Services
{
    public class LocalizationService
    {
        private static LocalizationService Instance;
        private static readonly string LocalizationFile = "translations_en.json";
        private Dictionary<string, string> Localizations;

        public static LocalizationService GetSingleton()
        {
            if (Instance == null)
                Instance = new LocalizationService();
            return Instance;
        }

        private LocalizationService()
        {
            if (File.Exists(LocalizationFile))
            {
                try
                {
                    using StreamReader sr = new StreamReader(LocalizationFile);
                    var JSON = sr.ReadToEnd();
                    Localizations = JsonConvert.DeserializeObject<Dictionary<string, string>>(JSON);
                }
                catch (Exception e)
                {
                    LoggerService.Log(LogSeverity.Warning, $"Failed to read localizations; generating a new one!\n{e}");
                    CreateNewLocalization();
                }
            }
            else
                CreateNewLocalization();
        }

        private void CreateNewLocalization()
        {
            Localizations = new Dictionary<string, string>();
            using StreamWriter sw = new StreamWriter(LocalizationFile);
            sw.Write(JsonConvert.SerializeObject(Localizations));
            LoggerService.Log(LogSeverity.Info, "Created empty localization file.");
        }
    }
}