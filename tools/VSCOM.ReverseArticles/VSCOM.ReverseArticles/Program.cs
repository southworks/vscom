namespace VSCOM.ReverseArticles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using HtmlAgilityPack;

    public static class Program
    {
        private static string baseUrl = "https://www.visualstudio.com";
        private static string localFolder = @"C:\temp\vscom-articles\";

        public static void Main(string[] args)
        {
            // 0. Clean up from previous executions and recreate folder
            string articlesPath = Path.Combine(localFolder, "articles");
            var directory = new DirectoryInfo(articlesPath);
            if (directory.Exists)
            {
                directory.Empty();
                directory.Delete();
            }

            directory.Create();

            // 1. Get the list of all the articles on the left nav.
            TOC toc = GetArticlesTOC();
            toc = InsertServiceHookIntegrationArticles(toc);

            // Dump the TOC to a file as Markdown
            StringBuilder tocBuilder = new StringBuilder();

            // Insert DPS metadata for HTML conversion
            tocBuilder.AppendLine(@"<properties
    pageTitle=""Table of Contents""
  description=""Table of Contents""
  authors=""terryaustin"" /> ");
            tocBuilder.AppendLine();
            foreach (var section in toc.Sections)
            {
                // mimic original TOC behavior (escaped closing parenthesis)
                string sectionLink = string.Format("- [{0}](javascript:void(0)\\)", section.Title);
                tocBuilder.AppendLine(sectionLink);
                foreach (var article in section.Articles.Where(a => !a.DoNotIncludeInTOC))
                {
                    string markdownLink = string.Format("    - [{0}]({1})", article.TOCTitle, article.Path);
                    tocBuilder.AppendLine(markdownLink);
                }
            }

            string path = Path.Combine(articlesPath, "table-of-contents.md");
            File.WriteAllText(path, tocBuilder.ToString());
        }

        private static TOC InsertServiceHookIntegrationArticles(TOC toc)
        {
            string IntegrateWithServiceHookArticle = "/en-us/get-started/integrate/integrating-with-service-hooks-vs";

            WebClient client = new WebClient();
            string articleContent = client.DownloadString(baseUrl + IntegrateWithServiceHookArticle);

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(articleContent);
            HtmlNode tocTopNode = htmlDoc.DocumentNode.SelectSingleNode(@".//h2[text()='Available services']/following-sibling::table");
            var tocNodes = tocTopNode.SelectNodes(".//a");
            var IntegrateSection = toc.Sections.Single(s => s.Title == "Integrate");
            foreach(var link in tocNodes)
            {
                string linkUrl = link.Attributes["href"].Value;
                if (linkUrl.StartsWith(baseUrl))
                {
                    linkUrl = linkUrl.Replace(baseUrl, string.Empty);
                    Article a = ProcessArticle(linkUrl);
                    a.TOCTitle = link.InnerText;
                    a.DoNotIncludeInTOC = true;
                    IntegrateSection.Articles.Add(a);
                }
            }

            return toc;
        }

        private static TOC GetArticlesTOC()
        {
            string startArticle = "/en-us/get-started/code/share-your-code-in-tfvc-vs";

            WebClient client = new WebClient();
            string articleContent = client.DownloadString(baseUrl + startArticle);

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(articleContent);
            HtmlNode tocTopNode = htmlDoc.DocumentNode.SelectSingleNode(@".//div[@class='TocNavigationVertical']");

            // Go through the TOC     
            TOC toc = new TOC();
            IEnumerable<HtmlNode> tocNodes = tocTopNode.SelectNodes(".//a");

            foreach (var link in tocNodes)
            {
                // 3 types of links in the TOC. Sections and articles and external links
                string linkUrl = link.Attributes["href"].Value;
                if (linkUrl == "javascript:void(0)")
                {
                    // Section
                    Section section = new Section();
                    section.Title = link.InnerText;
                    toc.Sections.Add(section);
                }
                else
                {
                    Article a;
                    if (linkUrl.StartsWith("/en-us/get-started/"))
                    {
                        // A link to an article
                        a = ProcessArticle(linkUrl);
                    }
                    else
                    {
                        // An external link
                        a = new Article { Path = linkUrl };
                    }

                    a.TOCTitle = link.InnerText;
                    toc.Sections[toc.Sections.Count - 1].Articles.Add(a);
                }
            }

            return toc;
        }

        private static Article ProcessArticle(string articleUrl)
        {
            Article article = new Article();

            // Create the folders
            string[] articleSlugSections = articleUrl.Split(new char[] { '/' });
            article.Slug = articleSlugSections[4];
            article.Folder = articleSlugSections[3];

            string folderPath = Path.Combine(localFolder, "articles", article.Folder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);

                string mediaFolderPath = Path.Combine(localFolder, "articles", article.Folder, "media");
                Directory.CreateDirectory(mediaFolderPath);
            }

            // Create the media folder for this article
            string articleMediaFolderPath = Path.Combine(localFolder, "articles", article.Folder, "media", article.Slug);
            Directory.CreateDirectory(articleMediaFolderPath);

            // Download the content
            WebClient client = new WebClient();
            var articleContent = client.DownloadString(baseUrl + articleUrl);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(articleContent);

            // Get <div class="content">
            HtmlNode contentNode = htmlDoc.DocumentNode.SelectSingleNode(@".//div[@class='content']");

            // H1
            var titleNode = contentNode.SelectSingleNode(@"./h1");
            article.Title = titleNode.InnerText;

            // Identify all the images
            var imagesNodes = contentNode.SelectNodes(@".//img");
            if (imagesNodes != null)
            {
                foreach (var imageNode in imagesNodes)
                {
                    var imageUrl = imageNode.Attributes["src"].Value;
                    string[] imageUrlSections = imageUrl.Split(new char[] { '/' });
                    string imageFileName = imageUrlSections[imageUrlSections.Length - 1];
                    string articleImageLocalPath = Path.Combine(articleMediaFolderPath, imageFileName);
                    client.DownloadFile(imageUrl, articleImageLocalPath);
                    imageNode.Attributes["src"].Value = string.Format("./media/{0}/{1}", article.Slug, imageFileName);
                }
            }

            // Fix all internal links
            var linksNodes = contentNode.SelectNodes(@".//a[contains(@href,'visualstudio.com/get-started/')]/@href");
            if (linksNodes != null)
            {
                foreach (var linkNode in linksNodes)
                {
                    var linkUrl = linkNode.Attributes["href"].Value;
                    string[] imageUrlSections = linkUrl.Split(new char[] { '/' });
                    string linkFolder = imageUrlSections[imageUrlSections.Length - 2];
                    string linkArticle = imageUrlSections[imageUrlSections.Length - 1];

                    // If this article is in the same folder as the article it links to
                    string internalLink = string.Empty;
                    if (linkFolder == article.Folder)
                    {
                        internalLink = string.Empty;
                    }
                    else
                    {
                        internalLink = string.Format("../{0}/", linkFolder);
                    }

                    // Handle # in the URL
                    if (linkArticle.Contains("#"))
                    {
                        linkArticle = linkArticle.Replace("#", ".md#");
                    }
                    else
                    {
                        linkArticle += ".md";
                    }

                    linkNode.Attributes["href"].Value = string.Format("{0}{1}", internalLink, linkArticle);
                }
            }

            // Concert the content to Markdown       
            string htmlContents = contentNode.InnerHtml;
            string unknownTagsConverter = "pass_through";
            bool githubFlavored = true;
            var config = new ReverseMarkdown.Config(unknownTagsConverter, githubFlavored);
            var converter = new ReverseMarkdown.Converter(config);
            string converted = converter.Convert(htmlContents);

            // Add the netadata section at tge beginning of the articles
            string metadataFormat =
      @"<properties
	pageTitle=""{0}""
  description=""{1}""
  services=""visual-studio-online""
  documentationCenter = """"
  authors=""terryaustin""
  manager=""terryaustin""
  editor=""terryaustin"" /> ";
            string metadataWithValues = string.Format(metadataFormat, article.Title, article.Title);

            string articleMarkdownContent = metadataWithValues + Environment.NewLine + converted;

            string path = Path.Combine(localFolder, "articles", article.Folder, article.Slug + ".md");
            File.WriteAllText(path, articleMarkdownContent);

            article.Path = string.Format("articles/{0}/{1}.md", article.Folder, article.Slug);

            return article;
        }

        private static void Empty(this System.IO.DirectoryInfo directory)
        {
            foreach (System.IO.FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }

            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories())
            {
                subDirectory.Empty();
                subDirectory.Delete(true);
            }
        }
    }
}