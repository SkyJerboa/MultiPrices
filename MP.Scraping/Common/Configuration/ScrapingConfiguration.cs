﻿using Microsoft.Extensions.Configuration;
using MP.Core.Common.Configuration;
using MP.Scraping.Common.Ftp;

namespace MP.Scraping.Common.Configuration
{
    public class ScrapingConfiguration : DefaultConfiguration
    {
        public string SiteConnection { get; private set; }

        public ImageConfiguration ImageConfiguration { get; private set; }
        public FtpConfiguration FtpConfiguration { get; private set; }

        public override void UpdateConfiguration(IConfiguration configuration)
        {
            DefaultConnection = configuration.GetConnectionString("Services");
            SiteConnection = configuration.GetConnectionString("Production");
            ImageConfiguration = configuration.GetSection("ImageConfiguration").Get<ImageConfiguration>();
            FtpConfiguration = configuration.GetSection("FtpConfiguration").Get<FtpConfiguration>();
        }
    }
}
