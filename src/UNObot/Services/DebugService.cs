namespace UNObot.Services
{
    public class DebugService
    {
        private static DebugService Instance;
        public static DebugService GetSingleton()
        {
            if (Instance == null)
                Instance = new DebugService();
            return Instance;
        }

        private DebugService()
        {
            /*
            if (!Program.version.Contains("Debug", StringComparison.OrdinalIgnoreCase))
                return;
                */
        }
    }
}
