using MP.Client.ComponentModels.Common;
using MP.Client.ComponentModels.DbAdapters;
using MP.Client.ComponentModels.ModelsResponses;
using MP.Client.Controllers;
using MP.Client.SiteModels.GameModels;
using MP.Client.SiteModels.GameModels.GameWithServices;
using System.Collections.Generic;
using System.Linq;

namespace MP.Client.ComponentModels
{
    public class AllPricesGameGroup : IComponentModel
    {
        public int GamesPerGroup { get; set; }
        public GameGroup[] GameGroups { get; set; }

        #region CreateResponseObject
        public object CreateResponseObject(ComponentModelOptions options)
        {
            if (GameGroups.Length == 0)
                return null;

            GamesQueryOptions gqOptions = new GamesQueryOptions
            {
                Count = GamesPerGroup,
                CountryCode = options.Country,
                CurrencyCode = options.Currency
            };

            GameWithPricesGroupAdapter adapter = new GameWithPricesGroupAdapter(
                connection: options.Connection,
                options: gqOptions,
                gameGroups: GameGroups);


            var prices = adapter.ReadData();
            var gamesGroups = ConvertPricesToGameGroups(prices);

            var responseObject = new AllPricesGameGroupResponse
            {
                Title = options.Title,
                Type = nameof(AllPricesGameGroup),
                Data = gamesGroups
            };

            return responseObject;
        }

        private IEnumerable<AllPricesGameGroupResponse.GroupModel> ConvertPricesToGameGroups(
            IEnumerable<ServicePrice> prices)
        {
            Dictionary<string, GroupedWithServiceGame> games = new Dictionary<string, GroupedWithServiceGame>();
            foreach (var price in prices)
            {
                string key = price.Game.ID.ToString() + (price.Game as GroupedWithServiceGame).GroupName;

                if (!games.ContainsKey(key))
                {
                    games.Add(key, price.Game as GroupedWithServiceGame);
                    price.Game.Prices.Add(price);

                    price.Game.ImageHorizontal = GamesController.CreateImageUrl(price.Game.ImageHorizontal);
                    price.Game.ImageVertical = GamesController.CreateImageUrl(price.Game.ImageVertical);
                }
                else
                {
                    games[key].Prices.Add(price);
                }
            }

            return games
                .Select(i => i.Value)
                .GroupBy(i => i.GroupName)
                .Select(i => new AllPricesGameGroupResponse.GroupModel { GroupName = i.Key, Data = i });
        }
        #endregion
    }

    public class GameGroup
    {
        public string Name { get; set; }
        public string Link { get; set; }
        public string Condition { get; set; }
        public string PriceInfoCondition { get; set; }
        public string OrderBy { get; set; }
        public int[] GameIds { get; set; }

    }
}
