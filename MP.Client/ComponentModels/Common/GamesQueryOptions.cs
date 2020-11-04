namespace MP.Client.ComponentModels.Common
{
    public class GamesQueryOptions
    {
        public string Condition { get; set; }
        public int Count { get; set; }
        public string CountryCode { get; set; }
        public string CurrencyCode { get; set; }
        public string OrderBy { get; set; }
    }
}
