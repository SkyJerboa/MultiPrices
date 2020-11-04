using MP.Core.GameInterfaces;
using System.ComponentModel.DataAnnotations;

namespace MP.Core.Contexts.Games
{
    public class GImage : IImage
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string Name { get; set; }
        public int GameID { get; set; }
        public Game Game { get; set; }
        public MediaType MediaType { get; set; }
        public string Tag { get; set; }
        [Required]
        public string Path { get; set; }
        public int Order { get; set; }
    }
}
