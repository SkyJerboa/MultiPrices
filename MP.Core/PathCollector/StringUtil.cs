using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MP.Core
{
    public static class StringUtil
    {
        public const string ARRAY_PATTERN = "[*]";

        readonly static Dictionary<string, string> ReplacesDictionary = new Dictionary<string, string>
        {
            { "[", @"\[" },
            { "]", @"\]" },
            { "*", @"[0-9]+" },
            { ".", @"\." }
        };

        readonly static char[] FirstSymbolsRemoved = new char[]
        { '$', '.' };

        public static string CreatePattern(this string input)
        {
            string output = input.Trim();

            foreach (KeyValuePair<string, string> pattern in ReplacesDictionary)
                output = output.Replace(pattern.Key, pattern.Value);

            return "^" + output + "$";
        }

        /// <summary>
        /// Заменяет [*] на определенный индекс по паттерну строк
        /// </summary>
        /// <param name="patternFrom">паттерн, откуда копируем</param>
        /// <param name="patternTo">паттерн, куда копируем</param>
        /// <param name="fromStr">строка, откуда подставляются значения в patternTo</param>
        /// <returns></returns>
        public static string IndexSubstitution(string patternFrom, string patternTo, string fromStr)
        {
            string[] pFromArr = patternFrom.Split('.');
            string[] pToArr = patternTo.Split('.');
            string[] fStrArr = fromStr.Split('.');

            for (int i = 0; i < pToArr.Length; i++)
            {
                if (IsPatternEquals(pFromArr[i], pToArr[i]))
                    pToArr[i] = fStrArr[i];
                else
                    break;
            }

            string result = String.Join(".", pToArr);

            return (result.Contains("[*]")) ? null : result;



            bool IsPatternEquals(string pat1, string pat2)
            {
                if (pat1 == pat2)
                    return true;

                int bracketIndex = pat1.IndexOf("[");
                if (bracketIndex != pat2.IndexOf("[") || bracketIndex < 0)
                    return false;

                return pat1.Substring(0, bracketIndex) == pat2.Substring(0, bracketIndex);
            }
        }

        /// <summary>
        /// заменяет паттерн массива на конкретный индекс. В обоих паттернах должно быть одинаковое количество 
        /// изменяемых индексов массивов.
        /// </summary>
        /// <param name="patternFrom">Паттерн, откуда берем строку</param>
        /// <param name="patternTo">Паттерн, куда подставляем индекс</param>
        /// <param name="fromString">сформированная строка паттерна from</param>
        /// <returns></returns>
        public static string SetIndexFromAnotherArray(string patternFrom, string patternTo, string fromString)
        {
            string to = patternTo;
            string replaceablePattern = patternFrom;

            while (to.Contains("[*]"))
            {
                int asteriskIndex = replaceablePattern.IndexOf("[*]") + 1;

                int numberSize = 0;
                string numberStr = "";
                char digitChar = fromString[asteriskIndex];

                while (Char.IsDigit(digitChar))
                {
                    numberStr += digitChar;
                    numberSize++;
                    digitChar = fromString[asteriskIndex + numberSize];
                }

                replaceablePattern = replaceablePattern.Remove(asteriskIndex, 1);
                replaceablePattern = replaceablePattern.Insert(asteriskIndex, numberStr);

                asteriskIndex = to.IndexOf("[*]") + 1;
                to = to.Remove(asteriskIndex, 1);
                to = to.Insert(asteriskIndex, numberStr);
            }

            return to;
        }


        /// <summary>
        /// Возвращает количество совпадений при поиске подстроки
        /// </summary>
        /// <param name="mainString">строка, в котрой производится поиск</param>
        /// <param name="sequence">искомая последовательность символов</param>
        /// <returns></returns>
        public static int GetSequenceCount(string mainString, string sequence)
        {
            Regex reg = new Regex(sequence);
            return reg.Matches(mainString).Count;
        }

        /// <summary>
        /// Преобразует строку поиска по джейсону в строку пути коллектора
        /// </summary>
        /// <param name="dataPath">строка поиска по джейсону</param>
        /// <returns></returns>
        public static string ClearDataPath(string dataPath)
        {
            while(FirstSymbolsRemoved.Contains(dataPath[0]))
            {
                dataPath = dataPath.Remove(0, 1);
            }
            dataPath = dataPath.Replace("[new]", "[*]");

            return dataPath;
        }

        /// <summary>
        /// Разбивает строки на подстроки, выделяя имена всех путей
        /// </summary>
        /// <param name="fullPath">полный путь</param>
        /// <returns></returns>
        public static string[] GetPathNames(string fullPath)
        {
            string[] subPaths = fullPath.Split('.');

            if (!fullPath.Contains("["))
                return subPaths;

            List<string> allNames = new List<string>();

            foreach(string path in subPaths)
            {
                if (path.EndsWith("]"))
                {
                    int bracketIndex = path.LastIndexOf('[');
                    string arrayName = path.Substring(0, bracketIndex);
                    allNames.Add(arrayName);
                }
                allNames.Add(path);
            }

            return allNames.ToArray();
        }

        /// <summary>
        /// Удовлетворяет ли передеваемый путь заданному патерну
        /// </summary>
        /// <param name="pathName">полный путь</param>
        /// <param name="pattern">паттерн, с которым сравнивается путь</param>
        /// <returns></returns>
        public static bool IsCurrentPathPattern(string pathName, string pattern)
        {
            string nPattern = CreatePattern(pattern);
            Regex reg = new Regex(nPattern);
            return reg.IsMatch(pathName);
        }

        /// <summary>
        /// Ищет общего родителя среди предложенных путей
        /// </summary>
        /// <param name="paths">Список путей, у которых будет искаться общий родитель</param>
        /// <param name="searchInArrays">Если true, то обрезает результирующую строку по [*]</param>
        /// <returns></returns>
        public static string GetCommonParentPath(string[] paths, bool searchInArrays = false)
        {
            IEnumerable<string[]> splitedPaths = paths.Select(i => i.Split('.'));
            int minPaths = splitedPaths.Min(i => i.Length);
            string commonParent = "";

            for (int i = 0; i < minPaths; i++)
            {
                string currentSubpath = splitedPaths.First()[i];
                if (splitedPaths.All(o => o[i] == currentSubpath))
                    commonParent += "." + currentSubpath;
                else
                    break;
            }

            if (commonParent.Length > 0 && commonParent[0] == '.')
                commonParent = commonParent.Remove(0, 1);

            if (searchInArrays && !commonParent.EndsWith("]"))
            {
                if (!commonParent.Contains("]"))
                    return "";

                int lastBracketIndex = commonParent.LastIndexOf(']');
                return commonParent.Substring(0, lastBracketIndex + 1);
            }

            return commonParent;
        }
        public static string GetCommonParentPath(params string[] paths) => GetCommonParentPath(paths, false);
        public static string GetCommonParentPathArray(params string[] paths) => GetCommonParentPath(paths, true);

        /// <summary>
        /// заменяет в строке паттерн массива на определенные индексы, взятые из первой строки.
        /// Строки должны быть одного паттерна да последнего массива
        /// </summary>
        /// <param name="from">Строка, откуда берутся индексы</param>
        /// <param name="to">Строка, куда подставляются индексы</param>
        /// <returns></returns>
        public static string ArraySubstitutionWithTwoStr(string from, string to)
        {
            string newPath = null;

            do
            {
                int index = from.IndexOf('[');

                if (index == -1 || from.Substring(0, index) != to.Substring(0, index))
                    break;

                if (!Char.IsDigit(to[index]))
                {
                    int fromCloseBracketIndex = from.IndexOf(']');
                    string arrIndex = from.Substring(index + 1, fromCloseBracketIndex - index - 1);
                    from = from.Substring(fromCloseBracketIndex + 1);

                    int toCloseBracketIndex = to.IndexOf(']');
                    newPath += to.Substring(0, index + 1) + arrIndex + "]";
                    to = to.Substring(toCloseBracketIndex + 1);
                }
            }
            while (to.Contains(StringUtil.ARRAY_PATTERN));

            newPath += to;

            return newPath;
        }


        /// <summary>
        /// Делет путь на левую и правую часть. Возвращает правую.
        /// </summary>
        /// <param name="fullPath">Разделяемый путь</param>
        /// <param name="leftPart">Левая часть пути, которая будет отсечена</param>
        /// <returns>Оставшаяся правая часть пути</returns>
        public static string GetRightPathPart(this string fullPath, string leftPart)
        {
            if (string.IsNullOrEmpty(leftPart))
                return null;

            int leftPartLength = leftPart.Length;
            if (fullPath.Length <= leftPartLength || !fullPath.StartsWith(leftPart))
                return null;

            string rightPath = fullPath.Substring(leftPartLength);

            return (rightPath.StartsWith(".")) ? rightPath.Substring(1) : rightPath;
        }

        /// <summary>
        /// Заменяет последнее вхождение фразы в выражение
        /// </summary>
        /// <param name="str">Исходная строка</param>
        /// <param name="find">Что заменяем</param>
        /// <param name="replace">На что заменяем</param>
        /// <returns></returns>
        public static string ReplaceLastOccurrence(this string str, string find, string replace)
        {
            int place = str.LastIndexOf(find);

            if (place == -1)
                return str;

            string result = str.Remove(place, find.Length).Insert(place, replace);
            return result;
        }
    }
}
