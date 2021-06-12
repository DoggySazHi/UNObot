using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace UNObot.Plugins.Helpers
{
    public static class PluginHelper
    {
        private static readonly Emote Delete;

        static PluginHelper()
        {
            Delete = Emote.Parse("<:trash:747166938467401808>");
        }

        /// <summary>
        /// Get the directory recommended for the plugin to store its files.
        /// </summary>
        /// <returns>A directory path.</returns>
        public static string Directory()
        {
            var folder = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)!,
                Assembly.GetCallingAssembly().GetName().Name!);
            if (!System.IO.Directory.Exists(folder))
                System.IO.Directory.CreateDirectory(folder);
            return folder;
        }

        /// <summary>
        /// Add a reaction recognizing that the message is deletable.
        /// </summary>
        /// <param name="message">The message to add the react to.</param>
        /// <returns>The same message (used in a chain).</returns>
        public static IUserMessage MakeDeletable(this IUserMessage message)
        {
            message.AddReactionAsync(Delete).GetAwaiter().GetResult();
            return message;
        }

        /// <summary>
        /// Delete the message if the user deletable reaction has been added.
        /// Only used internally in the command handler.
        /// </summary>
        /// <param name="client">The Discord bot client.</param>
        /// <param name="message">The message to analyze.</param>
        /// <param name="emote">The reaction that is recognized as the delete emote.</param>
        public static async Task DeleteReact(DiscordSocketClient client, IUserMessage message, SocketReaction emote)
        {
            if (message.Author.Id == client.CurrentUser.Id && emote.UserId != client.CurrentUser.Id &&
                emote.Emote.Equals(Delete))
                await message.DeleteAsync();
        }
        
        public static void ContinueWithoutAwait(this Task task, Action<Task> exceptionCallback)
        {
            _ = task.ContinueWith(exceptionCallback, TaskContinuationOptions.OnlyOnFaulted);
        }
        
        public static void ContinueWithoutAwait(this Task task, ILogger logger)
        {
            _ = task.ContinueWith(t =>
            {
                var fieldInfo = typeof(Task).GetField("m_action", BindingFlags.Instance | BindingFlags.NonPublic);
                var action = fieldInfo?.GetValue(t) as Delegate;
                var method = $"{action?.Method.Name ?? "<unknown method>"}.{action?.Method.DeclaringType?.FullName ?? "<unknown>"}";
                logger.Log(LogSeverity.Error, $"Exception raised in async method {method}.", t.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Append a blank field to the embed.
        /// </summary>
        /// <param name="builder">The <code>EmbedBuilder</code> to add the blank field to.</param>
        /// <returns>The same <code>EmbedBuilder</code>, for chaining purposes.</returns>
        public static EmbedBuilder AddBlankField(this EmbedBuilder builder)
            => builder.AddField("\u200b", "\u200b");
        
        public static async Task<IUserMessage> GhostMessage(ICommandContext context, string text = null, string fallback = null, Embed embed = null, int time = 5000)
        {
            if (text == null && embed == null)
                return null;
            IUserMessage message;
            try
            {
                message = await context.Channel.SendMessageAsync(text, embed: embed);
            }
            catch (CommandException)
            {
                fallback ??= text;
                message = await context.Channel.SendMessageAsync(fallback);
            }

            if (time <= 0)
                return message;
            await Task.Delay(time);
            await message.DeleteAsync();
            return null;
        }
    }
}