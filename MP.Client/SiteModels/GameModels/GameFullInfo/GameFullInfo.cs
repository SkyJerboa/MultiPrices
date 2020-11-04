using MP.Core.Contexts.Games;
using MP.Core.Enums;
using System.Collections.Generic;

namespace MP.Client.SiteModels.GameModels.GameFullInfo
{
    public class GameFullInfo
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string LocalizedName { get; set; }
        public string Description { get; set; }
        public string NameID { get; set; }
        public GameEntityType Type { get; set; }
        public GamePlatform Platforms { get; set; }
        public string Publisher { get; set; }
        public string Developer { get; set; }
        public string Brand { get; set; }
        public Dictionary<string, Localization> Languages { get; set; }
        public string ReleaseDate { get; set; }
        public string ImageVertical { get; set; }
        public string ImageHorizontal { get; set; }
        
        public IEnumerable<string> Tags { get; set; }
        public IEnumerable<string> Screenshots { get; set; }
        public IEnumerable<ChildGame> Children { get; set; }
        public IEnumerable<ChildGame> Parents { get; set; }
        public IEnumerable<Service> Services { get; set; }
        public IEnumerable<SystemRequirement> SystemRequirements { get; set; }
    }
}
