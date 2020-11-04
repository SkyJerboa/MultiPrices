using Microsoft.Extensions.Configuration;
using MP.Core.Common.Configuration;

namespace MP.Client.Common.Configuration
{
    public class SiteConfigurationManager : ConfigurationManager<SiteConfiguration>
    {
        public SiteConfigurationManager(IConfiguration configuration) 
            : base(configuration)
        { }
}
}
