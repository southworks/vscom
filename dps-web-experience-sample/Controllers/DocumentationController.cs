namespace ACOM.DocumentationSample.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Configuration;
    using System.Web.Mvc;
    using ACOM.DocumentationSample.Helpers;
    using ACOM.DocumentationSample.Models;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class DocumentationController : Controller
    {
        private readonly ArticlesConfiguration config;
        private readonly CloudBlobContainer blobContainer;
        private readonly string partnerName;

        public DocumentationController()
        {
            this.config = new ArticlesConfiguration
            {
                ConnectionString = WebConfigurationManager.ConnectionStrings["articles"].ConnectionString,
                Container = WebConfigurationManager.AppSettings["container"],
                ArticleListFormat = "{0}/{1}/documentation/articles/{2}.html",
                SettingsName = "settings.json"
            };

            var storageAccount = CloudStorageAccount.Parse(this.config.ConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            this.blobContainer = blobClient.GetContainerReference(this.config.Container);

            this.partnerName = WebConfigurationManager.AppSettings["partner"];
        }

        /// <summary>
        /// Lists the documentation articles.
        /// </summary>
        /// <param name="culture">Culture from where the articles list will be loaded.</param>
        /// <returns></returns>
        [Route("{culture=en-us}/docs")]
        public async Task<ActionResult> Index(string culture)
        {
            var version = await this.GetPublishedVersion(this.blobContainer, culture) ?? await this.GetPublishedVersion(this.blobContainer, CultureHelper.GetDefaultCulture() ?? string.Empty);
            var blobPrefixFormat = this.config.ArticleListFormat.Replace("{2}.html", string.Empty);
            var prefix = string.Format(CultureInfo.InvariantCulture, blobPrefixFormat, version.Language, version.PublishedVersion);
            BlobContinuationToken token = null;

            var model = new DocumentationArticlesListModel { PageTitle = this.partnerName + " Documentation Articles" };

            do
            {
                var currentSegment = await this.blobContainer.ListBlobsSegmentedAsync(prefix, true, BlobListingDetails.Metadata, null, token, null, null);
                model.Articles.AddRange(currentSegment.Results.Cast<CloudBlockBlob>().Select(this.GetArticleMetada).Where(x => x.ArticleTitle != null).ToList());
                token = currentSegment.ContinuationToken;
            } while (token != null);

            model.Count = model.Articles.Count;
            return this.View(model);
        }

        /// <summary>
        /// Loads a documentation article.
        /// </summary>
        /// <param name="culture">Culture to localize the requested documentation article.</param>
        /// <param name="id">The id of the requested documentation article.</param>
        /// <returns></returns>
        [Route("{culture=en-us}/docs/{id}")]
        public async Task<ActionResult> Article(string culture, string id)
        {
            var version = await this.GetPublishedVersion(this.blobContainer, culture) ?? await this.GetPublishedVersion(this.blobContainer, CultureHelper.GetDefaultCulture() ?? string.Empty);
            var blobUrl = string.Format(CultureInfo.InvariantCulture, this.config.ArticleListFormat, version.Language, version.PublishedVersion, id);
            CloudBlockBlob blob = this.blobContainer.GetBlockBlobReference(blobUrl);
            if (!(await blob.ExistsAsync()))
            {
                throw new HttpException(404, "Page not found");
            }

            var articleMetadata = this.GetArticleMetada(blob);
            var articleContent = await this.GetContentAsync(blob);

            var viewModel = new DocumentationArticleMetadataModel
            {
                Culture = version.Language,
                Slug = articleMetadata.Slug,
                Tags = articleMetadata.Tags,
                Content = articleContent,
                PageTitle = articleMetadata.PageTitle,
                ArticleTitle = articleMetadata.ArticleTitle,
                MetaDescription = articleMetadata.MetaDescription,
                Contributors = this.GetContributorsAndAuthors(articleMetadata),
                PublishedVersion = version.PublishedVersion
            };

            return this.View(viewModel);
        }

        [ChildActionOnly]
        public ActionResult TableOfContents(string culture, string publishedVersion, string articleSlug)
        {
            var tocUrl = string.Format(CultureInfo.InvariantCulture, this.config.ArticleListFormat, culture, publishedVersion, "table-of-contents");
            CloudBlockBlob tocBlob = this.blobContainer.GetBlockBlobReference(tocUrl);
            if (!tocBlob.Exists())
            {
                throw new HttpException(404, "Page not found");
            }

            var tocContent = this.GetContent(tocBlob);
            var model = new TableOfContentModel
            {
                Content = tocContent,
                ArticleSlug = articleSlug
            };

            return this.PartialView(model);
        }

        #region Internal
        /// <summary>
        /// Get a list containing the authors of the article and the contributors.
        /// </summary>
        /// <param name="article">Documentation article.</param>
        /// <returns>List of GithubAuthor.</returns>
        internal IEnumerable<GithubAuthor> GetContributorsAndAuthors(Article article)
        {
            var authors = article.Authors.SelectMany(a => article.GitHubContributors.Where(c => c.Login.Equals(a, StringComparison.InvariantCultureIgnoreCase))).ToList();
            var otherContributors = article.GitHubContributors.Except(authors).Reverse().ToList();

            return authors.Union(otherContributors);
        }

        /// <summary>
        /// Parse the article tags.
        /// </summary>
        /// <param name="tagsString">JSON array containing the article tags.</param>
        /// <returns></returns>
        internal Dictionary<string, string> ParseTags(string tagsString)
        {
            var tagsDictionary = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(tagsString))
            {
                tagsDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(tagsString);
            }

            return tagsDictionary;
        }

        /// <summary>
        /// Resolve the slug from a blob name.
        /// </summary>
        /// <param name="blobName">Blob name.</param>
        /// <returns>Slugified blob name.</returns>
        internal string GetSlugFromBlobName(string blobName)
        {
            return blobName.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries).Last().Replace(".html", string.Empty);
        }

        /// <summary>
        /// Parse the authors names from a comma separated string.
        /// </summary>
        /// <param name="articleAuthorString">Comma separated array of author names.</param>
        /// <returns>Array of strings containing the parsed author names.</returns>
        internal string[] ParseAuthors(string articleAuthorString)
        {
            if (string.IsNullOrWhiteSpace(articleAuthorString))
            {
                return new string[0];
            }

            return articleAuthorString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray();
        }

        /// <summary>
        /// Deserialize the GithubContributors list.
        /// </summary>
        /// <param name="githubContributorsString">JSON string containing the array of Github Authors.</param>
        /// <returns>A collection with the parsed GithubAuthors.</returns>
        internal IEnumerable<GithubAuthor> ParseGitHubContributors(string githubContributorsString)
        {
            if (!string.IsNullOrEmpty(githubContributorsString))
            {
                var obj = JToken.Parse(githubContributorsString);
                return obj.ToObject<List<GithubAuthor>>();
            }

            return Enumerable.Empty<GithubAuthor>();
        }
        
        /// <summary>
        /// Loads the version info from the settings file in the storage container.
        /// Every culture can have a separate version.
        /// </summary>
        /// <param name="blobContainer">The blob storage container.</param>
        /// <param name="culture">Culture identifier</param>
        /// <returns></returns>
        internal async Task<PublishVersionInfo> GetPublishedVersion(CloudBlobContainer blobContainer, string culture)
        {
            PublishVersionInfo version = null;
            var blob = blobContainer.GetBlockBlobReference(culture + "/" + this.config.SettingsName);
            if (await blob.ExistsAsync())
            {
                var contents = await blob.DownloadTextAsync();
                version = JsonConvert.DeserializeObject<PublishVersionInfo>(contents);
            }

            return version;
        }
        #endregion Internal

        #region Private
        /// <summary>
        /// Loads the blob text content.
        /// </summary>
        /// <param name="blob">Documentation article blob.</param>
        /// <returns>The blob content as an HTML-encoded string.</returns>
        private async Task<IHtmlString> GetContentAsync(CloudBlockBlob blob)
        {
            var contents = await blob.DownloadTextAsync();

            return new HtmlString(contents);
        }

        /// <summary>
        /// Loads the blob text content.
        /// </summary>
        /// <param name="blob">Documentation article blob.</param>
        /// <returns>The blob content as an HTML-encoded string.</returns>
        private IHtmlString GetContent(CloudBlockBlob blob)
        {
            var contents = blob.DownloadText();

            return new HtmlString(contents);
        }

        /// <summary>
        /// Parse the article metadata from the given.
        /// </summary>
        /// <param name="blockBlob">Article blob.</param>
        /// <returns>Documentation article.</returns>
        private Article GetArticleMetada(CloudBlockBlob blockBlob)
        {
            var lastModifiedProp = blockBlob.Properties.LastModified;
            var pageTitle = string.Empty;
            var articleTitle = string.Empty;
            var metaDescription = string.Empty;
            var serviceString = string.Empty;
            var docCenterString = string.Empty;
            var tagsString = string.Empty;
            var createdDate = string.Empty;
            var githubContributorsString = string.Empty;
            string articleAuthorString = string.Empty;

            blockBlob.Metadata.TryGetValue("articleTitle", out articleTitle);
            blockBlob.Metadata.TryGetValue("pageTitle", out pageTitle);
            blockBlob.Metadata.TryGetValue("metaDescription", out metaDescription);
            blockBlob.Metadata.TryGetValue("articleServices", out serviceString);
            blockBlob.Metadata.TryGetValue("articleDocumentationCenter", out docCenterString);
            blockBlob.Metadata.TryGetValue("tags", out tagsString);
            blockBlob.Metadata.TryGetValue("createdDate", out createdDate);
            blockBlob.Metadata.TryGetValue("articleAuthor", out articleAuthorString);
            blockBlob.Metadata.TryGetValue("gitHubContributors", out githubContributorsString);

            if (string.IsNullOrEmpty(pageTitle))
            {
                pageTitle = articleTitle;
            }

            return new Article
            {
                LastModified = lastModifiedProp != null ? lastModifiedProp.Value.UtcDateTime : new DateTime(),
                Slug = this.GetSlugFromBlobName(blockBlob.Name),
                BlobName = blockBlob.Name,
                PageTitle = WebUtility.HtmlDecode(pageTitle),
                ArticleTitle = WebUtility.HtmlDecode(articleTitle),
                MetaDescription = WebUtility.HtmlDecode(metaDescription),
                Tags = this.ParseTags(tagsString),
                Authors = this.ParseAuthors(articleAuthorString),
                GitHubContributors = this.ParseGitHubContributors(WebUtility.HtmlDecode(githubContributorsString)).ToArray(),
            };
        }
        #endregion Private
    }
}