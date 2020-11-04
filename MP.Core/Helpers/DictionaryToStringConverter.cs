using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MP.Core.Helpers
{
    public class DictionaryToStringConverter : ValueConverter<Dictionary<string, object>, string>
    {
        private static readonly Expression<Func<string, Dictionary<string, object>>> ToDictionary
            = str => JsonConvert.DeserializeObject<Dictionary<string, object>>(str);

        private static readonly Expression<Func<Dictionary<string, object>, string>> ToStringValue
          = dict => JsonConvert.SerializeObject(dict, new JsonSerializerSettings
              {
                  Converters = new List<JsonConverter>() { new Newtonsoft.Json.Converters.StringEnumConverter() }
              });

        public DictionaryToStringConverter(ConverterMappingHints mappingHints = null)
          : base(ToStringValue, ToDictionary, mappingHints) { }

        public static ValueConverterInfo DefaultInfo
          => new ValueConverterInfo(typeof(Dictionary<string, object>), typeof(string),
            i => new DictionaryToStringConverter(i.MappingHints));
    }
}
