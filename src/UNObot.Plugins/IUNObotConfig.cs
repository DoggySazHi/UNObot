namespace UNObot.Plugins
{
    public interface IUNObotConfig
    {
        public string Token { get; }
        public string Version { get; }
        public string SqlUser { get; }
        public string SqlServer { get; }
        public int SqlPort { get; }
        public string SqlPassword { get; }
        public bool UseSqlServer { get; }
        public string Build { get; }
        public string Commit { get; }
        public string MySqlConnection { get; }
        public string SqlConnection { get; }
    }
}