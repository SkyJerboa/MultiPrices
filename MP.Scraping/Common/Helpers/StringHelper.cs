using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MP.Scraping.Common.Helpers
{
    public static class StringHelper
    {
        private const string DIRECTX_PATTERN = @"[1-9]{1}[0-9]?(\.[0-9]{1,2})?[a-zA-Z]?";

        public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);

        public static T ToEnum<T>(this string value) where T : struct
        {
            Enum.TryParse<T>(value, true, out T nEnum);
            return nEnum;
        }

        public static string ClearNumber(this string str) => String.Concat(str.Where(i => Char.IsDigit(i)));

        public static string ClearDate(this string str)
        {
            if (String.IsNullOrEmpty(str) || !str.Any(char.IsDigit))
                return null;

            char[] allowedChars = { '/', '.', ' ', ':' };
            string res = String.Concat(str.Where(i => Char.IsDigit(i) || allowedChars.Contains(i)));

            while (!Char.IsDigit(res[0]))
                res = res.Remove(0, 1);
            while (!Char.IsDigit(res[res.Length - 1]))
                res = res.Remove(res.Length - 1);

            return res;
        }

        public static string ClearPrice(this string str)
        {
            if (str == null)
                return null;

            Stack<char> price = new Stack<char>();
            byte digitCharCount = 0;
            for (int i = str.Length - 1; i >= 0; i--)
            {
                char curChar = str[i];
                if (digitCharCount == 2 && (curChar == ',' || curChar == '.'))
                {
                    digitCharCount++;
                    price.Push(',');
                    continue;
                }
                if (Char.IsDigit(curChar))
                {
                    digitCharCount++;
                    price.Push(curChar);
                }
            }

            return new string(price.ToArray());
        }

        public static string ClearName(this string name)
        {
            if (name.IsNullOrEmpty())
                return name;

            Regex reg = new Regex("®|™");
            return reg.Replace(name, String.Empty);
        }

        public static string RemoveLineBreaks(this string str)
        {
            Regex reg = new Regex("\r|\n|\r\n");
            return reg.Replace(str, String.Empty);
            //str.Replace("\n", String.Empty);
        }

        public static string RemoveTabulation(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            Regex reg = new Regex("\t");
            return reg.Replace(str, String.Empty);
        }

        public static string RemoveMultipleWhitespaces(this string str)
        {
            Regex reg = new Regex("[ ]{2,}");
            string res = reg.Replace(str, " ");

            if (res[0] == ' ')
                res = res.Substring(1);
            if (res.Length > 0 && res[res.Length - 1] == ' ')
                res = res.Remove(res.Length - 1);

            return res;
        }

        public static string GetString(this JToken token) => token?.Value<string>();
        public static string GetFirstString(this IEnumerable<JToken> tokens) 
        {
            if (tokens == null || tokens.Count() == 0)
                return null;
            
            return tokens.First().GetString(); 
        }

        public static string CreateGuidString()
        {
            Guid g = Guid.NewGuid();
            string str = g.ToString("N");

            return str;
        }

        public static string ClearHtmlText(this string text)
        {
            //string res = Regex.Replace(text, @"<br>", "\n");
            return Regex.Replace(text, @"<.*?>", String.Empty);
        }

        public static string ClearMemoryString(string str)
        {
            if (str == null)
                return null;

            str = str.ToUpper();
            string memory = "";
            if (str.Contains("MB"))
                memory = "MB";
            else if (str.Contains("GB"))
                memory = "GB";
            else
            { }

            str = $"{str.ClearNumber()} {memory}";
            if (str == " ")
                str = String.Empty;

            if (str.Length > 10)
                str = str.Substring(0, 10);

            return str;
        }

        public static string ClearDirectX(string str)
        {
            if (str == null)
                return null;

            if (str.Length <= 20)
                return str;

            Regex reg = new Regex(DIRECTX_PATTERN);
            string version = reg.Match(str)?.Value;

            return (version == null) ? null : $"DirectX {version}";
        }

        //пока работает с простыпи json объектами, не работает с массивами
        //валидация отсутствует
        public static string ToPostgreJsonFormat(this string str)
        {
            if (str == null)
                return null;

            string pattern = @"(""[\S]+?"":)([\S])";
            str = Regex.Replace(str, pattern, "$1 $2");
            str = Regex.Replace(str, ",", ", ");

            return str;
        }

        public static string GetFileExtensionByMimeType(string type)
        {
            switch (type)
            {
                case "image/jpg":
                case "image/jpeg":
                    return "jpg";
                case "image/png":
                    return "png";
                default:
                    throw new Exception("type not found: " + type);
            }
        }
    }
}
