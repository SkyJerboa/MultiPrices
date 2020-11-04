using MP.Core.Common;
using MP.Core.History;

namespace MP.Scraping.Models.ServiceGames
{
    public class SGSystemRequirement : SystemRequirement, IVersioning
    {
        public ServiceGame Game { get; set; }
    }
}
