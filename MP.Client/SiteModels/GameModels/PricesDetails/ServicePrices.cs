using System.Collections.Generic;

namespace MP.Client.SiteModels.GameModels.PricesDetails
{
    public class ServicePrices
    {
        public string ServiceCode { get; set; }
        public List<Price> Prices { get; set; }
    }
}
