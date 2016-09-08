using System.Collections.Generic;

namespace InstagramNews.Models.Instagram
{
    public class Args
    {
        public List<Medium> media { get; set; }
        public List<Link> links { get; set; }
        public string text { get; set; }
        public int profile_id { get; set; }
        public string profile_image { get; set; }
        public int timestamp { get; set; }
    }
}