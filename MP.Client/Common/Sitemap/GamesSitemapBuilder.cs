using Dapper;
using MP.Client.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace MP.Client.Common.Sitemap
{
    public class GamesSitemapBuilder : SitemapBuilder
    {
        private const string GAMES_QUERY = @"SELECT ""NameID"" FROM ""Games"" WHERE ""Status"" = 'Completed'";
        
        public override void BuildAndSaveSiteMap(IDbConnection connection, string publicFolder)
        {
            IEnumerable<string> games = connection.Query<string>(GAMES_QUERY);
            string siteMap = BuildSiteMap(games);
            SaveSiteMap(siteMap, publicFolder);
        }

        private string BuildSiteMap(IEnumerable<string> games)
        {
            string host = SiteConfigurationManager.Config.Host;
            StringBuilder urlsBuilder = new StringBuilder();
            
            foreach(string game in games)
                urlsBuilder.AppendFormat(URL_TEMPLATE, host + "/games/" + game);

            return String.Format(SITEMAP_TEMPLATE, urlsBuilder.ToString());
        }

        private void SaveSiteMap(string siteMap, string publicFolder)
        {
            string sitemapPath = SiteConfigurationManager.Config.SitemapOptions.GamesSitemapPath;
            string filePath = Path.Join(publicFolder, sitemapPath);
            File.WriteAllText(filePath, siteMap);
        }
    }
}
