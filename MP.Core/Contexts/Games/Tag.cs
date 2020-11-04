using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MP.Core.Contexts.Games
{
    public class Tag
    {
        [Key]
        public int ID { get; set; }
        [Required]
        [Column(TypeName = "varchar(50)")]
        public string Name { get; set; }

        public ICollection<GameTagRelation> Games { get; set; }
    }
}
