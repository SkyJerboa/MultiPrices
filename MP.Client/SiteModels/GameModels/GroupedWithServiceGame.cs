using MP.Client.SiteModels.GameModels.GameWithServices;
using Newtonsoft.Json;

namespace MP.Client.SiteModels.GameModels
{
    public class GroupedWithServiceGame : AllServicesGame
    {
        [JsonIgnore]
        public string GroupName { get; set; }
    }
}
