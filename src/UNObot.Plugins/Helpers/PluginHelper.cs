﻿using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using UNObot.Plugins.TerminalCore;

namespace UNObot.Plugins.Helpers;

public static class PluginHelper
{
    private static readonly Emote Delete;

    static PluginHelper()
    {
        Delete = Emote.Parse("<a:trash:878428941013090314>");
    }

    /// <summary>
    /// Get the directory recommended for the plugin to store its files.
    /// </summary>
    /// <returns>A directory path.</returns>
    public static string Directory()
    {
        var assemblyPath = Assembly.GetCallingAssembly().Location;
        if (string.IsNullOrEmpty(assemblyPath))
            assemblyPath = AppContext.BaseDirectory;
        var folder = Path.Combine(Path.GetDirectoryName(assemblyPath)!,
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
        => MakeDeletable(message, 0);

    /// <summary>
    /// Add a reaction recognizing that the message is deletable.
    /// </summary>
    /// <param name="message">The message to add the react to.</param>
    /// <param name="user">The user allowed to delete the message.</param>
    /// <returns>The same message (used in a chain).</returns>
    public static IUserMessage MakeDeletable(this IUserMessage message, ulong user)
    {
        message.ModifyAsync(o =>
                o.Components = MakeDeletable(user))
            .ContinueWithoutAwait(_ => {});
        return message;
    }
    
    /// <summary>
    /// Create a message component recognizing that the message is deletable.
    /// </summary>
    /// <returns>A <see cref="MessageComponent"/> which can be used to delete the message.</returns>
    public static MessageComponent MakeDeletable()
        => MakeDeletable(0);

    /// <summary>
    /// Create a message component recognizing that the message is deletable.
    /// </summary>
    /// <param name="user">The user allowed to delete the message.</param>
    /// <returns>A <see cref="MessageComponent"/> which can be used to delete the message.</returns>
    public static MessageComponent MakeDeletable(ulong user)
        => new ComponentBuilder()
            .WithButton("Delete", $"delete{user}", ButtonStyle.Danger, Delete)
            .Build();

    /// <summary>
    /// Delete the message if the user-deletable button has been clicked.
    /// Used internally in the command handler.
    /// </summary>
    /// <param name="interaction">The interaction sent</param>
    /// <param name="client">The Discord bot client.</param>
    public static async Task DeleteReact(SocketInteraction interaction, DiscordSocketClient client)
    {
        if (interaction is not SocketMessageComponent button) return;
        var message = button.Message;
        //if (message == null) return;
        
        if (message.Author.Id == client.CurrentUser.Id &&
            button.Data.Type == ComponentType.Button &&
            button.Data.CustomId.StartsWith("delete"))
        {
            var user = ulong.Parse(button.Data.CustomId[6..]);
            if (button.User.Id == user)
                await message.DeleteAsync();
        }
    }

    /// <summary>
    /// Execute an asynchronous method without blocking or looking at its output.
    /// Recommended when there is a need to run an async method in a synchronous one.
    /// </summary>
    /// <param name="task">The method to execute.</param>
    /// <param name="exceptionCallback">A handler, if an exception is thrown.</param>
    public static void ContinueWithoutAwait(this Task task, Action<Task> exceptionCallback)
    {
        _ = task.ContinueWith(exceptionCallback, TaskContinuationOptions.OnlyOnFaulted);
    }
        
    /// <summary>
    /// Execute an asynchronous method without blocking or looking at its output.
    /// Recommended when there is a need to run an async method in a synchronous one.
    /// </summary>
    /// <param name="task">The method to execute.</param>
    /// <param name="logger">A logger to attach, if an exception is thrown.</param>
    public static void ContinueWithoutAwait(this Task task, ILogger logger)
    {
        ContinueWithoutAwait(task, t =>
        {
            var fieldInfo = typeof(Task).GetField("m_action", BindingFlags.Instance | BindingFlags.NonPublic);
            var action = fieldInfo?.GetValue(t) as Delegate;
            var method = $"{action?.Method.Name ?? "<unknown method>"}.{action?.Method.DeclaringType?.FullName ?? "<unknown>"}";
            logger?.Log(LogSeverity.Error, $"Exception thrown in async method {method}.", t.Exception);
        });
    }

    /// <summary>
    /// Append a blank field to the embed.
    /// </summary>
    /// <param name="builder">The <code>EmbedBuilder</code> to add the blank field to.</param>
    /// <returns>The same <code>EmbedBuilder</code>, for chaining purposes.</returns>
    public static EmbedBuilder AddBlankField(this EmbedBuilder builder)
        => builder.AddField("\u200b", "\u200b");

    /// <summary>
    /// Ghost a message in a channel for a period of time.
    /// </summary>
    /// <param name="context">The context of which the message is to be sent.</param>
    /// <param name="text">Text to include. May be null.</param>
    /// <param name="fallback">A fallback message to send, if the embed fails. If null, <code>text</code> will be used instead.</param>
    /// <param name="embed">An embed to include. May be null.</param>
    /// <param name="component">A Discord message component to attach to the message. May be null.</param>
    /// <param name="time">How long to wait until the message is deleted.</param>
    /// <returns>An <code>IUserMessage</code> associated with the ghosted message, or null if the message could not be sent.</returns>
    public static async Task<IUserMessage> GhostMessage(ICommandContext context, string text = null, string fallback = null, Embed embed = null, MessageComponent component = null, int time = 5000)
    {
        if (text == null && embed == null)
            return null;
        IUserMessage message;

        if (time <= 0)
        {
            try
            {
                message = await context.ReplyAsync(text, embed: embed, component: component);
            }
            catch (CommandException)
            {
                fallback ??= text;
                message = await context.ReplyAsync(fallback);
            }
            
            return message;
        }
        
        try
        {
            // IDK why ReplyAsync doesn't work; is Discord.NET.Labs not capable of deleting followup messages?
            message = await context.Channel.SendMessageAsync(text, embed: embed, components: component);
        }
        catch (CommandException)
        {
            fallback ??= text;
            message = await context.Channel.SendMessageAsync(fallback);
        }
        
        await Task.Delay(time);
        await message.DeleteAsync();
        return null;
    }

    public static async Task<IUserMessage> ReplyAsync(this ICommandContext context,
        string message = null,
        bool isTTS = false,
        Embed embed = null,
        RequestOptions options = null,
        AllowedMentions allowedMentions = null,
        MessageReference messageReference = null,
        MessageComponent component = null,
        bool ephemeral = false)
    {
        if (context is not UNObotCommandContext { Interaction: { } } unobotContext)
            return await context.Channel.SendMessageAsync(message, isTTS, embed, options, allowedMentions, messageReference,
                component);
        //if (unobotContext.Interaction.HasResponded)
            return await unobotContext.Interaction.FollowupAsync(message, null, isTTS, ephemeral, allowedMentions, component,
                embed, options);
            /*
        await unobotContext.Interaction.RespondAsync(message, null, isTTS, ephemeral, allowedMentions, component,
            embed, options);
        return null;*/
    }

    public static Color RandomColor()
    {
        var random = ThreadSafeRandom.ThisThreadsRandom;
            
        var rgb = new[] { 0, 255, random.Next(256) };
        for (var i = rgb.Length - 1; i >= 0; --i)
        {
            var index = random.Next(i + 1);
                
            (rgb[index], rgb[i]) = (rgb[i], rgb[index]);
        }

        return new Color(rgb[0], rgb[1], rgb[2]);
    }
}