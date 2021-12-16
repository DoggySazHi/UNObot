using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using UNObot.Plugins.Helpers;

namespace UNObot.Plugins;

/// <inheritdoc cref="ModuleBase{T}"/>
public abstract class UNObotModule<T> : ModuleBase<T> where T : class, ICommandContext
{
    /// <inheritdoc cref="ReplyAsync"/>
    protected async Task<IUserMessage> ReplyAsync(
        string message = null,
        bool isTTS = false,
        Embed embed = null,
        RequestOptions options = null,
        AllowedMentions allowedMentions = null,
        MessageReference messageReference = null,
        MessageComponent component = null,
        bool ephemeral = false)
    {
        return await Context.ReplyAsync(message, isTTS, embed, options, allowedMentions, messageReference,
            component, ephemeral);
    }
}