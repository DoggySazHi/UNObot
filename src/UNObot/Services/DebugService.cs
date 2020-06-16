namespace UNObot.Services
{
    public class DebugService
    {
        private static DebugService _instance;

        private DebugService()
        {
            /*
            if (!Program.version.Contains("Debug", StringComparison.OrdinalIgnoreCase))
                return;
                */
        }

        public static DebugService GetSingleton()
        {
            if (_instance == null)
                _instance = new DebugService();
            return _instance;
        }
    }
}