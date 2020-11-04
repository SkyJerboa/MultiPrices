using MP.Core;
using MP.Core.Contexts;
using MP.Scraping.Common;
using MP.Scraping.Common.Configuration;
using MP.Scraping.Models.ServiceGames;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MP.Scraping.Models.Services
{
    public class Service : GService
    {
        [Required]
        public string TypeName { get; set; }
        public bool Enabled { get; set; }
        
        [Column(TypeName = "time")]
        public TimeSpan ScrapingStartTime { get; set; }
        public int? MinutesInterval { get; set; }
        public ICollection<ServiceRequest> Requests { get; set; }
        public ICollection<ServiceCountry> SupportedCountries { get; set; }
        public ICollection<ServiceGame> Games { get; set; }

        private string _imageDirectory;

        [NotMapped]
        public string ImageDirectory
        {
            get
            {
                if (_imageDirectory == null)
                    _imageDirectory = System.IO.Path.Combine(ScrapingConfigurationManager.Config.ImageFolderPath, Code.ToLower());
                return _imageDirectory;
            }
        }
    }
}
