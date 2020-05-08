#pragma warning disable IDE1006 // Naming Styles

namespace UNObot.BitbucketEntities
{
    public class Owner
    {
        public string display_name { get; set; }
        public string UUIDThing { get; set; }
        public UserLinks links { get; set; }
        public string nickname { get; set; }
        public string type { get; set; }
        public string account_id { get; set; }
    }
}