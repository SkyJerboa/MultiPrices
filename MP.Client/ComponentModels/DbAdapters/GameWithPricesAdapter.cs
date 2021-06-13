using Dapper;
using MP.Client.Common.Queries;
using MP.Client.ComponentModels.Common;
using MP.Client.SiteModels.GameModels.GameWithServices;
using MP.Core.Common.Heplers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MP.Client.ComponentModels.DbAdapters
{
    public class GameWithPricesAdapter : ComponentModelDbAdapter<ServicePrice>
    {
        const string WHERE_GAMEID_IN = @"AND ""ID"" in ({0})";

        protected GamesQueryOptions _options;
        protected string _priceInfoCondition;
        protected int[] _gamesIds;

        public GameWithPricesAdapter(
            IDbConnection connection,
            GamesQueryOptions options,
            string priceInfoCondition,
            int[] gamesIds
            ) : base(connection)
        {
            _options = options;
            _priceInfoCondition = priceInfoCondition;
            _gamesIds = gamesIds;
        }

        public override IEnumerable<ServicePrice> ReadData()
        {
            string query = CreateQuery();
            return ReadPricesWithGames<AllServicesGame>(query);
        }

        protected override string CreateQuery()
        {
            string gamesSelect = CreateGamesSelect();
            return String.Format(ConstantQueries.PRICES_WITH_GAMES_QUERY, 
                gamesSelect, _options.CountryCode, _options.CurrencyCode);
        }

        protected string CreateGamesSelect()
        {
            StringBuilder selectGames = new StringBuilder();
            bool needUnion = false;

            if (IsNeedSpecificCondition())
            {
                selectGames.Append('(');
                selectGames.Append(ConstantQueries.GAMES_QUERY);

                string ids = String.Join(',', _gamesIds);
                string where = String.Format(WHERE_GAMEID_IN, ids);

                selectGames.Append(where);
                selectGames.Append(')');

                needUnion = true;
            }

            if (IsNeedMainCondition())
            {
                if (needUnion)
                    selectGames.Append("\nUNION\n");

                selectGames.Append('(');
                selectGames.Append(ConstantQueries.GAMES_QUERY);

                if (!String.IsNullOrEmpty(_priceInfoCondition))
                    _priceInfoCondition = " AND " + _priceInfoCondition;
                
                string where = String.Format(ConstantQueries.COUNTRY_AND_CURRENCY_CONDITION,
                    _options.CountryCode, _options.CurrencyCode, _priceInfoCondition);

                selectGames.Append(where);

                if (!String.IsNullOrEmpty(_options.Condition))
                {
                    selectGames.Append("\nAND\n");
                    selectGames.Append(_options.Condition);
                }

                if (!String.IsNullOrEmpty(_options.OrderBy))
                {
                    selectGames.Append("\n");
                    _options.OrderBy = _options.OrderBy.SetValuesToString(
                        "countryCode", _options.CountryCode,
                        "currencyCode", _options.CurrencyCode
                    );
                    selectGames.Append($"ORDER BY {_options.OrderBy}");
                }

                int limit = _options.Count - (_gamesIds?.Length ?? 0);

                selectGames.Append("\n");
                selectGames.Append($"LIMIT {limit}");

                selectGames.Append(')');
            }

            return selectGames.ToString();
        }

        private bool IsNeedSpecificCondition() => _gamesIds != null && _gamesIds.Length > 0;

        private bool IsNeedMainCondition() => _gamesIds == null || _gamesIds.Length < _options.Count;

        protected IEnumerable<ServicePrice> ReadPricesWithGames<T>(string query)
            where T : AllServicesGame
        {
            return _dbConnection.Query<ServicePrice, T, ServicePrice>(query, (price, game) =>
            {
                price.Game = game;
                return price;
            }, splitOn: "ID");
        }
    }
}
