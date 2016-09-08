using System.Collections.Generic;

namespace InstagramNews.Models.Instagram
{
    public class News
    {
        public string status { get; set; }
        public List<Story> stories { get; set; }
        public bool SessionIdSet { get; set; }
        public string ValidationError { get; set; }
    }
}