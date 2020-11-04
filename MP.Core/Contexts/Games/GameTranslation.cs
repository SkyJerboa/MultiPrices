using MP.Core.Localization;

namespace MP.Core.Contexts.Games
{
    public class GameTranslation : Translation
    {
        public int ID { get; set; }
        public int GameID { get; set; }
        public Game Game { get; set; }
    }
}
