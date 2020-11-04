using Dapper;
using MP.Core.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;

namespace MP.Client.Common.DapperMapper
{
    public class LanguagesHandler : SqlMapper.TypeHandler<Dictionary<string, Localization>>
    {
        public override Dictionary<string, Localization> Parse(object value)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, Localization>>(value.ToString());
        }

        public override void SetValue(IDbDataParameter parameter, Dictionary<string, Localization> value)
        {
            throw new System.NotImplementedException();
        }
    }
}
