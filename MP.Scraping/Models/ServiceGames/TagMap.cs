using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MP.Scraping.Models.ServiceGames
{
    public class TagMap
    {
        [Key]
        [Column(TypeName = "varchar(50)")]
        public string SourceTag { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string MainTag { get; set; }
    }
}
