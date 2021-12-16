namespace UNObot.Plugins;

public interface IUNObotConfig : IDBConfig
{
    public string Token { get; }
    public string Version { get; }
    public string SqlUser { get; }
    public string SqlServer { get; }
    public int SqlPort { get; }
    public string SqlPassword { get; }
    public string Build { get; }
    public string Commit { get; }
}