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
                    LoggerService.Log(LogSeverity.Debug, $"Received request from {URL}.");
                    var Headers = "Headers: ";
                    foreach (var Key in Request.Headers.AllKeys)
                        Headers += $"{Key}, {Request.Headers[Key]}\n";
                    LoggerService.Log(LogSeverity.Debug, Headers);

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
                                LoggerService.Log(LogSeverity.Debug, 
                                    $"Found server, points to {Guild}, {Channel} of type {(WebhookType) Type}.");
                                using var Data = Request.InputStream;
                                using var Reader = new StreamReader(Data);
                                var Text = Reader.ReadToEnd();
                                //LoggerService.Log(LogSeverity.Verbose, $"Data received: {text}");
                                ProcessMessage(Text, Guild, Channel, (WebhookType) Type);
                            }
                            else
                            {
                                LoggerService.Log(LogSeverity.Debug, "Does not seem to point to a server.");
                            }
                            
                        }
                        else
                        {
                            LoggerService.Log(LogSeverity.Debug,
                                $"Given an invalid channel!");
                        }
                    }

                    using var response = Context.Response;
                    response.StatusCode = 200;

                    using var output = response.OutputStream;
                    output.Write(DefaultResponse, 0, DefaultResponse.Length);

                    LoggerService.Log(LogSeverity.Debug, "Sent back OK.");
                }
            }
            catch (Exception e)
            {
                if(!e.Message.Contains("close", StringComparison.CurrentCultureIgnoreCase))
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

        private void ProcessMessage(string Message, ulong Guild, ulong Channel, WebhookType WType)
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
                    
                    LoggerService.Log(LogSeverity.Debug, Thing.ToString(Formatting.Indented));
                    if (Thing["push"] != null)
                    {
                        var Commit = Thing["push"]?["changes"]?.First?["new"]?["target"];

                        var CommitInfo = new CommitInfo
                        {
                            RepoName = Thing["repository"]?["name"]?.ToString() ?? "Unknown Repo Name",
                            RepoAvatar = Thing["repository"]?["links"]?["avatar"]?.ToString() ?? "",
                            
                            CommitHash = Commit?["hash"]?.ToString() ?? "Hash not found!",
                            CommitMessage = Commit?["message"]?.ToString() ?? "No message was attached.",
                            CommitDate = Commit?["date"]?.ToObject<DateTime>() ?? DateTime.Now,
                            
                            UserName = Commit?["author"]?["nickname"]?.ToString() ??
                                       Thing["actor"]?["display_name"]?.ToString() ?? "Unknown User Name",
                            UserAvatar = Commit?["author"]?["links"]?["avatar"]?.ToString() ?? ""
                        };
                        EmbedDisplayService.WebhookEmbed(CommitInfo, out var Embed);
                        Program._client.GetGuild(Guild).GetTextChannel(Channel).SendMessageAsync(null, false, Embed);
                    }
                }
                else if (WType == WebhookType.OctoPrint)
                {
                    
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

            try
            {
                Server.Stop();
                Server.Close();
                Exited.Close();
                ((IDisposable) Server)?.Dispose();
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
