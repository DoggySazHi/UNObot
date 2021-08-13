using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace UNObot.Plugins
{
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
            InteractionResponseType type = InteractionResponseType.ChannelMessageWithSource,
            bool ephemeral = false)
        {
            if (Context is not UNObotCommandContext { Interaction: { } } unobotContext)
                return await base.ReplyAsync(message, isTTS, embed, options, allowedMentions, messageReference,
                    component);
            await unobotContext.Interaction.RespondAsync(message, isTTS, embed, type, ephemeral, allowedMentions, options, component)
                .ConfigureAwait(false);
            return await unobotContext.Interaction.GetOriginalResponseAsync();

        }
        
     
    }
}