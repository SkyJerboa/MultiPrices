 namespace MP.Scraping.Common.Constants
{
    public static class TableDefinitions
    {
        public const string COUNT_ITEM = "gameCount";

        public const string TABLE_ITEM = "games[*]";

        /// <summary>
        /// Required
        /// </summary>
        public const string INNER_ID = "innerId";
        public const string TYPE_NAME = "gameType";
        /// <summary>
        /// Required
        /// </summary>
        //public const string IMAGE_URL = "image";
        public const string IMAGE_PATH_NAME = "imageName";
        /// <summary>
        /// Required
        /// </summary>
        public const string NAME = "gameName";
        /// <summary>
        /// Required
        /// </summary>
        public const string NAME_ID = "nameId";
        public const string NAME_LOCALIZED = "localizedName";
        public const string DESCRIPTION = "gameDescription";
        public const string RELEASE_DATE = "releaseDate";
        /// <summary>
        /// Required
        /// </summary>
        public const string CURRENT_PRICE = "currentPrice";
        /// <summary>
        /// Required
        /// </summary>
        public const string FULL_PRICE = "fullPrice";
        /// <summary>
        /// Required
        /// </summary>
        public const string DISCOUNT = "discount";
        /// <summary>
        /// Required
        /// </summary>
        public const string URL = "gameUrl";
        public const string SYSTEM_REQUIREMENT = "systemRequirements";
        public const string DEVELOPER = "developer";
        public const string PUBLISHER = "publisher";
        public const string CONST_URL = "constUrl";
        public const string PLATFORM = "platform";
        public const string PREORDER = "preorder";
        /// <summary>
        /// Required
        /// </summary>
        public const string AVAILABLE = "isAvailable";
        public const string PARENT_ID = "parentInnerID";
        public const string CHILDREN_IDS = "childrenIDs";
        public const string OFFER_ID = "offerID";
        public const string SERIES = "gameSeries";
        public const string LOCALIZATIONS = "languages";
        /// <summary>
        /// Required
        /// </summary>
        public const string IS_NEW_ITEM = "isNewGame";
        public const string SCREENSHOTS = "screenshots";
        public const string TAGS = "tags";
        public const string IMG_VERTICAL = "imgVertical";
        public const string IMG_HORIZONTAL = "imgHorizontal";
        public const string IMG_LOGO = "imgLogo";
        public const string IMG_LONG = "imgHorizontalLong";


        public const string ITEM_INNER_ID = TABLE_ITEM + "." + INNER_ID;
        public const string ITEM_TYPE = TABLE_ITEM + "." + TYPE_NAME;
        //public const string ITEM_IMAGE = TABLE_ITEM + "." + IMAGE_URL;
        public const string ITEM_IMAGE_NAME = TABLE_ITEM + "." + IMAGE_PATH_NAME;
        public const string ITEM_NAME = TABLE_ITEM + "." + NAME;
        public const string ITEM_NAME_ID = TABLE_ITEM + "." + NAME_ID;
        public const string ITEM_DESCRIPTION = TABLE_ITEM + "." + DESCRIPTION;
        public const string ITEM_RELEASE_DATE = TABLE_ITEM + "." + RELEASE_DATE;
        public const string ITEM_CURRENT_PRICE = TABLE_ITEM + "." + CURRENT_PRICE;
        public const string ITEM_FULL_PRICE = TABLE_ITEM + "." + FULL_PRICE;
        public const string ITEM_DISCOUNT = TABLE_ITEM + "." + DISCOUNT;
        public const string ITEM_URL = TABLE_ITEM + "." + URL;
        public const string ITEM_SYSTEM_REQUIREMENTS = TABLE_ITEM + "." + SYSTEM_REQUIREMENT;
        public const string ITEM_DEVELOPER = TABLE_ITEM + "." + DEVELOPER;
        public const string ITEM_PUBLISHER = TABLE_ITEM + "." + PUBLISHER;
        public const string ITEM_CONST_URL = TABLE_ITEM + "." + CONST_URL;
        public const string ITEM_PREORDER = TABLE_ITEM + "." + PREORDER;
        public const string ITEM_AVAILABLE = TABLE_ITEM + "." + AVAILABLE;
        public const string ITEM_PARENT_ID = TABLE_ITEM + "." + PARENT_ID;
        public const string ITEM_CHILDREN_IDS = TABLE_ITEM + "." + CHILDREN_IDS;
        public const string ITEM_OFFER_OD = TABLE_ITEM + "." + OFFER_ID;
        public const string ITEM_SERIES = TABLE_ITEM + "." + SERIES;
        public const string ITEM_LOCALIZATIONS = TABLE_ITEM + "." + LOCALIZATIONS;
        public const string ITEM_IS_NEW_ITEM = TABLE_ITEM + "." + IS_NEW_ITEM;
        public const string ITEM_SCREENSHOTS = TABLE_ITEM + "." + SCREENSHOTS;
        public const string ITEM_TAGS = TABLE_ITEM + "." + TAGS;
    }
}
