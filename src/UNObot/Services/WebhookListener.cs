using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Discord;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace UNObot.Services
{
    public enum WebhookType : byte { Bitbucket = 0, OctoPrint = 1 }

    public class WebhookListener : IDisposable
    {
        private static WebhookListener Instance;
        private readonly HttpListener Server;
        private readonly byte[] DefaultResponse;
        private readonly ManualResetEvent Exited;
        private bool Stop;
        private bool Disposed;

        private WebhookListener()
        {
            if (!HttpListener.IsSupported)
            {
                LoggerService.Log(LogSeverity.Error, "Webhook listener is not supported on this computer!");
                return;
            }

            DefaultResponse = Encoding.UTF8.GetBytes("Mukyu!");
            Exited = new ManualResetEvent(false);

            try
            {
                Server = new HttpListener();
                Server.Prefixes.Add("http://127.0.0.1:6860/");
                Server.Prefixes.Add("http://localhost:6860/");

                Server.Start();
            }
            catch (HttpListenerException ex)
            {
                LoggerService.Log(LogSeverity.Critical, "Webhook Listener failed!", ex);
            }

            Task.Run(Listener);
        }

        public static WebhookListener GetSingleton()
        {
            return Instance ??= new WebhookListener();
        }

        private void Listener()
        {
            try
            {
                while (!Stop)
                {
                    var Context = Server.GetContext();
                    var Request = Context.Request;
                    var URL = Context.Request.RawUrl;
#if DEBUG
                    LoggerService.Log(LogSeverity.Debug, $"Received request from {URL}.");
                    var Headers = "Headers: ";
                    foreach (var Key in Request.Headers.AllKeys)
                        Headers += $"{Key}, {Request.Headers[Key]}\n";
                    LoggerService.Log(LogSeverity.Debug, Headers);
#endif

                    var Sections = URL.Split("/");

                    if (Sections.Length >= 3)
                    {
                        var ChannelParse = ulong.TryParse(Sections[1], out var Channel);
                        if (ChannelParse)
                        {
                            var Key = Sections[2];
                            var (Guild, Type) = UNODatabaseService.GetWebhook(Channel, Key).GetAwaiter().GetResult();
                            if (Guild != 0)
                            {
#if DEBUG
                                LoggerService.Log(LogSeverity.Debug,
                                    $"Found server, points to {Guild}, {Channel} of type {(WebhookType)Type}.");
#endif
                                using var Data = Request.InputStream;
                                using var Reader = new StreamReader(Data);
                                var Text = Reader.ReadToEnd();
#if DEBUG
                                LoggerService.Log(LogSeverity.Verbose, $"Data received: {Text}");
#endif
                                try
                                {
                                    ProcessMessage(Text, Request.Headers, Guild, Channel, (WebhookType)Type);
                                }
                                catch (Exception e)
                                {
                                    LoggerService.Log(LogSeverity.Critical, "Webhook Parser failed!", e);
                                    LoggerService.Log(LogSeverity.Critical, $"URL: {URL}");
                                    LoggerService.Log(LogSeverity.Critical, Text);

                                }
                            }
                            else
                            {
                                LoggerService.Log(LogSeverity.Warning, $"Does not seem to point to a server. URL: {URL}");
                            }

                        }
                        else
                        {
                            LoggerService.Log(LogSeverity.Debug, $"Given an invalid channel! URL: {URL}");
                        }
                    }

                    using var response = Context.Response;
                    response.StatusCode = 200;

                    using var output = response.OutputStream;
                    output.Write(DefaultResponse, 0, DefaultResponse.Length);
#if DEBUG
                    LoggerService.Log(LogSeverity.Debug, "Sent back OK.");
#endif
                }
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("close", StringComparison.CurrentCultureIgnoreCase))
                    LoggerService.Log(LogSeverity.Critical, "Webhook Listener commit the die.", e);
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

        private void ProcessMessage(string Message, NameValueCollection Headers, ulong Guild, ulong Channel, WebhookType WType)
        {
            try
            {
                if (WType == WebhookType.Bitbucket)
                {
                    JToken Thing;
                    string exceptionPath = null;
                    using (var textReader = new StringReader(Message))
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
                            LoggerService.Log(LogSeverity.Error, $@"Error near string: {Message.Substring(
                                Math.Max(0, ex.LinePosition - 10), Math.Min(20, Message.Length - ex.LinePosition - 10)
                            )}", ex);
                        }
                        Thing = jsonWriter.Token;
                    }
                    Debug.Assert(Thing != null, nameof(Thing) + " != null");
                    if (exceptionPath != null)
                    {
                        var badToken = Thing.SelectToken(exceptionPath);
                        LoggerService.Log(LogSeverity.Error, $"Error occurred with token: {badToken}");
                    }

                    //LoggerService.Log(LogSeverity.Debug, Thing.ToString(Formatting.Indented));
                    if (Thing["push"] != null)
                    {
                        var Commits = Thing["push"]?["changes"]?.Children();
                        if (Commits == null) return;
                        var Embeds = new List<Embed>();
                        foreach (var Item in Commits)
                        {
                            var Commit = Item["new"]?["target"];

                            var CommitInfo = new CommitInfo
                            {
                                RepoName = Thing["repository"]?["name"]?.ToString() ?? "Unknown Repo Name",
                                RepoAvatar = Thing["repository"]?["links"]?["avatar"]?["href"]?.ToString() ?? "",

                                CommitHash = Commit?["hash"]?.ToString() ?? "Hash not found!",
                                CommitMessage = Commit?["message"]?.ToString() ?? "No message was attached.",
                                CommitDate = Commit?["date"]?.ToObject<DateTime>() ?? DateTime.Now,

                                UserName = Commit?["author"]?["user"]?["nickname"]?.ToString() ??
                                           Commit?["author"]?["user"]?["display_name"]?.ToString() ?? "Unknown User Name",
                                UserAvatar = Commit?["author"]?["user"]?["links"]?["avatar"]?["href"]?.ToString() ?? ""
                            };
                            EmbedDisplayService.WebhookEmbed(CommitInfo, out var Embed);
                            Embeds.Add(Embed);
                        }
                        Embeds.Sort((a, b) => (int)((a.Timestamp ?? DateTimeOffset.Now) - (b.Timestamp ?? DateTimeOffset.Now)).TotalMilliseconds);
                        Embeds.ForEach(o => Program._client.GetGuild(Guild).GetTextChannel(Channel).SendMessageAsync(null, false, o));
                    }
                }
                else if (WType == WebhookType.OctoPrint)
                {
                    var ContentType = Headers.GetValues("Content-Type");
                    if (ContentType == null)
                    {
                        LoggerService.Log(LogSeverity.Error, "Could not find Content-Type!");
                        return;
                    }
                    var Boundaries = ContentType.Where(o => o.Contains("boundary=")).ToList();
                    if (!Boundaries.Any())
                        return;
                    var Boundary = Boundaries.First();
                    var Key = Boundary.Remove(0, Boundary.IndexOf('=') + 1);
                    var Parts = Message.Split(
                        new[] { "\r\n", "\r", "\n" },
                        StringSplitOptions.None
                    );
                    var Data = new Dictionary<string, string>();
                    byte State = 0; // 0: Invalid (not ready) 1: Waiting for name 2: Empty 3: Data
                    string Name = "";
                    foreach (var Line in Parts)
                    {
                        if (Line.Contains(Key))
                        {
                            if (State == 1)
                                LoggerService.Log(LogSeverity.Warning, "Huh? Got two boundaries in a row!");
                            State = 1;
                            continue;
                        }
                        if (State == 1)
                        {
                            if (string.IsNullOrWhiteSpace(Line))
                                break;
                            if (Line.Contains("Content-Disposition: form-data; name=\""))
                            {
                                var FirstIndex = Line.IndexOf("\"", StringComparison.Ordinal);
                                var LastIndex = Line.LastIndexOf("\"", StringComparison.Ordinal);
                                if (FirstIndex != -1 && FirstIndex != LastIndex)
                                {
                                    Name = Line.Substring(FirstIndex + 1, LastIndex - FirstIndex - 1);
                                    State = 2;
                                    continue;
                                }
                            }
                            State = 0;
                            LoggerService.Log(LogSeverity.Warning, "Did not see a valid name!");
                        }
                        if (State == 2)
                        {
                            State = 3;
                            continue;
                        }
                        if (State == 3)
                        {
                            if (string.IsNullOrWhiteSpace(Name))
                            {
                                LoggerService.Log(LogSeverity.Error, "Will not read an empty name!");
                            }
                            else
                            {
                                Data.Add(Name, Line);
                            }
                            State = 0;
                        }
                    }

                    foreach (var Thing in Data)
                    {
                        LoggerService.Log(LogSeverity.Debug, $"{Thing.Key} - {Thing.Value}");
                    }

                    var Info = new OctoprintInfo
                    {
                        Extra = JObject.Parse(Data["extra"]),
                        Topic = Data["topic"],
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(Data["currentTime"])),
                        State = JObject.Parse(Data["state"]),
                        Job = JObject.Parse(Data["job"]),
                        Message = Data["message"],
                        Progress = JObject.Parse(Data["progress"])
                    };
                    EmbedDisplayService.OctoprintEmbed(Info, out var Embed);
                    Program._client.GetGuild(Guild).GetTextChannel(Channel).SendMessageAsync(null, false, Embed);
                }
            }
            catch (JsonReaderException)
            {
                LoggerService.Log(LogSeverity.Error, $"Could not read JSON from server! Log:\n{Message}");
            }
        }

        public void Dispose()
        {
            Stop = true;
            if (Disposed)
                return;
            Disposed = true;

            try
            {
                Server.Stop();
                Server.Close();
                Exited.Close();
                ((IDisposable)Server)?.Dispose();
                Exited.Dispose();
            }
            catch (Exception e)
            {
                LoggerService.Log(LogSeverity.Error, "Failed to dispose WebhookListener properly: ", e);
                // ignored
            }
        }
    }
}
