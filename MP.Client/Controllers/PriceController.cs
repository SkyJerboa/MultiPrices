using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MP.Client.Common.JsonResponses;
using MP.Client.SiteModels.GameModels.PricesDetails;
using MP.Core.Contexts.Games;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Price = MP.Client.SiteModels.GameModels.PricesDetails.Price;

namespace MP.Client.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PriceController : Controller
    {
        private const string QUERY_PRICES = @"
            SELECT pi.""ServiceCode"",  p.""CurrentPrice"" as ""Value"", p.""Discount"", p.""ChangingDate"" FROM ""PriceInfos"" as pi
            JOIN ""Prices"" as p ON pi.""ID"" = p.""ServicePriceID""
            WHERE ""IsAvailable"" AND NOT ""IsIgnore"" AND ""GameID"" = @GameId AND ""CountryCode"" = @CountryCode AND ""CurrencyCode"" = @CurrencyCode";

        private IDbConnection _connection { get; }

        public PriceController(GameContext context)
        {
            _connection = context.Database.GetDbConnection();
        }

        [FromQuery]
        public int ID { get; set; }
        [FromQuery]
        public string CountryCode { get; set; } = "RU";
        [FromQuery]
        public string CurrencyCode { get; set; } = "RUB";

        public IActionResult Index()
        {
            if (ID == 0)
                return new JsonErrorResult("Misiing ID", @"Required parameter ""ID"" missing");

            IEnumerable<Price> prices = _connection.Query<Price>(QUERY_PRICES, new { GameId = ID, CountryCode, CurrencyCode });

            if (prices.Count() == 0)
                return new JsonErrorResult("No result", "Price history was not found");

            
            RegionalPriceHistory regionalPriceHistory = new RegionalPriceHistory
            {
                Country = CountryCode,
                Currency = CurrencyCode,
                PriceHistory = new List<ServicePrices>()
            };

            var groupedPrices = prices.GroupBy(i => i.ServiceCode);
            foreach(var priceGroup in groupedPrices)
            {
                ServicePrices servicePrices = new ServicePrices { ServiceCode = priceGroup.Key };
                servicePrices.Prices = priceGroup.ToList();
                regionalPriceHistory.PriceHistory.Add(servicePrices);
            }

            return new JsonResult(regionalPriceHistory);
        }
    }
}
