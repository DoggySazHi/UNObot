#pragma warning disable IDE1006 // Naming Styles

namespace UNObot.BitbucketEntities
{
    public class Project
    {
        public string type { get; set; }
        // Apparently project and name are the same thing? API listing isn't helpful.
        public string project { get; set; }
        public string name { get; set; }
        public string uuid { get; set; }
        public ProjectLinks links { get; set; }
        public string key { get; set; }
    }
}
