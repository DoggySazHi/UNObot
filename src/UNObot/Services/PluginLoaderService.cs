using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using UNObot.Plugins;

namespace UNObot.Services
{
    internal class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }
    }
    
    public enum PluginStatus { Success = 0, Failed, NotFound, Conflict, AlreadyUnloaded, AlreadyLoaded }
    
    internal class PluginLoaderService
    {
        private readonly bool _init;

        internal class PluginInfo
        {
            internal string FileName { get; }
            internal Assembly PluginAssembly { get; }
            public IPlugin Plugin { get; }
            public bool Loaded { get; protected internal set; }

            public PluginInfo(string fileName, Assembly pluginAssembly, IPlugin plugin, bool loaded = true)
            {
                FileName = fileName;
                PluginAssembly = pluginAssembly;
                Plugin = plugin;
                Loaded = loaded;
            }
        }
        
        private List<PluginInfo> _plugins { get; }

        public IReadOnlyList<PluginInfo> Plugins => _plugins;

        private PluginLoaderService()
        {
            if (_init)
            {
                LoggerService.Log(LogSeverity.Warning, "Attempted to re-init plugins!");
                return;
            }
            if (!Directory.Exists("plugins"))
                Directory.CreateDirectory("plugins");
            _plugins =
                Directory.EnumerateFiles("plugins").Select(path =>
                {
                    LoggerService.Log(LogSeverity.Debug, path);
                    var pluginAssembly = LoadPlugin(path);
                    var (plugin, loaded) = CreatePlugin(pluginAssembly);
                    return new PluginInfo(path, pluginAssembly, plugin, loaded);
                }).ToList();
            _plugins.RemoveAll(o => !o.Loaded);
            LoggerService.Log(LogSeverity.Info, $"Loaded {Plugins.Count} plugins!");
            _init = true;
        }

        private static Assembly LoadPlugin(string path)
        {
            var pluginLocation = Path.GetFullPath(path);
            LoggerService.Log(LogSeverity.Info, $"Loading plugin at {pluginLocation}");
            var loadContext = new PluginLoadContext(pluginLocation);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }

        private static (IPlugin plugin, bool loaded) CreatePlugin(Assembly assembly)
        {
            IPlugin plugin = null;
            var loaded = false;

            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(IPlugin).IsAssignableFrom(type)) continue;
                if (!(Activator.CreateInstance(type) is IPlugin result)) continue;
                if (plugin != null) throw new InvalidOperationException("Read multiple IPlugins from one assembly!");
                plugin = result;
                var status = plugin.OnLoad();
                if (status != 0)
                {
                    LoggerService.Log(LogSeverity.Error,
                        $"Plugin {plugin.GetName()} failed to load with error code {status}. Unloading.");
                    var unloadStatus = plugin.OnUnload();
                    if (unloadStatus != 0)
                        LoggerService.Log(LogSeverity.Error,
                        $"Plugin {plugin.GetName()} failed to unload with error code {unloadStatus}.");
                }
                else
                {
                    LoggerService.Log(LogSeverity.Info, $"Loaded {plugin.GetName()}.");
                    loaded = true;
                }
            }
            var moduleCounter = Program.Services.GetRequiredService<CommandHandlingService>()
                .AddModulesAsync(assembly)
                .GetAwaiter()
                .GetResult()
                .Count();
            if(plugin == null)
                throw new MissingMemberException("Could not find plugin information!");
            LoggerService.Log(LogSeverity.Info, $"Found {moduleCounter} module{(moduleCounter == 1 ? "" : "s")} in {assembly.GetName().Name}.");
            return (plugin, loaded);
        }

        public PluginStatus LoadPluginByName(string name)
        {
            if (_plugins.Any(o => o.FileName.Contains(name, StringComparison.CurrentCultureIgnoreCase)))
                return PluginStatus.AlreadyLoaded;
            var availablePlugins =
                Directory.EnumerateFiles("plugins").Where(path =>
                    path.Trim().Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1)
                        .Equals(name.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList();
            if (availablePlugins.Count == 0) return PluginStatus.NotFound;
            if (availablePlugins.Count > 1) return PluginStatus.Conflict;
            var pluginAssembly = LoadPlugin(availablePlugins[0]);
            var (plugin, loaded) = CreatePlugin(pluginAssembly);
            if (!loaded) return PluginStatus.Failed;
            _plugins.Add(new PluginInfo(availablePlugins[0], pluginAssembly, plugin));
            return PluginStatus.Success;
        }

        public async Task<PluginStatus> UnloadPlugin(PluginInfo plugin)
        {
            if (!plugin.Loaded) return PluginStatus.AlreadyUnloaded;
            await Program.Services.GetRequiredService<CommandHandlingService>().RemoveModulesAsync(plugin.PluginAssembly);
            var unloadStatus = plugin.Plugin.OnUnload();
            plugin.Loaded = false;
            if (unloadStatus == 0)
            {
                _plugins.Remove(plugin);
                return PluginStatus.Success;
            }
            LoggerService.Log(LogSeverity.Error,
                $"Plugin {plugin.Plugin.Name} failed to unload with error code {unloadStatus}.");
            return PluginStatus.Failed;
        }
    }
}