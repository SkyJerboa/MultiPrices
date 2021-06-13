namespace MP.Client.Common.Constants
{
    public static class FilterParams
    {
        public const string SORT_RELEASE_DATE = "releasedate";
        public const string SORT_RELEASE_DATE_DESC = "releasedatedesc";
        public const string SORT_ABC = "alphabet";
        public const string SORT_PRICE = "price";
        public const string SORT_PRICE_DESC = "pricedesc";

        public const string GAME_TYPE_BASE_GAME = "game";
        public const string GAME_TYPE_EDITION = "edition";
        public const string GAME_TYPE_DLC = "dlc";
        public const string GAME_TYPE_PACK = "pack";
        public const string GAME_TYPE_DEMO = "demo";
        public const string GAME_TYPE_SOFT = "soft";

        public const string PARAM_GAME_SERVICE = "gameservice";
        public const string PARAM_GAME_TYPE = "type";
        public const string PARAM_TAG = "tag";
        public const string PARAM_DEVELOPER = "developer";

        public static readonly string[] GAME_TYPES = new string[]
        {
            GAME_TYPE_BASE_GAME,
            GAME_TYPE_EDITION,
            GAME_TYPE_DLC,
            GAME_TYPE_PACK,
            GAME_TYPE_DEMO,
            GAME_TYPE_SOFT
        };
    }
}
