using System.Collections.Generic;

namespace DuplicateDetector.Templates
{
    public class ImageMessage
    {
        public ulong Author { get; set; }
        public string Link { get; set; }
        public List<ImageAttachment> Attachments { get; set; }
    }

    public class ImageAttachment
    {
        public string URL { get; set; }
        public string ProxyURL { get; set; }
        public bool IsSpoiler { get; set; }
    }
}