using MP.Core.Contexts.Games;
using System.Collections.Generic;

namespace MP.Client.SiteModels.GameModels.GameWithServices
{
    //Multi Services Game
    public class AllServicesGame
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string NameID { get; set; }
        public GameEntityType GameType { get; set; }
        public string ReleaseDate { get; set; }
        public string ImageHorizontal { get; set; }
        public string ImageVertical { get; set; }
        public ICollection<ServicePrice> Prices { get; set; } = new List<ServicePrice>();
    }
}
