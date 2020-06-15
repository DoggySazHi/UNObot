#pragma warning disable IDE1006 // Naming Styles

namespace UNObot.BitbucketEntities
{
    public class Repository
    {
        public string type { get; set; }
        public string name { get; set; }
        public string full_name { get; set; }
        public string uuid { get; set; }
        public UserLinks links { get; set; }
        public Project project { get; set; }
        public string website { get; set; }
        public Owner owner { get; set; }
        public string scm { get; set; }
        public bool is_private { get; set; }
    }
}