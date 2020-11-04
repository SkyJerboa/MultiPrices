using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MP.Core.Common
{
    public class Currency
    {
        [Key]
        [MaxLength(3)]
        public string Code { get; set; }
        public string Name { get; set; }
        [Column(TypeName = "varchar(5)")]
        public string Symbol { get; set; }
    }
}
