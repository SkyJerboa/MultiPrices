using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MP.Client.Common.ClientMetadata;
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
            WHERE ""NameID"" = @NameId AND ""Status"" = 'Completed'
                AND EXISTS (SELECT 1 FROM ""PriceInfos"" pi 
                    WHERE pi.""GameID"" = ""Games"".""ID"" AND pi.""CountryCode"" = @CountryCode 
                    AND pi.""CurrencyCode"" = @CurrencyCode AND pi.""IsAvailable"" AND NOT pi.""IsIgnore"")";

        private const string QUERY_TAGS = @"
            SELECT ""Name"" FROM ""Tags""
            WHERE ""ID"" IN (SELECT ""TagID"" FROM ""GameTagRelations"" WHERE ""GameID"" = @GameId);";

        private const string QUERY_IMAGES = @"
            SELECT ""Tag"", ""Path"", ""Order"" FROM ""Images"" WHERE ""GameID"" = @GameId;";

        //QUERY_CHILDREN и QUERY_PARENTS можно объединить в 1 запрос с помощью UNION
        private const string QUERY_CHILDREN = @"SELECT g.""Name"", g.""NameID"", g.""GameType"" AS ""Type"", sc.""Services"",
                (SELECT ""Path"" FROM ""Images"" WHERE ""GameID"" = g.""ID"" AND ""Tag"" = 'logo-horizontal' LIMIT 1 ) AS ""ImageHorizontal"",
                (SELECT ""Path"" FROM ""Images"" WHERE ""GameID"" = g.""ID"" AND ""Tag"" = 'logo-vertical' LIMIT 1 ) AS ""ImageVertical""
            FROM ""GameRelationships"" gr
            JOIN (SELECT ""GameID"", array_agg(""ServiceCode"") AS ""Services"" FROM ""PriceInfos""
                    WHERE ""IsAvailable"" AND NOT ""IsIgnore"" AND ""CountryCode"" = @CountryCode AND ""CurrencyCode"" = @CurrencyCode
                    GROUP BY (""GameID"")
                ) sc ON gr.""ChildID"" = sc.""GameID""
            JOIN ""Games"" g ON g.""ID"" = gr.""ChildID""
            WHERE g.""Status"" = 'Completed' AND gr.""ParentID"" = @GameId;";

        private const string QUERY_PARENTS = @"SELECT g.""Name"", g.""NameID"", g.""GameType"" AS ""Type"", sc.""Services"",
                (SELECT ""Path"" FROM ""Images"" WHERE ""GameID"" = g.""ID"" AND ""Tag"" = 'logo-horizontal' LIMIT 1 ) AS ""ImageHorizontal"",
                (SELECT ""Path"" FROM ""Images"" WHERE ""GameID"" = g.""ID"" AND ""Tag"" = 'logo-vertical' LIMIT 1 ) AS ""ImageVertical""
            FROM ""GameRelationships"" gr
            JOIN (SELECT ""GameID"", array_agg(""ServiceCode"") AS ""Services"" FROM ""PriceInfos""
                    WHERE ""IsAvailable"" AND NOT ""IsIgnore"" AND ""CountryCode"" = @CountryCode AND ""CurrencyCode"" = @CurrencyCode
                    GROUP BY (""GameID"")
                ) sc ON gr.""ParentID"" = sc.""GameID""
            JOIN ""Games"" g ON g.""ID"" = gr.""ParentID""
            WHERE g.""Status"" = 'Completed' AND gr.""ChildID"" = @GameId;";

        private const string QUERY_SERVICES = @"
            SELECT ""ServiceCode"" AS ""Code"", ""CurrentPrice"", ""FullPrice"", ""Discount"", ""CurrencyCode"", ""IsPreorder"",
                ""GameLink"" AS ""Link"", ""IsFree"" AS ""Free""
            FROM ""PriceInfos"" 
            WHERE ""IsAvailable"" AND NOT ""IsIgnore"" AND ""GameID"" = @GameId AND ""CountryCode"" = @CountryCode AND ""CurrencyCode"" = @CurrencyCode;";

        private const string QUERY_SYSTEM_REQUIREMENTS = @"
            SELECT ""Type"", ""SystemType"", ""OS"", ""CPU"", ""RAM"", ""Storage"", ""DirectX"", ""Sound"", ""Network"", ""Other""
            FROM ""SystemRequirements"" WHERE ""GameID"" = @GameId;";

        private const string QUERY_TRANSLATIONS = @"
            SELECT ""Key"", ""Value"", ""LanguageCode"" FROM ""GameTranslations""
            WHERE ""GameID"" = @GameId AND ""LanguageCode"" = @LangCode;";

        private const string QUERY_TRANSLATIONS_WITH_DEFAULT = @"
            SELECT ""Key"", ""Value"", ""LanguageCode"" FROM ""GameTranslations""
            WHERE ""GameID"" = @GameId AND ""LanguageCode"" IN (@LangCode,@DefaultLangCode);";

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

            GameFullInfo game = _connection.QueryFirstOrDefault<GameFullInfo>(QUERY_GAME, 
                new { NameId = ID, CountryCode, CurrencyCode });
            if (game == null)
                return new JsonErrorResult("Not found", $"Game with NameID {ID} was not found");

            int id = game.ID;
            string imgServerUrl = SiteConfigurationManager.Config.ImageServerUrl;

            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.Append(QUERY_TAGS);
            queryBuilder.Append(QUERY_IMAGES);
            queryBuilder.Append(QUERY_CHILDREN);
            queryBuilder.Append(QUERY_PARENTS);
            queryBuilder.Append(QUERY_SERVICES);
            queryBuilder.Append(QUERY_SYSTEM_REQUIREMENTS);
            if (LangCode == DEFAULT_LANG)
                queryBuilder.Append(QUERY_TRANSLATIONS);
            else
                queryBuilder.Append(QUERY_TRANSLATIONS_WITH_DEFAULT);


            using (var multi = _connection.QueryMultiple(queryBuilder.ToString(), new { GameId = id, CountryCode, CurrencyCode, LangCode, DefaultLangCode = DEFAULT_LANG }))
            {
                game.Tags = multi.Read<string>().ToArray();
                IEnumerable<GImage> imgs = multi.Read<GImage>().ToArray();
                game.Children = multi.Read<ChildGame>().ToArray();
                game.Parents = multi.Read<ChildGame>().ToArray();
                game.Services = multi.Read<Service>().ToArray();
                game.SystemRequirements = multi.Read<SystemRequirement>().ToArray();
                IEnumerable<GTranslation> trans = multi.Read<GTranslation>().ToArray();


                SetImgUrlToAddnons(game.Children, imgServerUrl);
                SetImgUrlToAddnons(game.Parents, imgServerUrl);

                SetImagesToGame(game, imgs, imgServerUrl);

                SetLocalizationsToGame(game, trans);

                SetMetadata(game);
            }

            return new JsonResult(game);
        }

        private void SetImgUrlToAddnons(IEnumerable<ChildGame> games, string imgServerUrl)
        {
            foreach (ChildGame game in games)
            {
                if (game.ImageVertical != null)
                    game.ImageVertical = imgServerUrl + game.ImageVertical;
                if (game.ImageHorizontal != null)
                    game.ImageHorizontal = imgServerUrl + game.ImageHorizontal;
            }
        }

        private void SetImagesToGame(GameFullInfo game, IEnumerable<GImage> images, string imgServerUrl)
        {
            List<GImage> screenshots = new List<GImage>();
            foreach (GImage img in images)
            {
                switch (img.Tag)
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
        }

        private void SetLocalizationsToGame(GameFullInfo game, IEnumerable<GTranslation> trans)
        {
            game.LocalizedName = trans.FirstOrDefault(i => i.LanguageCode == LangCode && i.Key == TransKeys.GAME_NAME)?.Value;
            game.Description = trans.FirstOrDefault(i => i.LanguageCode == LangCode && i.Key == TransKeys.GAME_DESCRIPTION)?.Value;

            if (LangCode != DEFAULT_LANG)
            {
                if (game.LocalizedName == null)
                    game.LocalizedName = trans
                        .FirstOrDefault(i => i.LanguageCode == DEFAULT_LANG && i.Key == TransKeys.GAME_NAME)?.Value;
                if (game.Description == null)
                    game.Description = trans
                        .FirstOrDefault(i => i.LanguageCode == DEFAULT_LANG && i.Key == TransKeys.GAME_DESCRIPTION)?.Value;
            }
        }

        private void SetMetadata(GameFullInfo game)
        {
            GameMetadataCreator metadataCreator = new GameMetadataCreator();
            game.Meta = metadataCreator.CreateMetadata(game);
        }
    }
}
