using System.IO;
using System.Reflection;

namespace UNObot.Plugins.Helpers
{
    public static class PluginHelper
    {
        public static string Directory()
            => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }
}