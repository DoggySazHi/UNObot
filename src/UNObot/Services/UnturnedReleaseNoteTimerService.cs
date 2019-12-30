using System;
using System.ServiceModel.Syndication;
using System.Timers;
using System.Xml;

namespace UNObot.Services
{
    public class UnturnedReleaseNotes : IDisposable
    {
        private static UnturnedReleaseNotes instance;
        private string lastLink;
        private readonly Timer checkInterval;

        private UnturnedReleaseNotes()
        {
            lastLink = GetLatestLink();
            checkInterval = new Timer
            {
                AutoReset = true,
                Interval = 1000 * 60 * 5,
                Enabled = true
            };
            checkInterval.Elapsed += CheckForUpdates;
        }

        private async void CheckForUpdates(object sender, ElapsedEventArgs e)
        {
            string Link = GetLatestLink();
            if (lastLink != Link)
            {
                lastLink = Link;
                await Program._client.GetGuild(185593135458418701).GetTextChannel(477647595175411718).SendMessageAsync(Link);
                //_ = Program.SendPM(Link, 191397590946807809);
                Console.WriteLine("Found update.");
            }
            else
                Console.WriteLine("No updates found.");
        }

        public static UnturnedReleaseNotes GetInstance()
        {
            if (instance == null)
                instance = new UnturnedReleaseNotes();
            return instance;
        }

        public static string GetLatestLink()
        {
            string url = "https://steamcommunity.com/games/304930/rss/";
            SyndicationFeed feed;
            using (XmlReader reader = XmlReader.Create(url))
                feed = SyndicationFeed.Load(reader);
            string Link = "";
            foreach (SyndicationItem item in feed.Items)
            {
                //string subject = item.Title.Text;
                //string summary = item.Summary.Text;
                if (item.Links.Count > 0)
                {
                    Link = item.Links[0].GetAbsoluteUri().ToString();
                    break;
                }
            }
            return Link;
        }

        public void Dispose()
        {
            checkInterval?.Dispose();
        }
    }
}
