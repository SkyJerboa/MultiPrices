namespace MP.Client.SiteModels.GameModels.GameFullInfo
{
    public class Service
    {
        public string Code { get; set; }
        public float? CurrentPrice { get; set; }
        public float? FullPrice { get; set; }
        public float? Discount { get; set; }
        public string CurrencyCode { get; set; }
        public string Link { get; set; }
        public bool Free { get; set; }
        public bool IsPreorder { get; set; }
    }
}
