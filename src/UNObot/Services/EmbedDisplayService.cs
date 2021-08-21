using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Newtonsoft.Json.Linq;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;
using UNObot.Plugins.Settings;

namespace UNObot.Services
{
    public class EmbedDisplayService
    {
        private readonly IUNObotConfig _config;

        public EmbedDisplayService(IUNObotConfig config)
        {
            _config = config;
        }

        public Embed WebhookEmbed(WebhookListenerService.CommitInfo info)
        {
            var builder = new EmbedBuilder()
                .WithColor(PluginHelper.RandomColor())
                .WithTimestamp(info.CommitDate)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {_config.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"Push by {info.UserName} for {info.RepoName}")
                        .WithIconUrl(info.UserAvatar);
                })
                .WithThumbnailUrl(info.RepoAvatar)
                .WithDescription(info.CommitMessage)
                .AddField("Commit Hash", info.CommitHash.Substring(0, Math.Min(7, info.CommitHash.Length)), true);
            return builder.Build();
        }

        public Embed OctoprintEmbed(WebhookListenerService.OctoprintInfo info)
        {
            var builder = new EmbedBuilder()
                .WithColor(PluginHelper.RandomColor())
                .WithTimestamp(info.Timestamp)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {_config.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName($"{info.Topic} - {info.Job?["file"]?["name"] ?? "Unknown File"}")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                })
                .WithDescription(info.Message)
                .AddField("Status", info.State["text"], true);
            var completion = info.Progress["completion"];
            var printTime = info.Progress["printTime"];
            var printTimeLeft = info.Progress["printTimeLeft"];
            var bytesFile = info.Job["file"]?["size"];
            var bytesPrinted = info.Progress["filepos"];
            if (completion != null && completion.Type != JTokenType.Null)
                builder.AddField("Progress", completion.ToObject<double>().ToString("N2") + "%", true);
            if (printTime != null && printTime.Type != JTokenType.Null)
                builder.AddField("Time", TimeHelper.HumanReadable(printTime.ToObject<float>()), true);
            if (printTimeLeft != null && printTimeLeft.Type != JTokenType.Null)
                builder.AddField("Estimated Time Left", TimeHelper.HumanReadable(printTimeLeft.ToObject<float>()), true);
            if (bytesFile != null && bytesFile.Type != JTokenType.Null)
                builder.AddField("File Size", (bytesFile.ToObject<float>() / 1000000.0).ToString("N2") + " MB", true);
            if (bytesPrinted != null && bytesPrinted.Type != JTokenType.Null)
                builder.AddField("Bytes Printed", (bytesPrinted.ToObject<float>() / 1000000.0).ToString("N2") + " MB",
                    true);
            return builder.Build();
        }

        public Embed SettingsEmbed(IEnumerable<Setting> settings)
        {
            var builder = new EmbedBuilder()
                .WithColor(PluginHelper.RandomColor())
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"UNObot {_config.Version} - By DoggySazHi")
                        .WithIconUrl("https://williamle.com/unobot/doggysazhi.png");
                })
                .WithAuthor(author =>
                {
                    author
                        .WithName("UNObot Settings")
                        .WithIconUrl("https://williamle.com/unobot/unobot.png");
                });
            foreach (var group in settings)
            {
                var titleLength = group.Relation.Keys.Max(o => o.Length) + 1;
                var sb = new StringBuilder();
                foreach (var key in group.Relation.Keys)
                {
                    sb.Append($"`{key.PadRight(titleLength)}|` ");
                    var obj = group.GetSetting(key);
                    if (obj == null)
                        sb.Append("*None set*\n");
                    else
                        sb.Append(obj.Display).Append('\n');
                }
                builder.AddField(group.Category, sb.ToString());
            }

            return builder.Build();
        }
    }
}