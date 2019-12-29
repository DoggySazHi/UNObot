namespace UNObot.Services
{
    public class LocalizationService
    {
        private static LocalizationService Instance;

        public static LocalizationService GetSingleton()
        {
            if (Instance == null)
                Instance = new LocalizationService();
            return Instance;
        }
    }
}
