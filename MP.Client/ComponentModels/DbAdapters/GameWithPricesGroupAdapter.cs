using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using MP.Client.ComponentModels.Common;
using MP.Client.SiteModels.GameModels;
using MP.Client.SiteModels.GameModels.GameWithServices;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MP.Client.ComponentModels.DbAdapters
{
    public class GameWithPricesGroupAdapter : GameWithPricesAdapter
    {
        protected const string PRICES_WITH_GAMES_GROUPS_QUERY = @"
            WITH g AS({0})
            SELECT pi.""ServiceCode"", pi.""CountryCode"", pi.""CurrencyCode"", pi.""CurrentPrice"", pi.""FullPrice"", pi.""Discount"", pi.""IsPreorder"",
                g.""ID"", g.""Name"", g.""NameID"", g.""GameType"", g.""ReleaseDate"", g.""ImageHorizontal"", g.""ImageVertical"", '{3}' as ""GroupName""
            FROM g
            LEFT JOIN ""PriceInfos"" as pi ON pi.""GameID"" = g.""ID""
            WHERE pi.""IsAvailable"" AND pi.""CountryCode"" = '{1}' AND pi.""CurrencyCode"" = '{2}'";

        GameGroup[] _gameGroups;

        public GameWithPricesGroupAdapter(
            IDbConnection connection,
            GamesQueryOptions options,
            GameGroup[] gameGroups
            ) : base(connection, options, null, null)
        {
            _gameGroups = gameGroups;
        }

        public override IEnumerable<ServicePrice> ReadData()
        {
            string query = CreateQuery();
            return ReadPricesWithGames<GroupedWithServiceGame>(query);
        }

        protected override string CreateQuery()
        {
            StringBuilder queryBuilder = new StringBuilder();
            bool firstGroup = true;

            foreach (var ggm in _gameGroups)
            {
                if (!firstGroup)
                    queryBuilder.Append("\nUNION\n");

                _options.Condition = ggm.Condition;
                _options.OrderBy = ggm.OrderBy;
                _priceInfoCondition = ggm.PriceInfoCondition;
                _gamesIds = ggm.GameIds;

                queryBuilder.Append('(');

                string query = base.CreateQuery();
                query = String.Format(PRICES_WITH_GAMES_GROUPS_QUERY,
                    query, _options.CountryCode, _options.CurrencyCode, ggm.Name);

                queryBuilder.Append(query);
                queryBuilder.Append(')');

                firstGroup = false;
            }

            return queryBuilder.ToString();
        }
    }
}
