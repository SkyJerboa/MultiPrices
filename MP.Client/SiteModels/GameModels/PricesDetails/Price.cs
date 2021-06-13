using Newtonsoft.Json;
using System;

namespace MP.Client.SiteModels.GameModels.PricesDetails
{
    public class Price
    {
        [JsonIgnore]
        public string ServiceCode { get; set; }
        public float? Value { get; set; }
        public float? Discount { get; set; }
        public DateTime ChangingDate { get; set; }
    }
}
