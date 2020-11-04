using MP.Client.ComponentModels.Common;
using MP.Client.ComponentModels.DbAdapters;
using MP.Client.ComponentModels.ModelsResponses;
using MP.Client.Controllers;
using MP.Client.SiteModels.GameModels.GameWithServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MP.Client.ComponentModels
{
    public class AllPricesGame : GameModel, IComponentModel
    {
        public int[] GamesIds { get; set; }
        public string PriceInfoCondition { get; set; }

        #region CreateResponseObject
        public object CreateResponseObject(ComponentModelOptions options)
        {
            if (IsEmpryConditions())
                return null;

            GamesQueryOptions gqOptions = new GamesQueryOptions
            {
                Condition = Condition,
                Count = Count,
                CountryCode = options.Country,
                CurrencyCode = options.Currency,
                OrderBy = OrderBy
            };

            GameWithPricesAdapter adapter = new GameWithPricesAdapter(
                connection: options.Connection,
                options: gqOptions,
                priceInfoCondition: PriceInfoCondition,
                gamesIds: GamesIds);

            var prices = adapter.ReadData();
            var games = ConvertPricesToGames(prices);
            

            var responseObject = new AllPricesGameResponse
            {
                Title = options.Title,
                Link = Link,
                Type = nameof(AllPricesGame),
                Data = games
            };

            return responseObject;
        }

        private bool IsEmpryConditions() 
            => Count == 0 || (GamesIds?.Length == 0) && String.IsNullOrEmpty(Condition);

        IEnumerable<AllServicesGame> ConvertPricesToGames(IEnumerable<ServicePrice> prices)
        {
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

                    price.Game.ImageHorizontal = GamesController.CreateImageUrl(price.Game.ImageHorizontal);
                    price.Game.ImageVertical = GamesController.CreateImageUrl(price.Game.ImageVertical);
                }
            }

            return games.Select(i => i.Value);
        }
        #endregion
    }
}
