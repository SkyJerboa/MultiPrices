using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq.Expressions;

namespace MP.Core.Helpers
{
    public class StringArrayToStringConverter : ValueConverter<string[],string>
    {
        private static readonly Expression<Func<string, string[]>> ToStringArray
            = str => str.Split('|', StringSplitOptions.RemoveEmptyEntries);

        private static readonly Expression<Func<string[], string>> ToStringValue
          = list => string.Join('|', list);

        public StringArrayToStringConverter(ConverterMappingHints mappingHints = null)
          : base(ToStringValue, ToStringArray, mappingHints) { }

        public static ValueConverterInfo DefaultInfo
          => new ValueConverterInfo(typeof(string[]), typeof(string),
            i => new StringArrayToStringConverter(i.MappingHints));
    }
}
