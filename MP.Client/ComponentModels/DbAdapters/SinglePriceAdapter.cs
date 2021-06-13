using Dapper;
using MP.Client.ComponentModels.Common;
using MP.Client.SiteModels.GameModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MP.Client.ComponentModels.DbAdapters
{
    public class SinglePriceAdapter : ComponentModelDbAdapter<OneServiceGame>
    {
        protected const string GAME_WITH_SINGLE_PRICE_QUERY = @"
            SELECT g.""Name"", g.""NameID"", pi.""CurrentPrice"", pi.""FullPrice"", pi.""Discount"", pi.""ServiceCode"", pi.""IsPreorder"",
            (SELECT ""Path"" FROM ""Images"" WHERE ""Tag"" = 'logo-horizontal' AND g.""ID"" = ""GameID"" LIMIT  1) as ""ImageHorizontal"",
            (SELECT ""Path"" FROM ""Images"" WHERE ""Tag"" = 'logo-vertical' AND g.""ID"" = ""GameID"" LIMIT  1) as ""ImageVertical""
            FROM ""PriceInfos"" as pi
            INNER JOIN ""Games"" as g ON ""GameID"" = g.""ID""
            WHERE g.""Status"" = 'Completed' AND pi.""IsAvailable"" AND NOT pi.""IsIgnore"" AND pi.""CountryCode"" = '{0}' AND pi.""CurrencyCode"" = '{1}' AND 
        ";

        protected const string GAMEID_AND_SERVICECODDE_CONDITION = @"
            (g.""ID"", pi.""ServiceCode"") in ({0})
        ";

        protected GamesQueryOptions _options;
        private Dictionary<int, string> _gamesWithSpecificService;

        public SinglePriceAdapter(
            IDbConnection connection, 
            GamesQueryOptions options, 
            Dictionary<int, string> gamesWithSpecificService) : base(connection)
        {
            _options = options;
            _gamesWithSpecificService = gamesWithSpecificService;
        }
        public override IEnumerable<OneServiceGame> ReadData()
        {
            string query = CreateQuery();
            return _dbConnection.Query<OneServiceGame>(query);
        }

        protected override string CreateQuery()
        {
            StringBuilder queryBuilder = new StringBuilder();

            bool needUnion = false;
            string query = String.Format(GAME_WITH_SINGLE_PRICE_QUERY, 
                _options.CountryCode, _options.CurrencyCode);

            if (IsNeedCreateSpecificQuery())
            {
                queryBuilder.Append('(');

                var prices = _gamesWithSpecificService.Select(i => $"({i.Key},'{i.Value}')");
                string where = String.Format(GAMEID_AND_SERVICECODDE_CONDITION, String.Join(',', prices));

                queryBuilder.Append(query);
                queryBuilder.Append(where);
                queryBuilder.Append(')');

                needUnion = true;
            }

            if (IsNeedCreateMainQuery())
            {
                if (needUnion)
                    queryBuilder.Append("\nUNION\n");

                queryBuilder.Append('(');
                queryBuilder.Append(query);
                queryBuilder.Append(_options.Condition);
                
                if (!String.IsNullOrEmpty(_options.OrderBy))
                {
                    queryBuilder.Append("\n ORDER BY ");
                    queryBuilder.Append(_options.OrderBy);
                }

                int limit = _options.Count - (_gamesWithSpecificService?.Count ?? 0);
                queryBuilder.Append($"\n LIMIT {limit}");

                queryBuilder.Append(")\n");
            }

            return queryBuilder.ToString();
        }

        private bool IsNeedCreateSpecificQuery()
            => _gamesWithSpecificService.Count > 0;

        private bool IsNeedCreateMainQuery() =>
            !String.IsNullOrEmpty(_options.Condition) && _gamesWithSpecificService.Count < _options.Count;
    }
}
