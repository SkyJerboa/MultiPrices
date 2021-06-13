using MP.Client.Common.Configuration;
using MP.Client.Common.Scheduler;
using System;
using System.Data;

namespace MP.Client.Common.Sitemap
{
    public class SitemapScheduler : IScheduler
    {
        public TimeSpan Interval { get; }
        public DateTime StartTime { get; }
        
        private GamesSitemapBuilder _gamesSitemapBuilder;
        private FilterSitemapBuilder _filtersSitemapBuilder;
        private IDbConnection _connection;
        private string _publicFolder;

        public SitemapScheduler(IDbConnection connection, string publicFolder)
        {
            _connection = connection;
            _publicFolder = publicFolder;
            _gamesSitemapBuilder = new GamesSitemapBuilder();
            _filtersSitemapBuilder = new FilterSitemapBuilder();
            SitemapConfiguration sitemapConfig = SiteConfigurationManager.Config.SitemapOptions;
            Interval = TimeSpan.FromHours(sitemapConfig.CreationIntevalFromHours);
            StartTime = DateTime.Today.AddHours(sitemapConfig.CreationHour);
        }

        public Action CreateSchedulerAction() => BuildSitemapAcrion;

        private void BuildSitemapAcrion()
        {
            _gamesSitemapBuilder.BuildAndSaveSiteMap(_connection, _publicFolder);
            _filtersSitemapBuilder.BuildAndSaveSiteMap(_connection, _publicFolder);
        }
    }
}
