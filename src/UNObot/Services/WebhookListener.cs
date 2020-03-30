using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace UNObot.Services
{
    public class WebhookListener : IDisposable
    {
        private readonly Dictionary<string, ulong> bitbucketServers = new Dictionary<string, ulong>();
        private static WebhookListener Instance;
        private readonly HttpListener Server;
        private readonly byte[] DefaultResponse;
        private readonly ManualResetEvent Exited;
        private bool Stop;
        private Task ServerThread;

        private WebhookListener()
        {
            if (!HttpListener.IsSupported)
            {
                LoggerService.Log(LogSeverity.Error, "Webhook listener is not supported on this computer!");
                return;
            }

            //bitbucketServers.Add("aURMBj-_zAJi6nsCPim6ezNxM6LYtwGrZmgMbV3RcKCfwFFygqUz2jLSecPjEEBUIYIh1Xewdp-vtJi5tqCidyKazInmq-20dtBNybd4xilFElaaMb0PFh8ileaZ6CN6q2BUZw", 420005591155605535);
            DefaultResponse = Encoding.UTF8.GetBytes("mukyu!");
            Exited = new ManualResetEvent(false);

            Server = new HttpListener();
            Server.Prefixes.Add("http://127.0.0.1:6860/");
            Server.Prefixes.Add("http://localhost:6860/");

            Server.Start();
            ServerThread = Task.Run(Listener);
        }

        public static WebhookListener GetSingleton()
        {
            return Instance ??= new WebhookListener();
        }

        private void Listener()
        {
            while (!Stop)
            {
                var context = Server.GetContext();
                var request = context.Request;
                LoggerService.Log(LogSeverity.Debug, $"Received request from {context.Request.RemoteEndPoint}.");

                using var data = request.InputStream;
                using var sr = new StreamReader(data);
                var text = sr.ReadToEnd();
                //LoggerService.Log(LogSeverity.Verbose, $"Data received: {text}");

                using var response = context.Response;
                response.StatusCode = 200;

                using var output = response.OutputStream;
                output.Write(DefaultResponse, 0, DefaultResponse.Length);
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
