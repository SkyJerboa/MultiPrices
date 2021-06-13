namespace MP.Client.Common.Queries
{
    public class ConstantQueries
    {
        public const string GAMES_QUERY = @"
            SELECT ""ID"", ""Name"", ""NameID"", ""GameType"", ""Order"", ""ReleaseDate"",
            (SELECT ""Path"" FROM ""Images"" WHERE ""Tag"" = 'logo-horizontal' AND ""Games"".""ID"" = ""GameID"" LIMIT  1) as ""ImageHorizontal"",
            (SELECT ""Path"" FROM ""Images"" WHERE ""Tag"" = 'logo-vertical' AND ""Games"".""ID"" = ""GameID"" LIMIT  1) as ""ImageVertical""
            FROM ""Games""
            WHERE ""Status"" = 'Completed'
            ";

        public const string COUNTRY_AND_CURRENCY_CONDITION = @"
            AND EXISTS (SELECT 1 FROM ""PriceInfos"" WHERE ""GameID"" = ""Games"".""ID"" AND ""CountryCode"" = '{0}' AND ""CurrencyCode"" = '{1}'
                AND ""IsAvailable"" AND NOT ""IsIgnore""{2})";

        public const string PRICES_WITH_GAMES_QUERY = @"
            WITH g AS({0})
            SELECT pi.""ServiceCode"", pi.""CountryCode"", pi.""CurrencyCode"", pi.""CurrentPrice"", pi.""FullPrice"", pi.""Discount"", pi.""IsPreorder"",
                g.""ID"", g.""Name"", g.""NameID"", g.""GameType"", g.""ReleaseDate"", g.""ImageHorizontal"", g.""ImageVertical""
            FROM g
            LEFT JOIN ""PriceInfos"" as pi ON pi.""GameID"" = g.""ID""
            WHERE pi.""IsAvailable"" AND NOT pi.""IsIgnore"" AND pi.""CountryCode"" = '{1}' AND pi.""CurrencyCode"" = '{2}'";
    }
}
