using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MP.Client.Common.Configuration;
using MP.Client.Common.Constants;
using MP.Client.SiteModels;
using MP.Client.SiteModels.GameModels.GameWithServices;
using MP.Core.Common.Heplers;
using MP.Core.Contexts.Games;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MP.Client.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class GamesController : Controller
    {
        private const string GAMES_COUNT_QUERY = @"
            SELECT COUNT(""ID"") FROM ""Games"" WHERE ""Status"" = 'Completed'";

        private const string TAG_CONDITION = @"EXISTS (
            SELECT 1 FROM ""GameTagRelations"" WHERE ""Games"".""ID"" = ""GameID"" AND ""TagID"" IN (
                SELECT ""ID"" FROM ""Tags"" WHERE ""Name"" in ({0})))";

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
        public bool WithoutPrices { get; set; }
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
            int offset = (Page - 1) * GamesPerPage;
            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.Append(Queries.GAMES_QUERY);

            string withoutPricesCondition = @" AND ""CurrentPrice"" IS NOT NULL";
            string piWhere = String.Format(Queries.COUNTRY_AND_CURRENCY_CONDITION, CountryCode, CurrencyCode, withoutPricesCondition);
            string countQuery = GAMES_COUNT_QUERY + "\n" + piWhere;

            queryBuilder.Append(piWhere);
            queryBuilder.Append("\n");
            queryBuilder.Append($"ORDER BY {GetOrderBy()}");
            queryBuilder.Append($"\nLIMIT {GamesPerPage} OFFSET {offset}");

            string query = String.Format(Queries.PRICES_WITH_GAMES_QUERY, queryBuilder.ToString(), CountryCode, CurrencyCode);

            FilteredPage fp = ExecuteQueriesAndCreateObject(countQuery, query);
            return new JsonResult(fp);
        }



        [Route("[action]")]
        public IActionResult Filtered()
        {
            NormalizeVariables();

            StringBuilder whereBuilder = new StringBuilder();

            string piWhere = "";

            if (!WithoutPrices)
            {
                piWhere += @" AND ""CurrentPrice"" IS NOT NULL";
            }

            if (GameService != null)
            {
                string[] gameServices = GameService.ToUpper().Split(',');
                piWhere += $@" AND ""ServiceCode"" in('" + String.Join("','", gameServices) + "')";
            }

            if(Price != null)
            {
                piWhere += $@" AND ""CurrentPrice"" <= {Price}";
            }

            if(Discount != null)
            {
                piWhere += $@" AND ""Discount"" >= {Discount}";
            }

            whereBuilder.Append(String.Format(Queries.COUNTRY_AND_CURRENCY_CONDITION, CountryCode, CurrencyCode, piWhere));

            if (Tag != null)
            {
                whereBuilder.Append("\nAND ");
                string[] tags = Tag.Split(',');
                string tagCondition = String.Format(TAG_CONDITION, $"'{String.Join(',', tags)}'");
                whereBuilder.Append(tagCondition);
            }

            if (Developer != null)
            {
                whereBuilder.Append("\nAND");
                whereBuilder.Append($@"""Games"".""Developer"" = '{Developer}'");
            }

            if (Q != null)
            {
                string nameId = StringHelper.CreateOneLineString(Q);
                whereBuilder.Append("\nAND ");
                whereBuilder.Append($@"""Games"".""NameID"" like '%{nameId}%'");
            }

            string typesCondition = GetTypeCondition();
            whereBuilder.Append("\nAND ");
            whereBuilder.Append(typesCondition);

            string orderBy = GetOrderBy();

            string countQuery = GAMES_COUNT_QUERY + "\n" + whereBuilder.ToString();

            whereBuilder.Append($"\nORDER BY {orderBy}");

            int offset = (Page - 1) * GamesPerPage;
            whereBuilder.Append($"\nLIMIT {GamesPerPage} OFFSET {offset}");
            
            string query = $"{Queries.GAMES_QUERY}\n{whereBuilder}";
            query = String.Format(Queries.PRICES_WITH_GAMES_QUERY, query, CountryCode, CurrencyCode);

            FilteredPage fp = ExecuteQueriesAndCreateObject(countQuery, query);
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

            FilteredPage fp = new FilteredPage
            {
                CurrentPage = Page,
                GamesCount = count,
                MaxPages = (count / GamesPerPage) + 1,
                PerPage = GamesPerPage,
                Games = games.Select(i => i.Value)
            };

            return fp;
        }

        private void NormalizeVariables()
        {
            if (GamesPerPage > 100 || GamesPerPage < 1)
                GamesPerPage = FilteredPage.GAMES_PER_PAGE;
            if (Page < 1)
                Page = 1;
        }

        private string GetTypeCondition()
        {
            if (String.IsNullOrEmpty(Type))
                return $@"""Games"".""GameType"" in ('{GameEntityType.FullGame}','{GameEntityType.Edition}')";

            string[] typesSrc = Type.Split(',');
            string[] typesSearch = new string[typesSrc.Length];

            for (int i = 0; i < typesSrc.Length; i++)
            {
                string type = typesSrc[i].ToLower();
                switch (type)
                {
                    case "game": typesSearch[i] = GameEntityType.FullGame.ToString(); break;
                    case "edition": typesSearch[i] = GameEntityType.Edition.ToString(); break;
                    case "dlc": typesSearch[i] = GameEntityType.DLC.ToString(); break;
                    case "pack": typesSearch[i] = GameEntityType.Pack.ToString(); break;
                    case "demo": typesSearch[i] = GameEntityType.Demo.ToString(); break;
                }
            }

            if (typesSearch.Length == 1)
                return $@"""Games"".""GameType"" = '{typesSearch[0]}'";
            else
                return $@"""Games"".""GameType"" in ('{String.Join("','", typesSearch)}')";
        }

        private string GetOrderBy()
        {
            string orderBy = @"""Games"".""Order"" DESC";
            if (!String.IsNullOrEmpty(Sort))
            {
                switch (Sort.ToLower())
                {
                    case "releasedate":
                        orderBy = @"""Games"".""ReleaseDate""";
                        break;
                    case "releasedatedesc":
                        orderBy = @"""Games"".""ReleaseDate"" DESC";
                        break;
                    case "alphabet":
                        orderBy = @"""Games"".""NameID""";
                        break;
                    case "price":
                        orderBy = @"(SELECT MIN(""CurrentPrice"") FROM ""PriceInfos"" WHERE ""GameID"" = ""Games"".""ID"")";
                        break;
                    case "pricedesc":
                        orderBy = @"(SELECT MIN(""CurrentPrice"") FROM ""PriceInfos"" WHERE ""GameID"" = ""Games"".""ID"") DESC";
                        break;
                }
            }

            return orderBy;
        }

        public static string CreateImageUrl(string url)
        {
            if (String.IsNullOrEmpty(url))
                return null;

            return SiteConfigurationManager.Config.ImageServerUrl + url;
        }
    }
}
