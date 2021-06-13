using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MP.Client.Common.ClientMetadata;
using MP.Client.Common.Configuration;
using MP.Client.Common.Queries;
using MP.Client.SiteModels;
using MP.Client.SiteModels.GameModels.GameWithServices;
using MP.Core.Contexts.Games;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MP.Client.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class GamesController : Controller
    {
        private IDbConnection _connection { get; }

        [FromQuery]
        public string CountryCode { get; set; } = "RU";
        [FromQuery]
        public string CurrencyCode { get; set; } = "RUB";

        [FromQuery]
        public int Page { get; set; } = 1;
        [FromQuery]
        public int GamesPerPage { get; set; } = FilteredPage.GAMES_PER_PAGE;

        [FromQuery]
        public int? Discount { get; set; }
        [FromQuery]
        public int? Price { get; set; }
        [FromQuery]
        public string GameService { get; set; }
        [FromQuery]
        public string Tag { get; set; }
        [FromQuery]
        public string Type { get; set; }
        [FromQuery]
        public string Q { get; set; }
        [FromQuery]
        public string Sort { get; set; }
        [FromQuery]
        public string Developer { get; set; }


        public GamesController(GameContext context)
        {
            _connection = context.Database.GetDbConnection();
        }

        [HttpGet]
        public IActionResult Index()
        {
            NormalizeVariables();

            GamesQueryBuilder queryBuilder = new GamesQueryBuilder();

            var (countQuery, gamesQuery) = queryBuilder
                .SetCountryAndCurrency(CountryCode, CurrencyCode)
                .SetOrder(Sort)
                .SetLimits(Page, GamesPerPage)
                .Build();

            FilteredPage fp = ExecuteQueriesAndCreateObject(countQuery, gamesQuery);
            return new JsonResult(fp);
        }



        [Route("[action]")]
        public IActionResult Filtered()
        {
            NormalizeVariables();

            GamesQueryBuilder gamesQueryBuilder = new GamesQueryBuilder();

            var (countQuery, gamesQuery) = gamesQueryBuilder
                .SetGameServices(GameService)
                .SetMaxPrice(Price)
                .SetMinDiscount(Discount)
                .SetCountryAndCurrency(CountryCode, CurrencyCode)
                .SetTags(Tag)
                .SetDeveloper(Developer)
                .SetSearchPhrase(Q)
                .SetTypes(Type)
                .SetOrder(Sort)
                .SetLimits(Page, GamesPerPage)
                .Build();

            FilteredPage fp = ExecuteQueriesAndCreateObject(countQuery, gamesQuery);
            return new JsonResult(fp);
        }

        FilteredPage ExecuteQueriesAndCreateObject(string countQuery, string dataQuery)
        {
            int count = _connection.QuerySingle<int>(countQuery);
            IEnumerable<ServicePrice> prices = _connection.Query<ServicePrice, AllServicesGame, ServicePrice>(dataQuery,
                (price, game) =>
                {
                    price.Game = game;
                    return price;
                },
                splitOn: "ID");

            Dictionary<int, AllServicesGame> games = new Dictionary<int, AllServicesGame>();
            foreach (var price in prices)
            {
                int gID = price.Game.ID;
                if (games.ContainsKey(gID))
                {
                    games[gID].Prices.Add(price);
                }
                else
                {
                    games.Add(gID, price.Game);
                    price.Game.Prices.Add(price);

                    price.Game.ImageHorizontal = CreateImageUrl(price.Game.ImageHorizontal);
                    price.Game.ImageVertical = CreateImageUrl(price.Game.ImageVertical);
                }
            }

            SearchMetadataCreator metadataCreatot = new SearchMetadataCreator();

            FilteredPage fp = new FilteredPage
            {
                CurrentPage = Page,
                GamesCount = count,
                MaxPages = GetMaxPages(count),
                PerPage = GamesPerPage,
                Meta = metadataCreatot.CreateMetadata(_connection, Request),
                Games = games.Select(i => i.Value)
            };

            return fp;
        }

        public static string CreateImageUrl(string url)
        {
            if (String.IsNullOrEmpty(url))
                return null;

            return SiteConfigurationManager.Config.ImageServerUrl + url;
        }

        private void NormalizeVariables()
        {
            if (GamesPerPage > 100 || GamesPerPage < 3)
                GamesPerPage = FilteredPage.GAMES_PER_PAGE;
            if (Page < 1)
                Page = 1;
        }

        private int GetMaxPages(int count)
        {
            if (count < 1)
                return 0;

            int maxPages = count / GamesPerPage;
            if (count % GamesPerPage != 0)
                maxPages++;

            return maxPages;
        }
    }
}
