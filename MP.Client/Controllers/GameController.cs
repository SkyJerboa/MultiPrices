using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MP.Client.Common.Configuration;
using MP.Client.Common.JsonResponses;
using MP.Client.SiteModels.GameModels.GameFullInfo;
using MP.Core.Common.Constants;
using MP.Core.Contexts.Games;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MP.Client.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class GameController : Controller
    {
        private const string DEFAULT_LANG = "ru";

        private const string QUERY_GAME = @"
            SELECT ""ID"", ""Name"", ""NameID"", ""GameType"" as ""Type"", ""GamePlatform"" as ""Platforms"", ""Publisher"", 
                ""Developer"", ""Brand"", ""Languages"", ""ReleaseDate""
	        FROM public.""Games""
            WHERE ""NameID"" = '{0}' AND ""Status"" = 'Completed'";

        private const string QUERY_TAGS = @"
            SELECT ""Name"" FROM ""Tags""
            WHERE ""ID"" IN (SELECT ""TagID"" FROM ""GameTagRelations"" WHERE ""GameID"" = {0});";

        private const string QUERY_IMAGES = @"
            SELECT ""Tag"", ""Path"", ""Order"" FROM ""Images"" WHERE ""GameID"" = {0};";

        //QUERY_CHILDREN и QUERY_PARENTS можно объединить в 1 запрос
        private const string QUERY_CHILDREN = @"
            SELECT ""Name"", ""NameID"", ""GameType"" as ""Type"", ""GameServicesCodes"" as ""Services"",
            (SELECT ""Path"" FROM ""Images"" WHERE ""GameID"" = ""Games"".""ID"" AND ""Tag"" = 'logo-horizontal' LIMIT 1) AS ""ImageHorizontal"",
            (SELECT ""Path"" FROM ""Images"" WHERE ""GameID"" = ""Games"".""ID"" AND ""Tag"" = 'logo-vertical' LIMIT 1) AS ""ImageVertical""
            FROM ""Games""
            WHERE ""ID"" IN (SELECT ""ChildID"" FROM ""GameRelationships"" WHERE ""ParentID"" = {0}) AND ""Status"" = 'Completed';";

        private const string QUERY_PARENTS = @"
            SELECT ""Name"", ""NameID"", ""GameType"" as ""Type"", ""GameServicesCodes"" as ""Services"",
            (SELECT ""Path"" FROM ""Images"" WHERE ""GameID"" = ""Games"".""ID"" AND ""Tag"" = 'logo-horizontal' LIMIT 1) AS ""ImageHorizontal"",
            (SELECT ""Path"" FROM ""Images"" WHERE ""GameID"" = ""Games"".""ID"" AND ""Tag"" = 'logo-vertical' LIMIT 1) AS ""ImageVertical""
            FROM ""Games""
            WHERE ""ID"" IN (SELECT ""ParentID"" FROM ""GameRelationships"" WHERE ""ChildID"" = {0}) AND ""Status"" = 'Completed';";

        private const string QUERY_SERVICES = @"
            SELECT ""ServiceCode"" AS ""Code"", ""CurrentPrice"", ""FullPrice"", ""Discount"", ""CurrencyCode"", ""IsPreorder"",
                ""GameLink"" AS ""Link"", ""IsFree"" AS ""Free""
            FROM ""PriceInfos"" 
            WHERE ""IsAvailable"" AND ""GameID"" = {0} AND ""CountryCode"" = '{1}' AND ""CurrencyCode"" = '{2}';";

        private const string QUERY_SYSTEM_REQUIREMENTS = @"
            SELECT ""Type"", ""SystemType"", ""OS"", ""CPU"", ""RAM"", ""Storage"", ""DirectX"", ""Sound"", ""Network"", ""Other""
            FROM ""SystemRequirements"" WHERE ""GameID"" = {0};";
        
        private const string QUERY_TRANSLATIONS = @"
            SELECT ""Key"", ""Value"", ""LanguageCode"" FROM ""GameTranslations""
            WHERE ""GameID"" = {0} AND ""LanguageCode"" = '{1}';";

        private const string QUERY_TRANSLATIONS_WITH_DEFAULT = @"
            SELECT ""Key"", ""Value"", ""LanguageCode"" FROM ""GameTranslations""
            WHERE ""GameID"" = {0} AND ""LanguageCode"" IN ('{1}','{2}');";

        private IDbConnection _connection { get; }

        public GameController(GameContext context)
        {
            _connection = context.Database.GetDbConnection();
        }

        [FromQuery]
        public string ID { get; set; }
        [FromQuery]
        public string LangCode { get; set; } = "ru";
        [FromQuery]
        public string CountryCode { get; set; } = "RU";
        [FromQuery]
        public string CurrencyCode { get; set; } = "RUB";

        public IActionResult Index()
        {
            if (String.IsNullOrEmpty(ID))
                return new JsonErrorResult("missing NameID", "NameID not found");

            string query = String.Format(QUERY_GAME, ID);
            GameFullInfo game = _connection.QueryFirstOrDefault<GameFullInfo>(query);
            if (game == null)
                return new JsonErrorResult("Not found", $"Game with NameID {ID} was not found");

            int id = game.ID;
            string imgServerUrl = SiteConfigurationManager.Config.ImageServerUrl;

            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.Append(String.Format(QUERY_TAGS, id));
            queryBuilder.Append(String.Format(QUERY_IMAGES, id));
            queryBuilder.Append(String.Format(QUERY_CHILDREN, id));
            queryBuilder.Append(String.Format(QUERY_PARENTS, id));
            queryBuilder.Append(String.Format(QUERY_SERVICES, id, CountryCode, CurrencyCode));
            queryBuilder.Append(String.Format(QUERY_SYSTEM_REQUIREMENTS, id));
            if (LangCode == DEFAULT_LANG)
                queryBuilder.Append(String.Format(QUERY_TRANSLATIONS, id, LangCode));
            else
                queryBuilder.Append(String.Format(QUERY_TRANSLATIONS_WITH_DEFAULT, id, LangCode, DEFAULT_LANG));


            using (var multi = _connection.QueryMultiple(queryBuilder.ToString()))
            {
                game.Tags = multi.Read<string>().ToArray();
                IEnumerable<GImage> imgs = multi.Read<GImage>().ToArray();
                game.Children = multi.Read<ChildGame>().ToArray();
                game.Parents = multi.Read<ChildGame>().ToArray();
                game.Services = multi.Read<Service>().ToArray();
                game.SystemRequirements = multi.Read<SystemRequirement>().ToArray();
                IEnumerable<GTranslation> trans = multi.Read<GTranslation>().ToArray();

                Action<IEnumerable<ChildGame>> setImgUrlToChildrenGames = (children) =>
                {
                    foreach (ChildGame child in children)
                    {
                        if (child.ImageVertical != null)
                            child.ImageVertical = imgServerUrl + child.ImageVertical;
                        if (child.ImageHorizontal != null)
                            child.ImageHorizontal = imgServerUrl + child.ImageHorizontal;
                    }
                };

                setImgUrlToChildrenGames(game.Children);
                setImgUrlToChildrenGames(game.Parents);

                List<GImage> screenshots = new List<GImage>();
                foreach(GImage img in imgs)
                {
                    switch(img.Tag)
                    {
                        case ImageTags.IMG_VERTICAL:
                            game.ImageVertical = imgServerUrl + img.Path;
                            break;
                        case ImageTags.IMG_HORIZONTAL:
                            game.ImageHorizontal = imgServerUrl + img.Path;
                            break;
                        case ImageTags.SCREENSHOT:
                            screenshots.Add(img);
                            break;
                    }
                }
                game.Screenshots = screenshots.OrderBy(i => i.Order).Select(i => imgServerUrl + i.Path);

                game.LocalizedName = trans.FirstOrDefault(i => i.LanguageCode == LangCode && i.Key == TransKeys.GAME_NAME)?.Value;
                game.Description = trans.FirstOrDefault(i => i.LanguageCode == LangCode && i.Key == TransKeys.GAME_DESCRIPTION)?.Value;

                if (LangCode != DEFAULT_LANG)
                {
                    if (game.LocalizedName == null)
                        game.LocalizedName = trans.FirstOrDefault(i => i.LanguageCode == DEFAULT_LANG && i.Key == TransKeys.GAME_NAME)?.Value;
                    if (game.Description == null)
                        game.Description = trans.FirstOrDefault(i => i.LanguageCode == DEFAULT_LANG && i.Key == TransKeys.GAME_DESCRIPTION)?.Value;
                }
            }

            return new JsonResult(game);
        }
    }
}
