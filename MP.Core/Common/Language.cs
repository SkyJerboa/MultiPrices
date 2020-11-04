using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MP.Core.Common
{
    public class Language
    {
        [Key]
        [MaxLength(10)]
        public string LangCode { get; set; }
        [Required]
        [MaxLength(100)]
        public string LanguageName { get; set; }
        [Required]
        [Column(TypeName = "varchar(50)")]
        public string NativeName { get; set; }

        //public ICollection<Translation> Translations { get; set; }
    }
}
