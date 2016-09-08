namespace InstagramNews.Models.Instagram
{
    public class RootObject<T>
    {
        public Meta meta { get; set; }
        public T data { get; set; }
    }
}