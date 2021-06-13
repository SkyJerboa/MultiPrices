using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MP.Core.Common.Heplers
{
    public static class StringHelper
    {
        public static string CreateOneLineString(this string str)
        {
            var normalizedString = str.Normalize(NormalizationForm.FormD);
            char[] charArr = normalizedString.ToArray();
            StringBuilder strBuilder = new StringBuilder();
            bool whiteSpace = false;

            foreach (char c in charArr)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);

                if (uc == UnicodeCategory.DecimalDigitNumber)
                {
                    strBuilder.Append(c);
                    whiteSpace = false;
                }
                else if (uc == UnicodeCategory.LowercaseLetter || uc == UnicodeCategory.UppercaseLetter)
                {
                    strBuilder.Append(Char.ToLower(c));
                    whiteSpace = false;
                }
                else if (!whiteSpace && (uc == UnicodeCategory.SpaceSeparator || c == '_' || c == '-'))
                {
                    strBuilder.Append('-');
                    whiteSpace = true;
                }
            }

            if (strBuilder.Length == 0)
                return null;

            if (strBuilder[0] == '-')
                strBuilder.Remove(0, 1);
            if (strBuilder[strBuilder.Length - 1] == '-')
                strBuilder.Remove(strBuilder.Length - 1, 1);

            return strBuilder.ToString();
        }

        public static string SetValuesToString<T>(this string s, T values, string prefix = "$")
        {
            var sb = new StringBuilder(s);
            foreach (var p in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                sb = sb.Replace(prefix + p.Name, p.GetValue(values, null).ToString());
            }
            return sb.ToString();
        }

        public static string SetValuesToString(this string str, params string[] keyValues)
        {
            if (keyValues.Length == 0 || keyValues.Length % 2 != 0)
                return null;

            string res = str;

            for (int i = 0; i < keyValues.Length; i += 2)
                res = res.Replace($"${keyValues[i]}", keyValues[i + 1]);

            return res;
        }

        public static string ChangeBackslash(this string source) => source.Replace("\\", "/");
    }
}
