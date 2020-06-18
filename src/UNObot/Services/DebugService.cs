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
            return _instance ??= new DebugService();
        }
    }
}