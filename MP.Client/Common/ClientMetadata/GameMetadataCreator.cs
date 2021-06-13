using Dapper;
using Microsoft.AspNetCore.Http;
using MP.Client.Common.Configuration;
using MP.Client.Common.Constants;
using MP.Client.SiteModels.GameModels.GameFullInfo;
using System;
using System.Data;

namespace MP.Client.Common.ClientMetadata
{
    public class GameMetadataCreator : IMetadataCreator
    {
        private const string GAME_QUERY = @"SELECT ""Name"" FROM ""Games"" WHERE ""NameID"" = '{0}'";
        
        public Metadata CreateMetadata(IDbConnection _connection, HttpRequest request)
        {
            string url = request.Path.Value;
            int slashIndex = url.IndexOf('/', 1);
            string gameId = url.Substring(slashIndex + 1);
            if (gameId[gameId.Length - 1] == '/')
                gameId = gameId.Substring(gameId.Length - 1);
            if (gameId.Contains('/'))
                return null;

            string query = String.Format(GAME_QUERY, gameId);
            string gameName = _connection.QueryFirstOrDefault<string>(query);

            if (String.IsNullOrEmpty(gameName))
                return null;

            return new Metadata
            {
                Title = String.Format(MetadataTemplates.GAME_TITLE_TEMPLATE, gameName),
                Description = String.Format(MetadataTemplates.GAME_DESCRIPTION_TEMPLATE, gameName),
                Canonical = GetCanonicalUrl(gameName)
            };
        }

        public Metadata CreateMetadata(GameFullInfo game)
        {
            return new Metadata
            {
                Title = String.Format(MetadataTemplates.GAME_TITLE_TEMPLATE, game.Name),
                Description = String.Format(MetadataTemplates.GAME_DESCRIPTION_TEMPLATE, game.Name),
                Canonical = GetCanonicalUrl(game.NameID)
            };
        }

        private string GetCanonicalUrl(string gameNameId)
        {
            string host = SiteConfigurationManager.Config.Host;
            string canonical = host + "/games/" + gameNameId;
            return canonical;
        }
    }
}
