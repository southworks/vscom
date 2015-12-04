namespace VSCOM.ReverseArticles
{
    using System.Collections.Generic;

    public class Section
    {
        public Section()
        {
            this.Articles = new List<Article>();
        }

        public List<Article> Articles { get; set; }

        public string Title { get; internal set; }
    }
}
