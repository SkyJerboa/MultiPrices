using System.ComponentModel.DataAnnotations;

namespace MP.Core.Contexts
{
    public class GService
    {
        [Key]
        [MaxLength(5)]
        public string Code { get; set; }
        [Required]
        public string Name { get; set; }
    }
}
