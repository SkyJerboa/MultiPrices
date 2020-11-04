using MP.Core.GameInterfaces;

namespace MP.Core.Contexts.Games
{
    public class GameRelationship : IGameRelationship
    {
        public int ParentID { get; set; }
        public Game Parent { get; set; }

        public int ChildID { get; set; }
        public Game Child { get; set; }
    }
}
