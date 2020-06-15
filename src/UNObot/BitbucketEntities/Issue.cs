using System;

#pragma warning disable IDE1006 // Naming Styles

namespace UNObot.BitbucketEntities
{
    public class Issue
    {
        public string priority { get; set; }
        public string kind { get; set; }
        public IssueLinks links { get; set; }
        public string title { get; set; }
        public Owner reporter { get; set; }
        public object component { get; set; }
        public int votes { get; set; }
        public int watches { get; set; }
        public Content content { get; set; }
        public Owner assignee { get; set; }
        public string state { get; set; }
        public object version { get; set; }
        public object edited_on { get; set; }
        public DateTime created_on { get; set; }
        public object milestone { get; set; }
        public DateTime updated_on { get; set; }
        public string type { get; set; }
        public int id { get; set; }
    }
}