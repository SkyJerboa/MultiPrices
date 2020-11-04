using MP.Client.Models;
using MP.Client.SiteModels.GameModels;
using System.Collections.Generic;

namespace MP.Client.ComponentModels.ModelsResponses
{
    public class SinglePriceGameResponse
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Type { get; set; }
        public IEnumerable<OneServiceGame> Data { get; set; }
    }
}
