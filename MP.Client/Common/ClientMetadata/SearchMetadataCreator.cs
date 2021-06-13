using Dapper;
using Microsoft.AspNetCore.Http;
using MP.Client.Common.Configuration;
using MP.Client.Common.Constants;
using System;
using System.Data;
using System.Linq;
using System.Text;

namespace MP.Client.Common.ClientMetadata
{
    public class SearchMetadataCreator : IMetadataCreator
    {
        private const string LANGUAGE_CODE = "ru";

        private const string SERVICE_NAME_QUERY = @"SELECT ""Name"" FROM ""Services"" WHERE ""Code"" = '{0}'";
        private const string DEVELOPER_QUERY = @"SELECT 1 FROM ""Games"" WHERE ""Developer"" = '{0}'";
        private const string TAG_QUERY = @"SELECT 1 FROM ""Tags"" WHERE ""Name"" = '{0}'";

        public Metadata CreateMetadata(IDbConnection connection, HttpRequest request)
        {
            IQueryCollection queryParams = request.Query;
            bool hasPageParam = queryParams.ContainsKey("page");

            if (queryParams.Count > 2 || queryParams.Count == 2 && !hasPageParam)
                return CreateMetadata(connection, null, null);

            var param = queryParams.FirstOrDefault(i => i.Key != "page");
            return CreateMetadata(connection, param.Key, param.Value);
        }

        private Metadata CreateMetadata(IDbConnection connection, string paramName, string paramValue)
        {
            Metadata metadata;
            switch (paramName?.ToLower())
            {
                case FilterParams.PARAM_GAME_SERVICE:
                    metadata = GetGameServiceMeta(connection, paramValue);
                    break;
                case FilterParams.PARAM_GAME_TYPE:
                    metadata = GetGameTypeMeta(paramValue);
                    break;
                case FilterParams.PARAM_TAG:
                    metadata = GetTagMeta(connection, paramValue);
                    break;
                case FilterParams.PARAM_DEVELOPER:
                    metadata = GetDeveloperMeta(connection, paramValue);
                    break;
                default:
                    metadata = CreateDefaultMetadata();
                    metadata.Canonical = CreateCanonical();
                    break;
            }

            return metadata;
        }

        private Metadata CreateDefaultMetadata()
        {
            return new Metadata
            {
                Title = MetadataTemplates.SEARCH_TITLE_TEMPLATE,
                Description = MetadataTemplates.SEARCH_DESCRIPTION_TEMPLATE
            };
        }

        private string CreateCanonical(string paramName = null, string paramValue = null)
        {
            string host = SiteConfigurationManager.Config.Host;
            StringBuilder sb = new StringBuilder(host + "/search");
            if (paramName != null)
                sb.Append($"?{paramName}={paramValue}");

            return sb.ToString();
        }

        #region Metadata creataion functions
        private Metadata GetGameServiceMeta(IDbConnection connection, string paramValue)
        {
            Metadata meta = CreateDefaultMetadata();

            if (!paramValue.Contains(','))
            {
                string serviceName = GetServiceName(connection, paramValue);
                if (!String.IsNullOrEmpty(serviceName))
                {
                    meta.Title = String.Format(MetadataTemplates.SERVICE_TITLE_TEMPLATE, serviceName);
                    meta.Description = String.Format(MetadataTemplates.SERVICE_DESCRIPTION_TEMPLATE, serviceName);
                    meta.Canonical = CreateCanonical(FilterParams.PARAM_GAME_SERVICE, paramValue);
                }
            }

            if (meta.Canonical == null)
                meta.Canonical = CreateCanonical();

            return meta;
        }

        private Metadata GetGameTypeMeta(string paramValue)
        {
            Metadata meta = CreateDefaultMetadata();

            if (!FilterParams.GAME_TYPES.Contains(paramValue))
            {
                meta.Canonical = CreateCanonical();
                return meta;
            }

            string transKey = "type_" + paramValue;
            string translatedType = TranslationManager.GetInstance().GetTranslation(transKey, LANGUAGE_CODE);
            if (!String.IsNullOrEmpty(translatedType))
            {
                meta.Title = String.Format(MetadataTemplates.PRODTYPE_TITLE_TEMPLATE, translatedType);
                meta.Description = String.Format(MetadataTemplates.PRODTYPE_DESCRIPTION_TEMPLATE, translatedType);
            }
            
            meta.Canonical = CreateCanonical(FilterParams.PARAM_GAME_TYPE, paramValue);

            return meta;
        }

        private Metadata GetTagMeta(IDbConnection connection, string paramValue)
        {
            Metadata meta = CreateDefaultMetadata();
            if (!TagExists(connection, paramValue))
            {
                meta.Canonical = CreateCanonical();
                return meta;
            }

            string translatedTag = TranslationManager.GetInstance().GetTranslation(paramValue, LANGUAGE_CODE);
            if (!String.IsNullOrEmpty(translatedTag))
            {
                meta.Title = String.Format(MetadataTemplates.TAG_TITLE_TEMPLATE, translatedTag);
                meta.Description = String.Format(MetadataTemplates.TAG_DESCTIPTION_TEMPLATE, translatedTag);
            }

            meta.Canonical = CreateCanonical(FilterParams.PARAM_TAG, paramValue);

            return meta;
        }

        private Metadata GetDeveloperMeta(IDbConnection connection, string paramValue)
        {
            Metadata meta = CreateDefaultMetadata();

            if (DeveloperExists(connection, paramValue))
            {
                meta.Title = String.Format(MetadataTemplates.DEVELOPER_TITLE_TEMPLATE, paramValue);
                meta.Description = String.Format(MetadataTemplates.DEVELOPER_DESCRIPTION_TEMPLATE, paramValue);
                meta.Canonical = CreateCanonical(FilterParams.PARAM_DEVELOPER, paramValue);
            }
            else
            {
                meta.Canonical = CreateCanonical();
            }

            return meta;
        }

        private string GetServiceName(IDbConnection connection, string serviceCode)
        {
            string query = String.Format(SERVICE_NAME_QUERY, serviceCode);
            return connection.QueryFirstOrDefault<string>(query);
        }

        private bool DeveloperExists(IDbConnection connection, string developer)
        {
            string query = String.Format(DEVELOPER_QUERY, developer);
            return connection.ExecuteScalar<bool>(query);
        }

        private bool TagExists(IDbConnection connection, string tag)
        {
            string query = String.Format(TAG_QUERY, tag);
            return connection.ExecuteScalar<bool>(query);
        }
        #endregion
    }
}
