using System;

namespace MP.Scraping.Common.Helpers
{
    public class ConvertHelper
    {
        public static T Convert<T>(object value)
        {
            Type t = typeof(T);

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                    return default(T);

                t = Nullable.GetUnderlyingType(t);
            }
            else if (t.IsEnum)
            {
                return (T)Enum.Parse(typeof(T), value.ToString());
            }

            return (T)System.Convert.ChangeType(value, t);
        }
    }
}
