using MP.Client.ComponentModels.Common;
using MP.Client.ComponentModels.DbAdapters;
using MP.Client.ComponentModels.ModelsResponses;
using MP.Client.Controllers;
using MP.Client.SiteModels.GameModels;
using System;
using System.Collections.Generic;

namespace MP.Client.ComponentModels
{
    public class SinglePriceGame : GameModel, IComponentModel
    {
        public Dictionary<int, string> GamesFromService { get; set; }

        #region CreateResponseObject
        public object CreateResponseObject(ComponentModelOptions options)
        {
            if (IsEmptyConditions())
                return null;

            GamesQueryOptions gqOptions = new GamesQueryOptions
            {
                Condition = Condition,
                Count = Count,
                CountryCode = options.Country,
                CurrencyCode = options.Currency,
                OrderBy = OrderBy
            };

            SinglePriceAdapter adapter = new SinglePriceAdapter(
                connection: options.Connection,
                options: gqOptions,
                gamesWithSpecificService: GamesFromService);

            IEnumerable<OneServiceGame> games = adapter.ReadData();
            SetImagesToGames(games);
            
            
            SinglePriceGameResponse responseObject = new SinglePriceGameResponse
            {
                Title = options.Title,
                Link = Link,
                Type = nameof(SinglePriceGame),
                Data = games
            };

            return responseObject;
        }

        private bool IsEmptyConditions()
            =>  Count == 0 || GamesFromService.Count == 0 && String.IsNullOrEmpty(Condition);

        private void SetImagesToGames(IEnumerable<OneServiceGame> games)
        {
            foreach (var g in games)
            {
                g.ImageHorizontal = GamesController.CreateImageUrl(g.ImageHorizontal);
                g.ImageVertical = GamesController.CreateImageUrl(g.ImageVertical);
            }
        }
        #endregion
    }
}
