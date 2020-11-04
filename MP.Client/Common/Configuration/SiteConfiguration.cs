using Microsoft.Extensions.Configuration;
using MP.Client.Common.Auth;
using MP.Client.Common.Email;
using MP.Core.Common.Configuration;

namespace MP.Client.Common.Configuration
{
    public class SiteConfiguration : DefaultConfiguration
    {
        public string ImageServerUrl { get; set; }
        public AuthOptions AuthOptions { get; set; }
        public MailOptions MailOptions { get; set; }

        public override void UpdateConfiguration(IConfiguration configuration)
        {
            DefaultConnection = configuration.GetConnectionString("DefaultConnection");
            ImageServerUrl = configuration["ImageServer"];
            AuthOptions = configuration.GetSection("AuthOptions").Get<AuthOptions>();
            MailOptions = configuration.GetSection("MailOptions").Get<MailOptions>();
        }
    }
}
