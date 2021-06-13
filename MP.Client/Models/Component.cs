using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MP.Client.Models
{
    public class Component
    {
        public string Name { get; set; }
        public string Title { get; set; }
        [Required]
        public string Model { get; set; }
        [Column(TypeName = "jsonb")]
        public string Data { get; set; }
    }
}
