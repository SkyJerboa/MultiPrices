using Dapper;
using MP.Client.Common.Configuration;
using MP.Client.Common.Constants;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace MP.Client.Common.Sitemap
{
    public class FilterSitemapBuilder : SitemapBuilder
    {
        private const string GAME_SERVICES_QUERY = @"SELECT ""Code"" FROM ""Services""";
        private const string TAGS_QUERY = @"SELECT ""Name"" FROM ""Tags""";
        private const string DEVELOPERS_QUERY = @"SELECT DISTINCT ""Developer"" FROM ""Games"" WHERE ""Status""='Completed' AND ""Developer"" IS NOT NULL";

        private readonly string _urlTemplate;

        public FilterSitemapBuilder()
        {
            string host = SiteConfigurationManager.Config.Host;
            _urlTemplate = host + "/search?{0}={1}";
        }

        public override void BuildAndSaveSiteMap(IDbConnection connection, string publicFolder)
        {
            string siteMap = BuildSiteMap(connection);
            SaveSiteMap(siteMap, publicFolder);
        }

        private string BuildSiteMap(IDbConnection connection)
        {
            
            StringBuilder urlsBuilder = new StringBuilder();

            AddGameServicesLinks(connection, urlsBuilder);
            AddGameTypesLinks(urlsBuilder);
            AddTagsLinks(connection, urlsBuilder);
            AddDevelopersLinks(connection, urlsBuilder);

            return String.Format(SITEMAP_TEMPLATE, urlsBuilder.ToString());
        }

        private void SaveSiteMap(string siteMap, string publicFolder)
        {
            string sitemapPath = SiteConfigurationManager.Config.SitemapOptions.FiltersSitemapPath;
            string filePath = Path.Join(publicFolder, sitemapPath);
            File.WriteAllText(filePath, siteMap);
        }

        private void AddGameServicesLinks(IDbConnection connection, StringBuilder urlsBuilder)
        {
            IEnumerable<string> services = connection.Query<string>(GAME_SERVICES_QUERY);
            foreach(string service in services)
            {
                string url = String.Format(_urlTemplate, FilterParams.PARAM_GAME_SERVICE, service);
                urlsBuilder.AppendFormat(URL_TEMPLATE, url);
            }
        }

        private void AddGameTypesLinks(StringBuilder urlsBuilder)
        {
            foreach(string gameType in FilterParams.GAME_TYPES)
            {
                string url = String.Format(_urlTemplate, FilterParams.PARAM_GAME_TYPE, gameType);
                urlsBuilder.AppendFormat(URL_TEMPLATE, url);
            }
        }

        private void AddTagsLinks(IDbConnection connection, StringBuilder urlsBuilder)
        {
            IEnumerable<string> tags = connection.Query<string>(TAGS_QUERY);
            foreach(string tag in tags)
            {
                string url = String.Format(_urlTemplate, FilterParams.PARAM_TAG, tag);
                urlsBuilder.AppendFormat(URL_TEMPLATE, url);
            }
        }

        private void AddDevelopersLinks(IDbConnection connection, StringBuilder urlBuilder)
        {
            IEnumerable<string> developers = connection.Query<string>(DEVELOPERS_QUERY);
            foreach(string developer in developers)
            {
                string clearDev = developer.Replace(" ", "%20").Replace("&", "%26");
                string url = String.Format(_urlTemplate, FilterParams.PARAM_DEVELOPER, clearDev);
                urlBuilder.AppendFormat(URL_TEMPLATE, url);
            }
        }
    }
}
