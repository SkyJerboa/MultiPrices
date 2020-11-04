using MP.Core.Contexts.Games;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MP.Scraping.DTOs
{
    public class GameDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string NameID { get; set; }
        public GameEntityType GameType { get; set; }
        public string[] ServiceCodes { get; set; }
        public GameStatus Status { get; set; }
        public string Image { get; set; }
        public IEnumerable<GameChildDTO> Children { get; set; }
        public IEnumerable<ServicePriceDTO> Prices { get; set; }
    }

    public class GameChildDTO
    {
        public int ID { get; set; }
    }

    public class ServicePriceDTO
    {
        public int ID { get; set; }
        [MaxLength(5)]
        public string ServiceCode { get; set; }
        [MaxLength(5)]
        public string CountryCode { get; set; }
        public float? CurrentPrice { get; set; }
        public float? FullPrice { get; set; }
        public float? Discount { get; set; }
    }
}
