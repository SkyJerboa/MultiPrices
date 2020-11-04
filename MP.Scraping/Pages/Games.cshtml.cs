using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MP.Core.Contexts.Games;
using MP.Scraping.Common.Extensions;
using MP.Scraping.DTOs;
using MP.Scraping.Models.Games;
using MP.Scraping.Common.Constants;
using System.Security.Claims;
using MP.Core.Common.Heplers;
using System.Text.RegularExpressions;

namespace MP.Scraping.Pages
{
    public class GamesModel : PageModel
    {
        private const string DELETED_NAMEID_PATTERN = @"\S+_d\d+$";

        private readonly GameWithHistoryContext _context;
        public List<GameDTO> Games { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int GamesCount { get; private set; }
        public int MaxPages { get; private set; }

        [BindProperty(SupportsGet =true)]
        [FromQuery]
        public string Q { get; set; }
        [FromQuery]
        public GameStatus? Status { get; set; }

        public GamesModel(GameWithHistoryContext db)
        {
            _context = db;
        }

        public void OnGet([FromQuery]int? page)
        {
            IQueryable<Game> games = _context.Games;

            if (Status != null)
            {
                games = games.Where(i => i.Status == Status);
            }

            if (!String.IsNullOrEmpty(Q))
            {
                games = games.Where(i => EF.Functions.Like(i.NameID, String.Format("%{0}%", Q.CreateOneLineString())));
            }

            GamesCount = games.Count();
            MaxPages = GamesCount / 50 + 1;

            if (page != null && page > 0 && page <= MaxPages)
                CurrentPage = (int)page;

            Games = GetGames(games, CurrentPage).ToList();
        }

        public ActionResult OnPost()
        {
            string role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!UserRoles.ALLOWED_ROLES_EDITING.Contains(role))
                return Forbid();

            string parentGameId = Request.Form["parent"];
            string[] mergedGames = Request.Form["merged[]"].ToArray();
            int parentId = Int32.Parse(parentGameId);

            List<int> ids = new List<int>() { parentId };
            ids.AddRange(mergedGames.Select(i => Int32.Parse(i)));

            List<Game> games = _context.Games
                .Include(i => i.Children)
                .Include(i => i.Parents)
                .Include(i => i.PriceInfos)
                .Include(i => i.Images)
                .Include(i => i.SystemRequirements)
                .Include(i => i.Translations)
                .Include(i => i.Tags)
                .Where(i => ids.Contains(i.ID))
                .ToList();


            Game parentGame = games.First(i => i.ID == parentId);
            games.Remove(parentGame);

            parentGame.MergeGames(games);
            SetDeletedGamesNameIDs(games);

            _context.SaveChanges(User.Identity.Name);

            return StatusCode(200);
        }

        private IQueryable<GameDTO> GetGames(IQueryable<Game> games, int page)
        {
            return games
                .Include(i => i.Children)
                    .ThenInclude(i => i.Child)
                .Include(i => i.PriceInfos)
                .Skip((page - 1) * 50)
                .Take(50)
                .Select(i =>
                    new GameDTO
                    {
                        ID = i.ID,
                        Name = i.Name,
                        NameID = i.NameID,
                        GameType = i.GameType,
                        ServiceCodes = i.GameServicesCodes,
                        Image = i.ImagesPath,
                        Status = i.Status,
                        Prices = i.PriceInfos.Select(p =>
                            new ServicePriceDTO
                            {
                                ID = p.ID,
                                CountryCode = p.CountryCode,
                                CurrentPrice = p.CurrentPrice,
                                Discount = p.Discount,
                                FullPrice = p.FullPrice,
                                ServiceCode = p.ServiceCode
                            }
                        ),
                        Children = i.Children.Select(c =>
                        new GameChildDTO
                        {
                            ID = c.Child.ID
                        })
                    });
        }

        private void SetDeletedGamesNameIDs(List<Game> deletedGames)
        {
            foreach(Game game in deletedGames)
            {
                if (game.Status != GameStatus.Deleted || Regex.IsMatch(game.NameID, DELETED_NAMEID_PATTERN))
                    continue;

                int underscoreIndex = game.NameID.IndexOf("_");
                string cleanNameId = (underscoreIndex == -1)
                    ? game.NameID
                    : game.NameID.Substring(0, underscoreIndex);

                int deletedIndex = 1;
                string deletedName = null;

                do
                {
                    deletedName = $"{cleanNameId}_d{deletedIndex}";
                    deletedIndex++;
                }
                while (_context.Games.Any(i => i.NameID == deletedName));

                game.NameID = deletedName;
            }
        }

        //private bool LikeQuery(Game game) => EF.Functions.Like(game.NameID, String.Format("%{0}%", Q.CreateOneLineString()));
    }
}