using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace ACOM.DocumentationSample.Helpers
{
    public static class CultureHelper
    {
        public static Regex LocaleRegex = new Regex("[a-zA-Z]{2}-[a-zA-Z]{2}");

        public static string LocalizeUrl(string url)
        {
            if (url == null)
            {
                return "#";
            }

            var startsWithSlash = url.StartsWith("/");

            var localizedUrl = url.TrimStart('/');

            if ((localizedUrl.Length >= 5) && LocaleRegex.IsMatch(localizedUrl.Substring(0, 5)))
            {
                return url;
            }

            var culturePrefix = startsWithSlash
                ? string.Format("/{0}/", GetCurrentCulture().ToLowerInvariant())
                : string.Format("{0}/", GetCurrentCulture().ToLowerInvariant());

            localizedUrl = culturePrefix + localizedUrl;

            return localizedUrl;
        }

        public static string GetCurrentCulture()
        {
            return Thread.CurrentThread.CurrentUICulture.Name.ToLowerInvariant();
        }

        public static IHtmlString LocalizeUrl(this HtmlHelper helper, string url)
        {
            return MvcHtmlString.Create(CultureHelper.LocalizeUrl(url));
        }

        public static string GetDefaultCulture()
        {
            return "en-us";
        }
    }
}