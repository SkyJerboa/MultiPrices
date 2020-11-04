using Dapper;
using Newtonsoft.Json;
using System.Data;

namespace MP.Client.Common.DapperMapper
{
    public class StringArrayHandler : SqlMapper.TypeHandler<string[]>
    {
        public override string[] Parse(object value)
        {
            return JsonConvert.DeserializeObject<string[]>(value.ToString());
        }

        public override void SetValue(IDbDataParameter parameter, string[] value)
        {
            throw new System.NotImplementedException();
        }
    }
}
