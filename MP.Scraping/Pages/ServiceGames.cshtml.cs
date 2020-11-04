using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MP.Scraping.Models.ServiceGames;
using MP.Scraping.Models.Services;
using MP.Scraping.Common.Helpers;
using Microsoft.EntityFrameworkCore;
using MP.Core.Common.Heplers;

namespace MP.Scraping.Pages
{
    public class ServiceGamesModel : PageModel
    {
        private ServiceGameContext _context;

        public string GameServiceCode { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int MaxPages { get; private set; }
        public string ServiceName { get; private set; }
        public List<ServiceGame> Games { get; set; }

        [BindProperty(SupportsGet = true)]
        [FromQuery]
        public int? page { get; set; }

        [BindProperty(SupportsGet = true)]
        [FromQuery]
        public string q { get; set; }

        public ServiceGamesModel(ServiceGameContext context)
        {
            _context = context;
        }

        public void OnGet(string gameServiceCode)
        {
            GameServiceCode = gameServiceCode;
            using (ServiceContext service = new ServiceContext())
                ServiceName = service.Services.FirstOrDefault(i => i.Code == GameServiceCode)?.Name;

            //_context = new ServiceGameTranslation(ServiceName);

            if (page != null && page > 0)
                CurrentPage = (int)page;

            if (!String.IsNullOrEmpty(q))
            {
                q = q.CreateOneLineString();
                //MaxPages = _context.Games
                //    .Where(i => EF.Functions.Like(i.Name.CreateOneLineString(), String.Format("%{0}%", q)))
                //    .Count();
                
                Games = _context
                    .GetServiceGamesWithoutTracking(GameServiceCode)
                    .Where(i => EF.Functions.Like(i.Name.CreateOneLineString(), String.Format("%{0}%", q)))
                    //.Skip((CurrentPage - 1) * 50)
                    //.Take(50)
                    .ToList();
            }
            else
            {
                var gamesQuery = _context.Games.Where(i => i.ServiceCode == GameServiceCode.ToUpper());

                MaxPages = gamesQuery.Count() / 50 + 1;

                Games = gamesQuery
                    .OrderBy(i => i.ID)
                    .Skip((CurrentPage - 1) * 50)
                    .Take(50)
                    .ToList();
            }
        }
    }
}