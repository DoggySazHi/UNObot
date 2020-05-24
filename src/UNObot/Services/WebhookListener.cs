﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Discord;

namespace UNObot.Services
{
    public class WebhookListener : IDisposable
    {
        private static WebhookListener Instance;
        private JsonSerializerSettings Settings;
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

                Settings = new JsonSerializerSettings {MissingMemberHandling = MissingMemberHandling.Error};
                /*
                Settings.Error += (self, errorArgs) =>
                {
    
                };
                */

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
                //TODO Split by Client ID and then Key to optimize database.
                while (!Stop)
                {
                    var context = Server.GetContext();
                    var request = context.Request;
                    var URL = context.Request.RawUrl;
                    LoggerService.Log(LogSeverity.Debug, $"Received request from {URL}.");
                    var Headers = "Headers: ";
                    foreach (var Key in request.Headers.AllKeys)
                        Headers += $"{Key}, {request.Headers[Key]}\n";
                    LoggerService.Log(LogSeverity.Debug, Headers);
                    
                    if (URL.Length >= 1)
                    {
                        var ID = URL.Substring(1);
                        var ValidServers = UNODatabaseService.GetWebhook(ID).GetAwaiter().GetResult();
                        if(ValidServers.Guild != 0)
                            LoggerService.Log(LogSeverity.Debug,
                                $"Found server, points to {ValidServers.Guild}, {ValidServers.Channel}.");
                        else
                            LoggerService.Log(LogSeverity.Debug,
                                $"Does not seem to point to a server.");
                    }

                    using var data = request.InputStream;
                    using var sr = new StreamReader(data);
                    var text = sr.ReadToEnd();
                    //LoggerService.Log(LogSeverity.Verbose, $"Data received: {text}");
                    ProcessMessage(text);

                    using var response = context.Response;
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

        private void ProcessMessage(string Message)
        {
            var Types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t =>
                    t.Namespace != null && 
                    t.Namespace.Contains("BitbucketEntities", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var Type in Types)
            {
                try
                {
                    var Processed = JsonConvert.DeserializeObject(Message, Type, Settings);
                    LoggerService.Log(LogSeverity.Verbose, $"I processed an {Type.Name} successfully!");
                    LoggerService.Log(LogSeverity.Verbose, $"Read {Processed?.GetType()}");
                    return;
                }
                catch (JsonException)
                {

                }
            }
            LoggerService.Log(LogSeverity.Verbose, $"Failed to match against {Types.Count} modules.");
            LoggerService.Log(LogSeverity.Verbose, Message);
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
