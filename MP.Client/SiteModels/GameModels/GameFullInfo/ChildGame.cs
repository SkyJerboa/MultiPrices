using MP.Core.Contexts.Games;
using System.Collections.Generic;

namespace MP.Client.SiteModels.GameModels.GameFullInfo
{
    public class ChildGame
    {
        public string Name { get; set; }
        public string NameID { get; set; }
        public GameEntityType Type { get; set; }
        public string[] Services { get; set; }
        public string ImageVertical { get; set; }
        public string ImageHorizontal { get; set; }
    }
}
