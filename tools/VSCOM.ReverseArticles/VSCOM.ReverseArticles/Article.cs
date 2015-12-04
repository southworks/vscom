namespace VSCOM.ReverseArticles
{
    public class Article
    {
        public string Folder { get; internal set; }

        public string Path { get; internal set; }

        public string Slug { get; internal set; }

        public object Title { get; internal set; }

        public string TOCTitle { get; internal set; }

        public bool DoNotIncludeInTOC { get; internal set; }
    }
}
