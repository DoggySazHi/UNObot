using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using UNObot.Plugins.Attributes;
using UNObot.Services;

namespace UNObot.Modules
{
    public class PluginConfigCommands : ModuleBase<SocketCommandContext>
    {
        private readonly PluginLoaderService _pluginService;

        internal PluginConfigCommands(PluginLoaderService pluginService)
        {
            _pluginService = pluginService;
        }
        
        [Command("plugins", RunMode = RunMode.Async), Alias("pl")]
        [Help(new[] {".plugins"}, "Get all plugins loaded in the bot.", true, "UNObot 4.1.8")]
        internal async Task Plugin()
        {
            var plugins = _pluginService.Plugins;
            if (plugins.Count > 0)
            {
                var response = plugins.Aggregate($"Plugins loaded ({plugins.Count}): \n" + "```",
                    (current, pluginInfo) =>
                        current + $"- {pluginInfo.Plugin.GetName()}: {pluginInfo.Plugin.Description}\n"
                                + $"  By {pluginInfo.Plugin.Author}\n");
                response += "```";
                await ReplyAsync(response);
                return;
            }

            await ReplyAsync("There are no plugins loaded.");
        }
        
        [Command("plugins", RunMode = RunMode.Async), Alias("pl")]
        [Help(new[] {".plugins (mode)"}, "Get all plugins loaded in the bot.", true, "UNObot 4.1.8")]
        internal async Task Plugin(string mode, [Remainder] string plugin)
        {
            switch (mode.Trim().ToLower())
            {
                case "reload":
                    await ReplyAsync("This is not implemented yet.");
                    break;
                case "load":
                    await LoadPlugin(plugin);
                    break;
                case "unload":
                    await UnloadPlugin(plugin);
                    break;
            }
        }

        private async Task LoadPlugin(string plugin)
        {
            var messageLoad = await ReplyAsync($"Loading {plugin}...");
            var result = _pluginService.LoadPluginByName(plugin);
            var message = $"Failed to load {plugin}.";
            switch (result)
            {
                case PluginStatus.Success:
                    message = $"{plugin} successfully loaded.";
                    break;
                case PluginStatus.NotFound:
                    message += " The requested plugin could not be found.";
                    break;
                case PluginStatus.Conflict:
                    message += " Multiple plugins matched the criteria.";
                    break;
                case PluginStatus.AlreadyUnloaded:
                    message += " Plugin was already unloaded. This should never happen.";
                    break;
                case PluginStatus.AlreadyLoaded:
                    message += " Plugin was already loaded.";
                    break;
                case PluginStatus.Failed:
                    message += " The plugin had an error trying to load.";
                    break;
                default:
                    message += " ¯\\_(ツ)_/¯";
                    break;
            }
            await messageLoad.ModifyAsync(o => o.Content = message);
        }

        private async Task UnloadPlugin(string plugin)
        {
            var messageUnload = await ReplyAsync($"Unloading {plugin}...");
            var pluginList = _pluginService.Plugins.Where(o =>
                o.Plugin.Name.Trim().Contains(plugin.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList();
            if (pluginList.Count == 0)
                await messageUnload.ModifyAsync(o => o.Content = $"Failed to find {plugin}!");
            else if (pluginList.Count > 1)
                await messageUnload.ModifyAsync(o => o.Content =
                    "Multiple plugins match the criteria: "
                    + pluginList.Aggregate("```", (accumulator, name) => accumulator + $"- {name.Plugin.Name}\n")
                    + "```");
            else
            {
                var removePlugin = pluginList[0];
                var result = await _pluginService.UnloadPlugin(removePlugin);
                var message = $"Failed to unload {removePlugin.Plugin.Name}.";
                switch (result)
                {
                    case PluginStatus.Success:
                        message = $"{removePlugin.Plugin.Name} successfully unloaded.";
                        break;
                    case PluginStatus.NotFound:
                        message += " The requested plugin could not be found.";
                        break;
                    case PluginStatus.Conflict:
                        message += " Multiple plugins matched the criteria.";
                        break;
                    case PluginStatus.AlreadyUnloaded:
                        message += " Plugin was already unloaded.";
                        break;
                    case PluginStatus.AlreadyLoaded:
                        message += " Plugin was already loaded. This should never happen.";
                        break;
                    case PluginStatus.Failed:
                        message += " The plugin had an error trying to unload.";
                        break;
                    default:
                        message += " ¯\\_(ツ)_/¯";
                        break;
                }

                await messageUnload.ModifyAsync(o => o.Content = message);
            }
        }
    }
}