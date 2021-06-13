using System.Collections.Generic;

namespace MP.Client.SiteModels.GameModels.PricesDetails
{
    public class RegionalPriceHistory
    {
        public string Country { get; set; }
        public string Currency { get; set; }
        public List<ServicePrices> PriceHistory { get; set; }
    }
}
