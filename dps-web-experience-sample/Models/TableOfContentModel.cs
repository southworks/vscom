namespace ACOM.DocumentationSample.Models
{
    using System.Web;

    public class TableOfContentModel
    {
        public IHtmlString Content { get; set; }

        public string ArticleSlug { get; set; }
    }
}