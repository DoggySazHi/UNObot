#pragma warning disable IDE1006 // Naming Styles

namespace UNObot.BitbucketEntities
{
    public class Link
    {
        public string href { get; set; }
    }

    public class UserLinks
    {
        public Link self { get; set; }
        public Link html { get; set; }
        public Link avatar { get; set; }
    }

    public class ProjectLinks
    {
        public Link html { get; set; }
        public Link avatar { get; set; }
    }

    public class ItemLinks
    {
        public Link self { get; set; }
        public Link html { get; set; }
    }

    public class IssueLinks
    {
        public Link attachments { get; set; }
        public Link self { get; set; }
        public Link watch { get; set; }
        public Link comments { get; set; }
        public Link html { get; set; }
        public Link vote { get; set; }
    }
}