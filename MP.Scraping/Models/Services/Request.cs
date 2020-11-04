using MP.Scraping.Common;
using MP.Scraping.Models.Users;
using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Scraping.Models.Services
{
    public class ServiceRequest
    {
        [Key]
        public int ID { get; set; }
        [MaxLength(5)]
        public string ServiceCode { get; set; }
        public Service Service { get; set; }
        [Required]
        public DateTime StartTime { get; set; }
        [Required]
        public DateTime EndTime { get; set; }
        public bool IsPriceUpdate { get; set; }
        public int New { get; set; } = 0;
        public int Updated { get; set; } = 0;
        public int Deleted { get; set; } = 0;
        public int PriceUpdated { get; set; } = 0;
        public int NoChanged { get; set; } = 0;
        public int Returned { get; set; } = 0;
        public int RequestsCount { get; set; } = 0;
        public string Exceptions { get; set; }
        public string UserName { get; set; }
        [Required]
        public string CountryCode { get; set; }
        [Required]
        public string CurrencyCode { get; set; }
        [Required]
        public string LanguageCode { get; set; }
        public User User { get; set; }

        //необходим для десериализации
        public ServiceRequest()
        { }

        public ServiceRequest(string serviceCode, ServiceRequestOptions options, string user = null)
        {
            ServiceCode = serviceCode;
            IsPriceUpdate = options.IsOnlyPrice;
            CountryCode = options.CountryCode;
            CurrencyCode = options.CurrencyCode;
            LanguageCode = options.LanguageCode;
            UserName = user;
        }
    }
}
