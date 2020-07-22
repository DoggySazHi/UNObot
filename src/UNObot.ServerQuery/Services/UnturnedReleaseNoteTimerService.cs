using System;
using System.ServiceModel.Syndication;
using System.Timers;
using System.Xml;
using Discord;
using Discord.WebSocket;
using UNObot.Plugins;

namespace UNObot.ServerQuery.Services
{
    internal class UnturnedReleaseNotes : IDisposable
    {
        private readonly Timer _checkInterval;
        private string _lastLink;
        private readonly DiscordSocketClient _client;
        private readonly ILogger _logger;

        internal UnturnedReleaseNotes(DiscordSocketClient client, ILogger logger)
        {
            _logger = logger;
            _client = client;
            _lastLink = GetLatestLink();
            _checkInterval = new Timer
            {
                AutoReset = true,
                Interval = 1000 * 60 * 5,
                Enabled = true
            };
            _checkInterval.Elapsed += CheckForUpdates;
        }

        public void Dispose()
        {
            _checkInterval?.Dispose();
        }

        private async void CheckForUpdates(object sender, ElapsedEventArgs e)
        {
            var link = GetLatestLink();
            if (_lastLink != link)
            {
                _lastLink = link;
                await _client.GetGuild(185593135458418701).GetTextChannel(477647595175411718)
                    .SendMessageAsync(link);
                //_ = Program.SendPM(Link, 191397590946807809);
                _logger.Log(LogSeverity.Verbose, "Found update.");
            }
            else
            {
                _logger.Log(LogSeverity.Verbose, "No updates found.");
            }
        }

        internal static string GetLatestLink()
        {
            var url = "https://steamcommunity.com/games/304930/rss/";
            SyndicationFeed feed;
            using (var reader = XmlReader.Create(url))
            {
                feed = SyndicationFeed.Load(reader);
            }

            var link = "";
            foreach (var item in feed.Items)
                //string subject = item.Title.Text;
                //string summary = item.Summary.Text;
                if (item.Links.Count > 0)
                {
                    link = item.Links[0].GetAbsoluteUri().ToString();
                    break;
                }

            return link;
        }
    }
}