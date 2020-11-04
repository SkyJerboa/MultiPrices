using Microsoft.Extensions.Configuration;
using MP.Core.Common.Configuration;
using System;

namespace MP.Scraping.Common.Configuration
{
    public class ScrapingConfigurationManager : ConfigurationManager<ScrapingConfiguration>
    {
        public ScrapingConfigurationManager(IConfiguration configuration) 
            : base(configuration)
        { }
    }
}
