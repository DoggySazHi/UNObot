using System;
using System.ServiceModel.Syndication;
using System.Timers;
using System.Xml;
using Discord;

namespace UNObot.Services
{
    public class UnturnedReleaseNotes : IDisposable
    {
        private static UnturnedReleaseNotes _instance;
        private readonly Timer _checkInterval;
        private string _lastLink;

        private UnturnedReleaseNotes()
        {
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
                await Program.Client.GetGuild(185593135458418701).GetTextChannel(477647595175411718)
                    .SendMessageAsync(link);
                //_ = Program.SendPM(Link, 191397590946807809);
                LoggerService.Log(LogSeverity.Verbose, "Found update.");
            }
            else
            {
                LoggerService.Log(LogSeverity.Verbose, "No updates found.");
            }
        }

        public static UnturnedReleaseNotes GetSingleton()
        {
            return _instance ??= new UnturnedReleaseNotes();
        }

        public static string GetLatestLink()
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