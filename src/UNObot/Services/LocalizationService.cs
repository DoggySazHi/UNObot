using System;
using System.Collections.Generic;
using System.IO;
using Discord;
using Newtonsoft.Json;

namespace UNObot.Services
{
    internal class LocalizationService
    {
        private static readonly string LocalizationFile = "translations_en.json";
        private Dictionary<string, string> _localizations;
        private LoggerService _logger;

        internal LocalizationService(LoggerService logger)
        {
            _logger = logger;
            
            if (File.Exists(LocalizationFile))
                try
                {
                    using var sr = new StreamReader(LocalizationFile);
                    var json = sr.ReadToEnd();
                    _localizations = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                }
                catch (Exception e)
                {
                    _logger.Log(LogSeverity.Warning, $"Failed to read localizations; generating a new one!\n{e}");
                    CreateNewLocalization();
                }
            else
                CreateNewLocalization();
        }

        private void CreateNewLocalization()
        {
            _localizations = new Dictionary<string, string>();
            using var sw = new StreamWriter(LocalizationFile);
            sw.Write(JsonConvert.SerializeObject(_localizations));
            _logger.Log(LogSeverity.Info, "Created empty localization file.");
        }
    }
}