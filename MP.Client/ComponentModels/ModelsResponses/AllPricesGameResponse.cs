using MP.Client.Models;
using MP.Client.SiteModels.GameModels.GameWithServices;
using System.Collections.Generic;

namespace MP.Client.ComponentModels.ModelsResponses
{
    public class AllPricesGameResponse
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Type { get; set; }
        public IEnumerable<AllServicesGame> Data { get; set; }
    }
}
