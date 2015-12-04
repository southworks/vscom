using System;
using System.Collections.Generic;

namespace ACOM.DocumentationSample.Models
{
    public class Article
    {
        public string Slug { get; set; }

        public string BlobName { get; set; }

        public string PageTitle { get; set; }

        public string ArticleTitle { get; set; }

        public string MetaDescription { get; set; }

        public DateTime LastModified { get; set; }

        public Dictionary<string, string> Tags { get; set; }

        public string[] Authors { get; set; }

        public GithubAuthor[] GitHubContributors { get; set; }
    }
}