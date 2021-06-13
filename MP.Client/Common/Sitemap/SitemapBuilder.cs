using System.Data;

namespace MP.Client.Common.Sitemap
{
    public abstract class SitemapBuilder
    {
        protected const string SITEMAP_TEMPLATE = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">{0}
</urlset>";
        protected const string URL_TEMPLATE = @"
    <url>
        <loc>{0}</loc>
        <priority>0.5</priority>
    </url>";

        public abstract void BuildAndSaveSiteMap(IDbConnection connection, string publicFolder);
    }
}
