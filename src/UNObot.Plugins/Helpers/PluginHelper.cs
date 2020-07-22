using System.IO;
using System.Reflection;

namespace UNObot.Plugins.Helpers
{
    public static class PluginHelper
    {
        public static string Directory()
        {
            var folder = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location),
                Assembly.GetCallingAssembly().GetName().Name);
            if (!System.IO.Directory.Exists(folder))
                System.IO.Directory.CreateDirectory(folder);
            return folder;
        }
    }
}