using MP.Client.Common.Constants;
using MP.Core.Common.Heplers;
using MP.Core.Contexts.Games;
using System;
using System.Collections.Generic;
using System.Text;

namespace MP.Client.Common.Queries
{
    public class GamesQueryBuilder
    {
        private const string GAMES_COUNT_QUERY = @"
            SELECT COUNT(""ID"") FROM ""Games"" WHERE ""Status"" = 'Completed'";

        private const string TAG_CONDITION = @"EXISTS (
            SELECT 1 FROM ""GameTagRelations"" WHERE ""Games"".""ID"" = ""GameID"" AND ""TagID"" IN (
                SELECT ""ID"" FROM ""Tags"" WHERE ""Name"" in ({0})))";

        private bool _builded = false;
        private List<string> _priceInfoConditions = new List<string>(5);
        private List<string> _gameConditions = new List<string>(5);

        private int _pageNum = 1;
        private int _gamesPerPage = 100;
        private string _currency = "RUB";
        private string _country = "RU";
        private string _types = @$"""Games"".""GameType"" in ('{GameEntityType.FullGame}','{GameEntityType.Edition}','{GameEntityType.Pack}')";
        private string _orderByCondition = @"""Games"".""Order"" DESC";

        public GamesQueryBuilder SetGameServices(string services)
        {
            if (String.IsNullOrEmpty(services))
                return this;

            string[] gameServices = services.ToUpper().Split(',');
            _priceInfoConditions.Add($@"""ServiceCode"" in('{ String.Join("','", gameServices)}')");
            return this;
        }

        public GamesQueryBuilder SetMaxPrice(int? price)
        {
            if (price.HasValue)
                _priceInfoConditions.Add($@"""CurrentPrice"" <= {price}");

            return this;
        }

        public GamesQueryBuilder SetMinDiscount(int? discount)
        {
            if (!discount.HasValue)
                return this;

            _priceInfoConditions.Add($@"""Discount"" >= {discount}");
            return this;
        }

        public GamesQueryBuilder SetCountryAndCurrency(string country, string currency)
        {
            if (!String.IsNullOrEmpty(country))
                _country = country;

            if (!String.IsNullOrEmpty(currency))
                _currency = currency;

            return this;
        }

        public GamesQueryBuilder SetTags(string tags)
        {
            if (String.IsNullOrEmpty(tags))
                return this;

            string[] tagsArr = tags.Split(',');
            _gameConditions.Add(String.Format(TAG_CONDITION, $"'{String.Join(',', tagsArr)}'"));
            return this;
        }

        public GamesQueryBuilder SetDeveloper(string developer)
        {
            if (!String.IsNullOrEmpty(developer))
                _gameConditions.Add($@"""Games"".""Developer"" = '{developer}'");

            return this;
        }

        public GamesQueryBuilder SetSearchPhrase(string phrase)
        {
            if (String.IsNullOrEmpty(phrase))
                return this;

            string nameIdPart = StringHelper.CreateOneLineString(phrase);
            _gameConditions.Add($@"""Games"".""NameID"" like '%{nameIdPart}%'");
            return this;
        }

        public GamesQueryBuilder SetTypes(string types)
        {
            if (String.IsNullOrEmpty(types))
                return this;

            string[] typesSrc = types.Split(',');
            List<string> typesSearch = new List<string>(typesSrc.Length);

            for (int i = 0; i < typesSrc.Length; i++)
            {
                string type = typesSrc[i].ToLower();
                switch (type)
                {
                    case FilterParams.GAME_TYPE_BASE_GAME:
                        typesSearch.Add(GameEntityType.FullGame.ToString());
                        break;
                    case FilterParams.GAME_TYPE_EDITION:
                        typesSearch.Add(GameEntityType.Edition.ToString());
                        break;
                    case FilterParams.GAME_TYPE_DLC:
                        typesSearch.Add(GameEntityType.DLC.ToString());
                        break;
                    case FilterParams.GAME_TYPE_PACK:
                        typesSearch.Add(GameEntityType.Pack.ToString());
                        break;
                    case FilterParams.GAME_TYPE_DEMO:
                        typesSearch.Add(GameEntityType.Demo.ToString());
                        break;
                    case FilterParams.GAME_TYPE_SOFT: 
                        typesSearch.Add(GameEntityType.Software.ToString()); 
                        break;
                }
            }

            _types = (typesSearch.Count == 1)
                ? @$"""Games"".""GameType"" = '{typesSearch[0]}'"
                : @$"""Games"".""GameType"" in ('{String.Join("','", typesSearch)}')";

            return this;
        }

        public GamesQueryBuilder SetOrder(string order)
        {
            if (String.IsNullOrEmpty(order))
                return this;

            switch (order.ToLower())
            {
                case FilterParams.SORT_RELEASE_DATE:
                    _orderByCondition = @"""Games"".""ReleaseDate""";
                    break;
                case FilterParams.SORT_RELEASE_DATE_DESC:
                    _orderByCondition = @"""Games"".""ReleaseDate"" DESC";
                    break;
                case FilterParams.SORT_ABC:
                    _orderByCondition = @"""Games"".""NameID""";
                    break;
                case FilterParams.SORT_PRICE:
                    _orderByCondition = @"(SELECT MIN(""CurrentPrice"") FROM ""PriceInfos"" WHERE ""GameID"" = ""Games"".""ID"")";
                    break;
                case FilterParams.SORT_PRICE_DESC:
                    _orderByCondition = @"(SELECT MIN(""CurrentPrice"") FROM ""PriceInfos"" WHERE ""GameID"" = ""Games"".""ID"") DESC";
                    break;
            }

            return this;
        }

        public GamesQueryBuilder SetLimits(int pageNum, int gamesPerPage)
        {
            if (pageNum > 0)
                _pageNum = pageNum;

            if (gamesPerPage > 0)
                _gamesPerPage = gamesPerPage;

            return this;
        }

        public (string countQuery, string gamesQuery) Build()
        {
            if (_builded)
                throw new InvalidOperationException("Query already builded");

            StringBuilder whereBuilder = new StringBuilder();

            string priceInfoConditions = (_priceInfoConditions.Count > 0)
                ? " AND " + String.Join(" AND ", _priceInfoConditions)
                : null;

            string priceInfoWhere = String.Format(ConstantQueries.COUNTRY_AND_CURRENCY_CONDITION,
                _country,
                _currency,
                priceInfoConditions);



            whereBuilder.Append(priceInfoWhere);

            _gameConditions.Add(_types);
            whereBuilder.Append(" AND ");
            whereBuilder.Append(String.Join(" AND ", _gameConditions));

            string countQuery = GAMES_COUNT_QUERY + "\n" + whereBuilder.ToString();

            whereBuilder.Append($"\nORDER BY {_orderByCondition}");

            int offset = (_pageNum - 1) * _gamesPerPage;
            whereBuilder.Append($"\nLIMIT {_gamesPerPage} OFFSET {offset}");

            whereBuilder.Insert(0, $"{ConstantQueries.GAMES_QUERY}\n");
            string query = String.Format(ConstantQueries.PRICES_WITH_GAMES_QUERY, whereBuilder, _country, _currency);

            _builded = true;

            return (countQuery: countQuery, gamesQuery: query);
        }
    }
}
