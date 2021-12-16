namespace UNObot.Plugins;

public interface IDBConfig
{
    public string MySqlConnection { get; }
    public string SqlConnection { get; }
    public bool UseSqlServer { get; }
}