using Microsoft.Extensions.Configuration;
using MP.Client.Common.Auth;
using MP.Client.Common.Email;
using MP.Client.Common.Sitemap;
using MP.Core.Common.Configuration;

namespace MP.Client.Common.Configuration
{
    public class SiteConfiguration : DefaultConfiguration
    {
        public string ImageServerUrl { get; private set; }

        public string Host { get; private set; }
        public AuthOptions AuthOptions { get; set; }
        public MailOptions MailOptions { get; set; }
        public SitemapConfiguration SitemapOptions { get; set; }

        public override void UpdateConfiguration(IConfiguration configuration)
        {
            DefaultConnection = configuration.GetConnectionString("DefaultConnection");
            ImageServerUrl = configuration["ImageServer"];
            Host = configuration["Host"];
            AuthOptions = configuration.GetSection("AuthOptions").Get<AuthOptions>();
            MailOptions = configuration.GetSection("MailOptions").Get<MailOptions>();
            SitemapOptions = configuration.GetSection("SitemapOptions").Get<SitemapConfiguration>();
        }
    }
}
