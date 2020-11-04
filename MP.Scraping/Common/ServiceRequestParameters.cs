namespace MP.Scraping.Common
{
    public struct ServiceRequestOptions
    {
        public string CountryCode { get; }
        public string CurrencyCode { get; }
        public string LanguageCode { get; }
        public bool IsOnlyPrice { get; }
        public bool IsTesting { get; }
        public bool IsAutonomusRun { get; }
        public string UserName { get; }

        public ServiceRequestOptions(string country, string currency, string lang, 
            bool isOnlyPrice, bool isTesting = false, string userName = null, bool isAutonomusRun = false)
        {
            CountryCode = country;
            CurrencyCode = currency;
            LanguageCode = lang;
            IsOnlyPrice = isOnlyPrice;
            IsTesting = isTesting;
            UserName = userName;
            IsAutonomusRun = isAutonomusRun;
        }
    }
}
