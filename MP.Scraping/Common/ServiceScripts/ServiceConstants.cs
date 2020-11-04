using System.Collections.Immutable;
using System.Collections.Generic;

namespace MP.Scraping.Common.ServiceScripts
{
    public class ServiceConstants
    {
        public ImmutableDictionary<string, string> LanguagesMap { get; }
        public ImmutableDictionary<string, string> CountryCodesMap { get; }
        public ImmutableDictionary<string, string> CurrencyCodesMap { get; }
        public string DateFormat { get; }
        public float PriceMultiplier { get; }

        public CountrySettings[] SupportedCountries { get; }

        public ServiceConstants(
            string dateFormat = null, 
            float priceMultiplier = 1, 
            Dictionary<string, string> languagesMap = null, 
            Dictionary<string, string> countryCodesMap = null, 
            Dictionary<string, string> currencyCodesMap = null, 
            CountrySettings[] supportedCountries = null)
        {
            DateFormat = dateFormat;
            PriceMultiplier = priceMultiplier;
            LanguagesMap = languagesMap?.ToImmutableDictionary();
            CountryCodesMap = countryCodesMap?.ToImmutableDictionary();
            CurrencyCodesMap = currencyCodesMap?.ToImmutableDictionary();
            SupportedCountries = supportedCountries;
        }
    }

    public class CountrySettings
    {
        public string CountryCode;
        public ImmutableArray<string> CurrencyCodes;
        public ImmutableArray<string> LanguagesCodes;

        public CountrySettings(string countryCode = null, string[] currencyCodes = null, string[] languagesCodes = null)
        {
            CountryCode = countryCode;
            CurrencyCodes = currencyCodes?.ToImmutableArray() ?? new ImmutableArray<string>();
            LanguagesCodes = languagesCodes?.ToImmutableArray() ?? new ImmutableArray<string>();
        }
    }
}
