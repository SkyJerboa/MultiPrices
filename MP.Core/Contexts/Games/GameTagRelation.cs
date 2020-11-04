namespace MP.Core.Contexts.Games
{
    public class GameTagRelation
    {
        public int GameID { get; set; }
        public Game Game { get; set; }

        public int TagID { get; set; }
        public Tag Tag { get; set; }
    }
}
