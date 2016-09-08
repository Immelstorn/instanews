using System.Collections.Generic;

namespace InstagramNews.Models.Instagram
{
    public class Likes
    {
        public int count { get; set; }
        public List<Datum> data { get; set; }
    }
}