using System.ComponentModel.DataAnnotations;

namespace MP.Core.Common
{
    public class Translation
    {
        [Required]
        public string LanguageCode { get; set; }
        public Language Language { get; set; }
        [Required]
        public string Key { get; set; }
        [Required]
        public string Value { get; set; }
    }
}
