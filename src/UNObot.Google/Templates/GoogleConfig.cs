using System.IO;
using Discord;
using Newtonsoft.Json;
using UNObot.Plugins;

namespace UNObot.Google.Templates;

public class GoogleConfig
{
    private const string DefaultVersion = "Unknown Version";
    private const string DefaultKey = "<Put Key Here>";
    private const string DefaultContext = "<Put Context Here>";

    [JsonProperty] public string Version { get; private set; }
    [JsonProperty] public string Key { get; private set; }
    [JsonProperty] public string Context { get; private set; }
    [JsonIgnore] private readonly ILogger _logger;
        
    public GoogleConfig()
    {
        Version = DefaultVersion;
        Key = DefaultKey;
        Context = DefaultContext;
    }
        
    public GoogleConfig(ILogger logger, string file = "config.json")
    {
        var json = File.ReadAllText(file);
        JsonConvert.PopulateObject(json, this);
        Write(file);

        _logger = logger;
    }

    public bool VerifyConfig()
    {
        var verificationFlag = true;

        if (string.IsNullOrWhiteSpace(Version))
        {
            Version = DefaultVersion;
        }

        if (Version == DefaultVersion)
        {
            _logger.Log(LogSeverity.Warning, "Google plugin has no version...");
        }
            
        if (Key == DefaultKey || string.IsNullOrWhiteSpace(Key))
        {
            _logger.Log(LogSeverity.Error, "Google API key has not been set!");
            verificationFlag = false;
        }
            
        if (Context == DefaultContext || string.IsNullOrWhiteSpace(Context))
        {
            _logger.Log(LogSeverity.Error, "Google Search context has not been set!");
            verificationFlag = false;
        }
            
        if (!verificationFlag)
            _logger.Log(LogSeverity.Error, "Configuration errors have been found. Refusing to load Google Search.");

        return verificationFlag;
    }
        
    public void Write(string path)
    {
        File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}