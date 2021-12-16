using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using UNObot.Plugins;
using UNObot.Plugins.Attributes;
using UNObot.Services;

namespace UNObot.Modules;

public class PluginConfigCommands : UNObotModule<UNObotCommandContext>
{
    private readonly PluginLoaderService _pluginService;

    public PluginConfigCommands(PluginLoaderService pluginService)
    {
        _pluginService = pluginService;
    }
        
    [RequireOwner]
    [Command("plugins", RunMode = RunMode.Async), Alias("pl")]
    [Help(new[] {".plugins"}, "Get all plugins loaded in the bot.", true, "UNObot 4.1.8")]
    public async Task Plugin()
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
        
    [RequireOwner]
    [Command("plugins", RunMode = RunMode.Async), Alias("pl")]
    [Help(new[] {".plugins (mode)"}, "Get all plugins loaded in the bot.", true, "UNObot 4.1.8")]
    public async Task Plugins(string mode, [Remainder] string plugin)
    {
        switch (mode.Trim().ToLower())
        {
            case "reload":
                await ReloadPlugin(plugin);
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
        var error = GetMessage(result);
        if (result == PluginStatus.Success)
            message = error;
        else
            message += $" {error}";
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
            var error = GetMessage(result);
            if (result == PluginStatus.Success)
                message = error;
            else
                message += $" {error}";

            await messageUnload.ModifyAsync(o => o.Content = message);
        }
    }
        
    private async Task ReloadPlugin(string plugin)
    {
        var messageUnload = await ReplyAsync($"Reloading {plugin}...");
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
            var pluginFile = removePlugin.FileName;
            var resultUnload = await _pluginService.UnloadPlugin(removePlugin);
            var resultLoad = _pluginService.LoadPluginByName(pluginFile);
                
            var message = $"Results of reloading {removePlugin.Plugin.Name}:";
            message += $"\n- Unload: {GetMessage(resultUnload)}";
            message += $"\n- Load: {GetMessage(resultLoad)}";

            await messageUnload.ModifyAsync(o => o.Content = message);
        }
    }

    private static string GetMessage(PluginStatus result)
    {
        return result switch
        {
            PluginStatus.Success => "The requested operation was successful.",
            PluginStatus.NotFound => "The requested plugin could not be found.",
            PluginStatus.Conflict => "Multiple plugins matched the criteria.",
            PluginStatus.AlreadyUnloaded => "Plugin was already unloaded. Please don't.",
            PluginStatus.AlreadyLoaded => "Plugin was already loaded. Stawp.",
            PluginStatus.Failed => "The plugin had an error trying to unload. See console for errors.",
            _ => "¯\\_(ツ)_/¯"
        };
    }
}