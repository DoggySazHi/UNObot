using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Discord;
using UNObot.Plugins;

namespace UNObot.Services
{
    public class PluginLoadContext : AssemblyLoadContext
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
    
    public class PluginLoaderService
    {
        private readonly bool _init;

        public class PluginInfo
        {
            public string FileName { get; }
            public Assembly PluginAssembly { get; }
            public IPlugin Plugin { get; }
            public bool Loaded { get; set; }

            public PluginInfo(string fileName, Assembly pluginAssembly, IPlugin plugin, bool loaded = true)
            {
                FileName = fileName;
                PluginAssembly = pluginAssembly;
                Plugin = plugin;
                Loaded = loaded;
            }
        }

        private readonly List<PluginInfo> _plugins;

        public IReadOnlyList<PluginInfo> Plugins => _plugins;
        private readonly ILogger _logger;
        private readonly CommandHandlingService _commands;

        public PluginLoaderService(ILogger logger, CommandHandlingService commands)
        {
            _logger = logger;
            _commands = commands;
            
            if (_init)
            {
                _logger.Log(LogSeverity.Warning, "Attempted to re-init plugins!");
                return;
            }
            if (!Directory.Exists("plugins"))
                Directory.CreateDirectory("plugins");
            _plugins =
                Directory.EnumerateFiles("plugins").Select(path =>
                {
                    try
                    {
                        var pluginAssembly = LoadPlugin(path);
                        var (plugin, loaded) = CreatePlugin(pluginAssembly);
                        return new PluginInfo(path, pluginAssembly, plugin, loaded);
                    }
                    catch (Exception e)
                    {
                        _logger.Log(LogSeverity.Critical, $"Could not load {path}!", e);
                        return new PluginInfo(path, null, null, false);
                    }
                }).ToList();
            _plugins.RemoveAll(o => !o.Loaded);
            _logger.Log(LogSeverity.Info, $"Loaded {Plugins.Count} plugins!");
            _init = true;

            _commands.RegisterCommands().GetAwaiter().GetResult();
        }

        private Assembly LoadPlugin(string path)
        {
            var pluginLocation = Path.GetFullPath(path);
            _logger.Log(LogSeverity.Info, $"Loading plugin at {pluginLocation}");
            var loadContext = new PluginLoadContext(pluginLocation);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }

        private (IPlugin plugin, bool loaded) CreatePlugin(Assembly assembly)
        {
            IPlugin plugin = null;
            var loaded = false;

            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(IPlugin).IsAssignableFrom(type)) continue;
                if (Activator.CreateInstance(type) is not IPlugin result) continue;
                if (plugin != null) throw new InvalidOperationException("Read multiple IPlugins from one assembly!");
                plugin = result;
                var status = -1;
                Exception ex = null;
                try
                {
                    status = plugin.OnLoad(_logger);
                }
                catch (Exception e)
                {
                    ex = e;
                }
                if (status != 0)
                {
                    _logger.Log(LogSeverity.Error,
                        $"Plugin {plugin.GetName()} failed to load with error code {status}. Unloading.", ex);
                    ex = null;
                    try
                    {
                        status = plugin.OnUnload(_logger);
                    }
                    catch (Exception e)
                    {
                        ex = e;
                    }
                    if (status != 0)
                        _logger.Log(LogSeverity.Error,
                        $"Plugin {plugin.GetName()} failed to unload with error code {status}.", ex);
                }
                else
                {
                    _logger.Log(LogSeverity.Info, $"Loaded {plugin.GetName()}.");
                    loaded = true;
                }
            }
            
            if (loaded)
                Task.Run(async () =>
                {
                    try
                    {
                        var moduleCounter = (await _commands.AddModulesAsync(assembly, plugin?.Services)).Count();
                        _logger.Log(LogSeverity.Info, $"Found {moduleCounter} module{(moduleCounter == 1 ? "" : "s")} in {assembly.GetName().Name}.");
                    }
                    catch (Exception e)
                    {
                        _logger.Log(LogSeverity.Critical, $"Could not load {assembly.GetName().Name}!", e);
                        throw;
                    }
                });
            
            if(plugin == null)
                throw new MissingMemberException("Could not find plugin information!");
            return (plugin, loaded);
        }

        public PluginStatus LoadPluginByName(string name)
        {
            if (_plugins.Any(o => o.FileName.Contains(name, StringComparison.CurrentCultureIgnoreCase)))
                return PluginStatus.AlreadyLoaded;
            var availablePlugins =
                Directory.EnumerateFiles("plugins").Where(path =>
                    path.Trim()[(path.LastIndexOf(Path.DirectorySeparatorChar) + 1)..]
                        .Equals(name.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList();
            if (availablePlugins.Count == 0) return PluginStatus.NotFound;
            if (availablePlugins.Count > 1) return PluginStatus.Conflict;
            var pluginAssembly = LoadPlugin(availablePlugins[0]);
            try
            {
                var (plugin, loaded) = CreatePlugin(pluginAssembly);
                if (!loaded) return PluginStatus.Failed;
                _plugins.Add(new PluginInfo(availablePlugins[0], pluginAssembly, plugin));
                return PluginStatus.Success;
            }
            catch (Exception ex)
            {
                _logger.Log(LogSeverity.Error, "Exception trying to load plugin!", ex);
                return PluginStatus.Failed;
            }
        }

        public async Task<PluginStatus> UnloadPlugin(PluginInfo plugin)
        {
            if (!plugin.Loaded) return PluginStatus.AlreadyUnloaded;
            var unloadStatus = -1;
            Exception ex = null;
            try
            {
                await _commands.RemoveModulesAsync(plugin.PluginAssembly);
                unloadStatus = plugin.Plugin.OnUnload(_logger);
            }
            catch (Exception e)
            {
                ex = e;
            }
            plugin.Loaded = false;
            _plugins.Remove(plugin);
            await ReloadHelp();
            if (unloadStatus == 0)
                return PluginStatus.Success;
            _logger.Log(LogSeverity.Error,
                $"Plugin {plugin.Plugin.Name} failed to unload with error code {unloadStatus}.", ex);
            return PluginStatus.Failed;
        }

        private async Task ReloadHelp()
        {
            await _commands.ClearHelp();
            foreach (var plugin in _plugins)
                await _commands.LoadHelp(plugin.PluginAssembly, plugin.Plugin?.Services);
            await _commands.RegisterCommands();
        }
    }
}