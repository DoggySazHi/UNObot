using System;

namespace UNObot.BitbucketEntities
{
    public class Push
    {
        public Push push { get; set; }
        public Owner actor { get; set; }
        public Repository repository { get; set; }
    }

    public class Changes
    {
        public Change[] changes { get; set; }
    }

    public class Change
    {
        public bool forced { get; set; }
        public Old old { get; set; }
        public Links4 links { get; set; }
        public bool created { get; set; }
        public Commit[] commits { get; set; }
        public bool truncated { get; set; }
        public bool closed { get; set; }
        public NEw _new { get; set; }
    }

    public class Old
    {
        public string name { get; set; }
        public Links links { get; set; }
        public string default_merge_strategy { get; set; }
        public string[] merge_strategies { get; set; }
        public string type { get; set; }
        public Target target { get; set; }
    }

    public class Links
    {
        public Commits commits { get; set; }
        public Self self { get; set; }
        public Html html { get; set; }
    }

    public class Commits
    {
        public string href { get; set; }
    }

    public class Self
    {
        public string href { get; set; }
    }

    public class Html
    {
        public string href { get; set; }
    }

    public class Target
    {
        public Rendered rendered { get; set; }
        public string hash { get; set; }
        public Links1 links { get; set; }
        public Author author { get; set; }
        public Summary summary { get; set; }
        public Parent[] parents { get; set; }
        public DateTime date { get; set; }
        public string message { get; set; }
        public string type { get; set; }
        public PropertIes properties { get; set; }
    }

    public class Rendered
    {
    }

    public class Links1
    {
        public Self1 self { get; set; }
        public Html1 html { get; set; }
    }

    public class Self1
    {
        public string href { get; set; }
    }

    public class Html1
    {
        public string href { get; set; }
    }

    public class Author
    {
        public string raw { get; set; }
        public string type { get; set; }
        public User user { get; set; }
    }

    public class User
    {
        public string display_name { get; set; }
        public string uuid { get; set; }
        public Links2 links { get; set; }
        public string nickname { get; set; }
        public string type { get; set; }
        public string account_id { get; set; }
    }

    public class Links2
    {
        public Self2 self { get; set; }
        public Html2 html { get; set; }
        public Avatar avatar { get; set; }
    }

    public class Self2
    {
        public string href { get; set; }
    }

    public class Html2
    {
        public string href { get; set; }
    }

    public class Avatar
    {
        public string href { get; set; }
    }

    public class Summary
    {
        public string raw { get; set; }
        public string markup { get; set; }
        public string html { get; set; }
        public string type { get; set; }
    }

    public class PropertIes
    {
    }

    public class Parent
    {
        public string hash { get; set; }
        public string type { get; set; }
        public Links3 links { get; set; }
    }

    public class Links3
    {
        public Self3 self { get; set; }
        public Html3 html { get; set; }
    }

    public class Self3
    {
        public string href { get; set; }
    }

    public class Html3
    {
        public string href { get; set; }
    }

    public class Links4
    {
        public Commits1 commits { get; set; }
        public Html4 html { get; set; }
        public Diff diff { get; set; }
    }

    public class Commits1
    {
        public string href { get; set; }
    }

    public class Html4
    {
        public string href { get; set; }
    }

    public class Diff
    {
        public string href { get; set; }
    }

    public class NEw
    {
        public string name { get; set; }
        public Links5 links { get; set; }
        public string default_merge_strategy { get; set; }
        public string[] merge_strategies { get; set; }
        public string type { get; set; }
        public Target1 target { get; set; }
    }

    public class Links5
    {
        public Commits2 commits { get; set; }
        public Self4 self { get; set; }
        public Html5 html { get; set; }
    }

    public class Commits2
    {
        public string href { get; set; }
    }

    public class Self4
    {
        public string href { get; set; }
    }

    public class Html5
    {
        public string href { get; set; }
    }

    public class Target1
    {
        public Rendered1 rendered { get; set; }
        public string hash { get; set; }
        public Links6 links { get; set; }
        public Author1 author { get; set; }
        public Summary1 summary { get; set; }
        public Parent1[] parents { get; set; }
        public string date { get; set; }
        public string message { get; set; }
        public string type { get; set; }
        public Properties properties { get; set; }
    }

    public class Rendered1
    {
    }

    public class Links6
    {
        public Self5 self { get; set; }
        public Html6 html { get; set; }
    }

    public class Self5
    {
        public string href { get; set; }
    }

    public class Html6
    {
        public string href { get; set; }
    }

