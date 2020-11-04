using MP.Core.Contexts.Games;
using System.ComponentModel.DataAnnotations;

namespace MP.Core.Contexts.Translations
{
    public class Translation
    {
        [Required]
        public string LanguageCode { get; set; }
        public Language Language { get; set; }
        //public int GameID { get; set; }
        //public Game Game { get; set; }
        [Required]
        public string Key { get; set; }
        [Required]
        public string Value { get; set; }
    }
}
