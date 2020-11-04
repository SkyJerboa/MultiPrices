using MP.Core.Common;
using MP.Core.GameInterfaces;
using MP.Core.History;
using System.ComponentModel.DataAnnotations;

namespace MP.Core.Contexts.Games
{
    public class GTranslation : ITranslation
    {
        [Required]
        public string LanguageCode { get; set; }
        public Language Language { get; set; }
        [Required]
        public int GameID { get; set; }
        public Game Game { get; set; }
        [Required]
        [NotCompare]
        public string Key { get; set; }
        public string Value { get; set; }
    }
}