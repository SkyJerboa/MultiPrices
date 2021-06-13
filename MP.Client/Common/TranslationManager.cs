using Dapper;
using MP.Client.Common.Configuration;
using MP.Core.Common;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MP.Client.Common
{
    public class TranslationManager
    {
        private static TranslationManager _translationManager;

        private const string TRANS_QUERY = @"SELECT ""Value"" FROM ""SiteTranslations"" WHERE ""Key"" = '{0}' AND ""LanguageCode"" = '{1}'";
        private const string TRANS_WITH_DEFAULT_LANG_QUERY = @"SELECT ""Key"", ""Value"" FROM ""SiteTranslations"" WHERE ""Key"" = '{0}' AND ""LanguageCode"" in ('{1}', 'en')";
        
        private NpgsqlConnection _dbConnection;

        private TranslationManager()
        {
            string connStr = SiteConfigurationManager.Config.DefaultConnection;
            _dbConnection = new NpgsqlConnection(connStr);
        }

        public static TranslationManager GetInstance()
        {
            if (_translationManager == null)
                _translationManager = new TranslationManager();

            return _translationManager;
        }

        public string GetTranslation(string key, string languageCode, bool withDefaultLang = true)
        {
            string query = (withDefaultLang)
                ? String.Format(TRANS_QUERY, key, languageCode)
                : String.Format(TRANS_WITH_DEFAULT_LANG_QUERY, key, languageCode);

            if (withDefaultLang)
            {
                IEnumerable<Translation> res = _dbConnection.Query<Translation>(query);
                if (res.Count() == 2)
                    return res.First(i => i.LanguageCode == languageCode).Value;
                else if (res.Count() == 1)
                    return res.First().Value;
                else
                    return null;
            }
            else
            {
                return _dbConnection.QueryFirstOrDefault<string>(query);
            }
        }
    }
}
