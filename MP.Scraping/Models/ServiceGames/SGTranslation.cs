using MP.Core.History;
using MP.Core.Common;
using System.ComponentModel.DataAnnotations;
using MP.Core.GameInterfaces;

namespace MP.Scraping.Models.ServiceGames
{
    public class SGTranslation : IVersioning, ITranslation
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string LanguageCode { get; set; }
        public Language Language { get; set; }
        [Required]
        [NotCompare]
        public int GameID { get; set; }
        public ServiceGame Game { get; set; }
        [Required]
        [NotCompare]
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
