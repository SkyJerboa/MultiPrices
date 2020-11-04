using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MP.Core.Contexts.Translations
{
    public class Language
    {
        [Key]
        [MaxLength(3)]
        public string LangCode { get; set; }
        public string LanguageName { get; set; }

        public ICollection<Translation> Translations { get; set; }
    }
}
