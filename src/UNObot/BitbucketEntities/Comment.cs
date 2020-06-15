using System;

#pragma warning disable IDE1006 // Naming Styles

namespace UNObot.BitbucketEntities
{
    public class Comment
    {
        public ItemLinks links { get; set; }
        public Content content { get; set; }
        public DateTime created_on { get; set; }
        public Owner user { get; set; }
        public object updated_on { get; set; }
        public string type { get; set; }
        public int id { get; set; }
        public ParentComment parent { get; set; }

        public class ParentComment
        {
            public string id;
        }
    }
}