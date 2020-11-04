using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MP.Core.Helpers
{
    public class ListToJsonStringConverter : ValueConverter<IList<string>, string>
    {
        private static readonly Expression<Func<string, IList<string>>> ToList
            = str => JsonConvert.DeserializeObject<IList<string>>(str);

        private static readonly Expression<Func<IList<string>, string>> ToStringValue
            = list => JsonConvert.SerializeObject(list);

        public ListToJsonStringConverter(ConverterMappingHints mappingHints = null)
            : base(ToStringValue, ToList, mappingHints) { }

        public static ValueConverterInfo DefaultInfo
          => new ValueConverterInfo(typeof(IList<string>), typeof(string),
            i => new DictionaryToStringConverter(i.MappingHints));
    }
}
