using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UNObot.Services
{
    public enum WebhookType : byte
    {
        Bitbucket = 0,
        OctoPrint = 1
    }

    internal class WebhookListenerService : IDisposable
    {
        private readonly byte[] _defaultResponse;
        private readonly ManualResetEvent _exited;
        private readonly HttpListener _server;
        private bool _disposed;
        private bool _stop;
        private readonly LoggerService _logger;
        private readonly EmbedDisplayService _embed;
        private readonly UNODatabaseService _db;
        private readonly DiscordSocketClient _client;

        public WebhookListenerService(LoggerService logger, EmbedDisplayService embed, UNODatabaseService db, DiscordSocketClient client)
        {
            _logger = logger;
            _embed = embed;
            _db = db;
            _client = client;
            
            if (!HttpListener.IsSupported)
            {
                _logger.Log(LogSeverity.Error, "Webhook listener is not supported on this computer!");
                return;
            }

            _defaultResponse = Encoding.UTF8.GetBytes("Mukyu!");
            _exited = new ManualResetEvent(false);

            try
            {
                _server = new HttpListener();
                _server.Prefixes.Add("http://127.0.0.1:6860/");
                _server.Prefixes.Add("http://localhost:6860/");

                _server.Start();
            }
            catch (HttpListenerException ex)
            {
                _logger.Log(LogSeverity.Critical, "Webhook Listener failed!", ex);
            }

            Task.Run(Listener);
        }

        public void Dispose()
        {
            _stop = true;
            if (_disposed)
                return;
            _disposed = true;

            try
            {
                _server.Stop();
                _server.Close();
                _exited.Close();
                ((IDisposable) _server)?.Dispose();
                _exited.Dispose();
            }
            catch (Exception e)
            {
                _logger.Log(LogSeverity.Error, "Failed to dispose WebhookListenerService properly: ", e);
                // ignored
            }
        }
        
        private void Listener()
        {
            try
            {
                while (!_stop)
                {
                    var context = _server.GetContext();
                    var request = context.Request;
                    var url = context.Request.RawUrl;
#if DEBUG
                    _logger.Log(LogSeverity.Debug, $"Received request from {url}.");
                    var headers = "Headers: ";
                    foreach (var key in request.Headers.AllKeys)
                        headers += $"{key}, {request.Headers[key]}\n";
                    _logger.Log(LogSeverity.Debug, headers);
#endif

                    var sections = url.Split("/");

                    if (sections.Length >= 3)
                    {
                        var channelParse = ulong.TryParse(sections[1], out var channel);
                        if (channelParse)
                        {
                            var key = sections[2];
                            var (guild, type) = _db.GetWebhook(channel, key).GetAwaiter().GetResult();
                            if (guild != 0)
                            {
#if DEBUG
                                _logger.Log(LogSeverity.Debug,
                                    $"Found server, points to {guild}, {channel} of type {(WebhookType) type}.");
#endif
                                using var data = request.InputStream;
                                using var reader = new StreamReader(data);
                                var text = reader.ReadToEnd();
#if DEBUG
                                _logger.Log(LogSeverity.Verbose, $"Data received: {text}");
#endif
                                try
                                {
                                    ProcessMessage(text, request.Headers, guild, channel, (WebhookType) type);
                                }
                                catch (Exception e)
                                {
                                    _logger.Log(LogSeverity.Critical, "Webhook Parser failed!", e);
                                    _logger.Log(LogSeverity.Critical, $"URL: {url}");
                                    _logger.Log(LogSeverity.Critical, text);
                                }
                            }
                            else
                            {
                                _logger.Log(LogSeverity.Warning,
                                    $"Does not seem to point to a server. URL: {url}");
                            }
                        }
                        else
                        {
                            _logger.Log(LogSeverity.Debug, $"Given an invalid channel! URL: {url}");
                        }
                    }

                    using var response = context.Response;
                    response.StatusCode = 200;

                    using var output = response.OutputStream;
                    output.Write(_defaultResponse, 0, _defaultResponse.Length);
#if DEBUG
                    _logger.Log(LogSeverity.Debug, "Sent back OK.");
#endif
                }
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("close", StringComparison.CurrentCultureIgnoreCase))
                    _logger.Log(LogSeverity.Critical, "Webhook Listener commit the die.", e);
            }
        }

        private void ProcessMessage(string message, NameValueCollection headers, ulong guild, ulong channel,
            WebhookType wType)
        {
            try
            {
                if (wType == WebhookType.Bitbucket)
                {
                    JToken thing;
                    string exceptionPath = null;
                    using (var textReader = new StringReader(message))
                    using (var jsonReader = new JsonTextReader(textReader))
                    using (var jsonWriter = new JTokenWriter())
                    {
                        try
                        {
                            jsonWriter.WriteToken(jsonReader);
                        }
                        catch (JsonReaderException ex)
                        {
                            exceptionPath = ex.Path;
                            _logger.Log(LogSeverity.Error, $@"Error near string: {message.Substring(
                                Math.Max(0, ex.LinePosition - 10), Math.Min(20, message.Length - ex.LinePosition - 10)
                            )}", ex);
                        }

                        thing = jsonWriter.Token;
                    }

                    Debug.Assert(thing != null, nameof(thing) + " != null");
                    if (exceptionPath != null)
                    {
                        var badToken = thing.SelectToken(exceptionPath);
                        _logger.Log(LogSeverity.Error, $"Error occurred with token: {badToken}");
                    }

                    //_logger.Log(LogSeverity.Debug, Thing.ToString(Formatting.Indented));
                    if (thing["push"] != null)
                    {
                        var commits = thing["push"]?["changes"]?.Children();
                        if (commits == null) return;
                        var embeds = new List<Embed>();
                        foreach (var item in commits)
                        {
                            var commit = item["new"]?["target"];

                            var commitInfo = new CommitInfo
                            {
                                RepoName = thing["repository"]?["name"]?.ToString() ?? "Unknown Repo Name",
                                RepoAvatar = thing["repository"]?["links"]?["avatar"]?["href"]?.ToString() ?? "",

                                CommitHash = commit?["hash"]?.ToString() ?? "Hash not found!",
                                CommitMessage = commit?["message"]?.ToString() ?? "No message was attached.",
                                CommitDate = commit?["date"]?.ToObject<DateTime>() ?? DateTime.Now,

                                UserName = commit?["author"]?["user"]?["nickname"]?.ToString() ??
                                           commit?["author"]?["user"]?["display_name"]?.ToString() ??
                                           "Unknown User Name",
                                UserAvatar = commit?["author"]?["user"]?["links"]?["avatar"]?["href"]?.ToString() ?? ""
                            };
                            _embed.WebhookEmbed(commitInfo, out var embed);
                            embeds.Add(embed);
                        }

                        embeds.Sort((a, b) =>
                            (int) ((a.Timestamp ?? DateTimeOffset.Now) - (b.Timestamp ?? DateTimeOffset.Now))
                            .TotalMilliseconds);
                        embeds.ForEach(o =>
                            _client.GetGuild(guild).GetTextChannel(channel).SendMessageAsync(null, false, o));
                    }
                }
                else if (wType == WebhookType.OctoPrint)
                {
                    var contentType = headers.GetValues("Content-Type");
                    if (contentType == null)
                    {
                        _logger.Log(LogSeverity.Error, "Could not find Content-Type!");
                        return;
                    }

                    var boundaries = contentType.Where(o => o.Contains("boundary=")).ToList();
                    if (!boundaries.Any())
                        return;
                    var boundary = boundaries.First();
                    var key = boundary.Remove(0, boundary.IndexOf('=') + 1);
                    var parts = message.Split(
                        new[] {"\r\n", "\r", "\n"},
                        StringSplitOptions.None
                    );
                    var data = new Dictionary<string, string>();
                    byte state = 0; // 0: Invalid (not ready) 1: Waiting for name 2: Empty 3: Data
                    var name = "";
                    foreach (var line in parts)
                    {
                        if (line.Contains(key))
                        {
                            if (state == 1)
                                _logger.Log(LogSeverity.Warning, "Huh? Got two boundaries in a row!");
                            state = 1;
                            continue;
                        }

                        if (state == 1)
                        {
                            if (string.IsNullOrWhiteSpace(line))
                                break;
                            if (line.Contains("Content-Disposition: form-data; name=\""))
                            {
                                var firstIndex = line.IndexOf("\"", StringComparison.Ordinal);
                                var lastIndex = line.LastIndexOf("\"", StringComparison.Ordinal);
                                if (firstIndex != -1 && firstIndex != lastIndex)
                                {
                                    name = line.Substring(firstIndex + 1, lastIndex - firstIndex - 1);
                                    state = 2;
                                    continue;
                                }
                            }

                            state = 0;
                            _logger.Log(LogSeverity.Warning, "Did not see a valid name!");
                        }

                        if (state == 2)
                        {
                            state = 3;
                            continue;
                        }

                        if (state == 3)
                        {
                            if (string.IsNullOrWhiteSpace(name))
                                _logger.Log(LogSeverity.Error, "Will not read an empty name!");
                            else
                                data.Add(name, line);
                            state = 0;
                        }
                    }

                    foreach (var thing in data) _logger.Log(LogSeverity.Debug, $"{thing.Key} - {thing.Value}");

                    var info = new OctoprintInfo
                    {
                        Extra = JObject.Parse(data["extra"]),
                        Topic = data["topic"],
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data["currentTime"])),
                        State = JObject.Parse(data["state"]),
                        Job = JObject.Parse(data["job"]),
                        Message = data["message"],
                        Progress = JObject.Parse(data["progress"])
                    };
                    _embed.OctoprintEmbed(info, out var embed);
                    _client.GetGuild(guild).GetTextChannel(channel).SendMessageAsync(null, false, embed);
                }
            }
            catch (JsonReaderException)
            {
                _logger.Log(LogSeverity.Error, $"Could not read JSON from server! Log:\n{message}");
            }
        }

        public struct CommitInfo
        {
            public string RepoName { get; set; }
            public string RepoAvatar { get; set; }

            public string CommitHash { get; set; }
            public string CommitMessage { get; set; }
            public DateTime CommitDate { get; set; }

            public string UserName { get; set; }
            public string UserAvatar { get; set; }
        }

        public struct OctoprintInfo
        {
            public string Topic { get; set; }
            public JObject Extra { get; set; }
            public string Message { get; set; }
            public JObject Progress { get; set; }
            public JObject State { get; set; }
            public JObject Job { get; set; }
            public DateTimeOffset Timestamp { get; set; }
        }
    }
}