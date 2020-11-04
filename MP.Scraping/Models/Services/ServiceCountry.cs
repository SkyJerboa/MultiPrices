using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MP.Scraping.Models.Services
{
    public class ServiceCountry
    {
        [Key]
        public int ID { get; set; }
        public string ServiceCode { get; set; }
        public Service Service { get; set; }
        [Required]
        [MaxLength(5)]
        public string CountryCode { get; set; }
        public IList<string> CurrencyList { get; set; }
        public IList<string> LanguageList { get; set; }
    }
}
