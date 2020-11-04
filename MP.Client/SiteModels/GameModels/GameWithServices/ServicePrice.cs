using Newtonsoft.Json;

namespace MP.Client.SiteModels.GameModels.GameWithServices
{
    public class ServicePrice
    {
        [JsonIgnore]
        public AllServicesGame Game { get; set; }
        public string ServiceCode { get; set; }
        public string CountryCode { get; set; }
        public string CurrencyCode { get; set; }
        public float? CurrentPrice { get; set; }
        public float? FullPrice { get; set; }
        public float? Discount { get; set; }
        public bool IsPreorder { get; set; }
    }
}
