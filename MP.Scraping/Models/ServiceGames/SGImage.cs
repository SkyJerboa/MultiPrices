using MP.Core.GameInterfaces;
using System.ComponentModel.DataAnnotations;

namespace MP.Scraping.Models.ServiceGames
{
    public class SGImage : IImage
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string Name { get; set; }
        public int GameID { get; set; }
        public ServiceGame Game { get; set; }
        public MediaType MediaType { get; set; }
        public string Tag { get; set; }
        [Required]
        public string Path { get; set; }
        [Required]
        public string SourceUrl { get; set; }
    }
}
