using System;
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
                                Task.Run(() => ProcessMessage(Text, (WebhookType) Type));
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
                }
            }
            catch (Exception e)
            {
                if(!e.Message.Contains("close", StringComparison.CurrentCultureIgnoreCase))
                    LoggerService.Log(LogSeverity.Critical, "Webhook Listener commit the die.", e);
            }
        }

        private void ProcessMessage(string Message, WebhookType WType)
        {
            if (WType == WebhookType.Bitbucket)
            {
                var Thing = new JObject(Message);
                LoggerService.Log(LogSeverity.Debug, Thing.ToString(Formatting.Indented));
            }
            else if (WType == WebhookType.OctoPrint)
            {
                
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
