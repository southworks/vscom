using System.Collections.Generic;

namespace ACOM.DocumentationSample.Models
{
    public class DocumentationArticlesListModel
    {
        public DocumentationArticlesListModel()
        {
            this.Articles = new List<Article>();
        }

        public string PageTitle { get; set; }

        public int Count { get; set; }

        public List<Article> Articles { get; set; }
    }
}