    public class Author1
    {
        public string raw { get; set; }
        public string type { get; set; }
        public User1 user { get; set; }
    }

    public class User1
    {
        public string display_name { get; set; }
        public string uuid { get; set; }
        public LInks links { get; set; }
        public string nickname { get; set; }
        public string type { get; set; }
        public string account_id { get; set; }
    }

    public class LInks
    {
        public Self6 self { get; set; }
        public Html7 html { get; set; }
        public Avatar1 avatar { get; set; }
    }

    public class Self6
    {
        public string href { get; set; }
    }

    public class Html7
    {
        public string href { get; set; }
    }

    public class Avatar1
    {
        public string href { get; set; }
    }

    public class Summary1
    {
        public string raw { get; set; }
        public string markup { get; set; }
        public string html { get; set; }
        public string type { get; set; }
    }

    public class Properties
    {
    }

    public class Parent1
    {
        public string hash { get; set; }
        public string type { get; set; }
        public Links7 links { get; set; }
    }

    public class Links7
    {
        public Self7 self { get; set; }
        public Html8 html { get; set; }
    }

    public class Self7
    {
        public string href { get; set; }
    }

    public class Html8
    {
        public string href { get; set; }
    }

    public class Commit
    {
        public Rendered2 rendered { get; set; }
        public string hash { get; set; }
        public Links8 links { get; set; }
        public Author2 author { get; set; }
        public Summary2 summary { get; set; }
        public Parent2[] parents { get; set; }
        public string date { get; set; }
        public string message { get; set; }
        public string type { get; set; }
        public Properties1 properties { get; set; }
    }

    public class Rendered2
    {
    }

    public class Links8
    {
        public Self8 self { get; set; }
        public Comments comments { get; set; }
        public PatCh patch { get; set; }
        public Html9 html { get; set; }
        public Diff1 diff { get; set; }
        public Approve approve { get; set; }
        public Statuses statuses { get; set; }
    }

    public class Self8
    {
        public string href { get; set; }
    }

    public class Comments
    {
        public string href { get; set; }
    }

    public class PatCh
    {
        public string href { get; set; }
    }

    public class Html9
    {
        public string href { get; set; }
    }

    public class Diff1
    {
        public string href { get; set; }
    }

    public class Approve
    {
        public string href { get; set; }
    }

    public class Statuses
    {
        public string href { get; set; }
    }

    public class Author2
    {
        public string raw { get; set; }
        public string type { get; set; }
        public User2 user { get; set; }
    }

    public class User2
    {
        public string display_name { get; set; }
        public string uuid { get; set; }
        public Links9 links { get; set; }
        public string nickname { get; set; }
        public string type { get; set; }
        public string account_id { get; set; }
    }

    public class Links9
    {
        public Self9 self { get; set; }
        public Html10 html { get; set; }
        public Avatar2 avatar { get; set; }
    }

    public class Self9
    {
        public string href { get; set; }
    }

    public class Html10
    {
        public string href { get; set; }
    }

    public class Avatar2
    {
        public string href { get; set; }
    }

    public class Summary2
    {
        public string raw { get; set; }
        public string markup { get; set; }
        public string html { get; set; }
        public string type { get; set; }
    }

    public class Properties1
    {
    }

    public class Parent2
    {
        public string hash { get; set; }
        public string type { get; set; }
        public LiNks links { get; set; }
    }

    public class LiNks
    {
        public Self10 self { get; set; }
        public Html11 html { get; set; }
    }

    public class Self10
    {
        public string href { get; set; }
    }

    public class Html11
    {
        public string href { get; set; }
    }

    public class Links10
    {
        public Self11 self { get; set; }
        public Html12 html { get; set; }
        public Avatar3 avatar { get; set; }
    }

    public class Self11
    {
        public string href { get; set; }
    }

    public class Html12
    {
        public string href { get; set; }
    }

    public class Avatar3
    {
        public string href { get; set; }
    }

    public class Links11
    {
        public Self12 self { get; set; }
        public Html13 html { get; set; }
        public Avatar4 avatar { get; set; }
    }

    public class Self12
    {
        public string href { get; set; }
    }

    public class Html13
    {
        public string href { get; set; }
    }

    public class Avatar4
    {
        public string href { get; set; }
    }

    public class Links12
    {
        public Self13 self { get; set; }
        public HtmL html { get; set; }
        public Avatar5 avatar { get; set; }
    }

    public class Self13
    {
        public string href { get; set; }
    }

    public class HtmL
    {
        public string href { get; set; }
    }

    public class Avatar5
    {
        public string href { get; set; }
    }

}
