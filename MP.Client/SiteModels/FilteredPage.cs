using MP.Client.SiteModels.GameModels.GameWithServices;
using System.Collections.Generic;

namespace MP.Client.SiteModels
{
    public class FilteredPage
    {
        public const int GAMES_PER_PAGE = 48;

        public int CurrentPage { get; set; }
        public int MaxPages { get; set; }
        public int GamesCount { get; set; }
        public int PerPage { get; set; }
        public IEnumerable<AllServicesGame> Games { get; set; }
    }
}
