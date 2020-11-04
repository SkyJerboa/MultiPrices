using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MP.Core.PathCollector
{
    public class MContainer
    {
        public string Name { get; private set; }

        public MPath RootPath { get; private set; }

        public bool IsEmpty { get { return RootPath?.Children.Count == 0; } }

        public static MContainer CreateGamesContainer()
        {
            MContainer container = new MContainer();
            MPath path = new MPath("games", container.RootPath);
            return container;
        }

        /// <summary>
        /// Не использовать
        /// </summary>
        public List<MPath> AllPaths { get { return GetAllPaths(); } }

        private IEnumerable<MPath> Flatten(IEnumerable<MPath> paths)
        {
            return paths.SelectMany(c => Flatten(c.Children)).Concat(paths);
        }

        public MContainer(string name = null)
        {
            RootPath = new MPath("Root");

            if (name != null)
                Name = name;
        }

        //создать контейнер на основе списка jtokens
        public MContainer(List<JToken> jTokens, string name = null) : this(name)
        {
            foreach (JToken jt in jTokens)
                AddPath(jt.Path, jt.Value<string>());
        }

        public void AddTokens(List<JToken> jTokens, ResolverOptions options = ResolverOptions.NoOptions)
        {
            foreach (JToken jt in jTokens)
                AddPath(jt.Path, jt.Value<string>(), options: options);
        }

        public void AddPathsFromJObject(JObject jObject)
        {
            JEnumerable<JToken> jTokens = jObject.PropertyValues();
            ExploreTokens(jTokens);

            void ExploreTokens(JEnumerable<JToken> tokens)
            {
                foreach(JToken token in tokens)
                {
                    JEnumerable<JToken> children = token.Children();
                    if (children.Count() == 0)
                        AddPath(token.Path, token.Value<string>());
                    else
                        ExploreTokens(children);
                }
            }
        }

        public MPath AddPath(string path, object value, string startPath = null, ResolverOptions options = ResolverOptions.NoOptions)
        {
            MPath prevPath = (startPath == null)
                ? RootPath
                : GetPath(startPath) ?? RootPath;

            return AddPath(path, value, prevPath, options);
        }

        public MPath AddPath(string path, object value, MPath startPath, ResolverOptions options = ResolverOptions.NoOptions)
        {
            MPath prevPath = (startPath == null) ? RootPath : startPath;

            string[] subpaths = path.Split('.');
            string pFullPath = (prevPath.IsRootPath) ? "" : prevPath.FullPath + ".";

            for (int i = 0; i < subpaths.Length; i++)
            {
                string currentPath = subpaths[i];
                pFullPath += (i == 0) ? currentPath : "." + currentPath;

                int? index = CheckArrayElement(pFullPath, currentPath, ref prevPath);

                if (index != null && index < 0)
                {
                    if (index == -1)
                    {
                        int maxIndex = (prevPath.MaxChildIndex ?? -1) + 1;
                        pFullPath = ChangeArrayIndex(pFullPath, maxIndex);
                        currentPath = ChangeArrayIndex(currentPath, maxIndex);
                    }
                    else if (index == -2)
                    {
                        List<MPath> paths = GetPaths(pFullPath);
                        foreach (MPath p in paths)
                            AddPath(String.Join(".", subpaths.Skip(i + 1)), value, p, options);

                        return RootPath;
                    }
                    else if (index == -3)
                    {
                        currentPath = currentPath.Replace("[\'", ".").Replace("\']", String.Empty);
                        currentPath = currentPath.Substring(currentPath.LastIndexOf('.') + 1);
                        pFullPath = pFullPath.Replace("[\'", ".").Replace("\']", String.Empty);
                        index = null;
                    }
                }

                if (options.HasFlag(ResolverOptions.ExpandArray) && index != null && index <= prevPath.MaxChildIndex)//можно переписать с рассчетом на то, что индекс будет задаваться автоматически, а не браться изначально из пути
                {
                    index = (prevPath.MaxChildIndex ?? -1) + 1;
                    pFullPath = ChangeArrayIndex(pFullPath, (int)index);
                    currentPath = ChangeArrayIndex(currentPath, (int)index);
                }

                SetPath(pFullPath, currentPath, ref prevPath, index);

                if (i == subpaths.Length - 1 && (prevPath.Value == null || options.HasFlag(ResolverOptions.ReplaceValues)))
                    prevPath.SetValue(value);
            }

            return prevPath;
        }

        //добавляет экземпляр пути вместе с его родителями
        public void AddPath(MPath path)
        {
            Stack<MPath> pathStack = new Stack<MPath>();
            MPath pPath;
            do
            {
                pPath = path.Parent;
                pathStack.Push(path);
                path = pPath;
            } while (!pPath.IsRootPath);

            MPath sPath, lastPath = RootPath;
            do
            {
                sPath = pathStack.Pop();
                pPath = GetPath(sPath.FullPath);
                if (pPath != null)
                    lastPath = pPath;
            } while (pPath != null);

            sPath.SetSameParent(lastPath);
        }

        //добавляет экземпляр пути к определенному пути
        public void AddPath(MPath path, string pathTo)
        {
            MPath pTo = GetPath(pathTo);

            if (pTo == null)
                return;

            path.GetCopy(pTo);
        }

        #region GetPathCollection
        public MPath GetPath(string path)
        {
            if (path == "Root" || path == "")
                return RootPath;

            string[] subpaths = StringUtil.GetPathNames(path);
            MPath pPath = RootPath;
            for (int i = 0; i < subpaths.Length; i++)
            {
                pPath = pPath.Children.FirstOrDefault(o => o.Name == subpaths[i]);
                if (pPath == null)
                    return null;
            }

            return pPath;
        }

        public MPath GetPathInParent(MPath path, string regexPath)
        {
            if (regexPath == "")
                return RootPath;

            while (!Regex.IsMatch(path.FullPath, regexPath) && !path.IsRootPath)
                path = path.Parent;

            return path;
        }

        public List<MPath> GetPaths(string regexPath)
        {
            string pattern = StringUtil.CreatePattern(regexPath);
            return Flatten(RootPath.Children).Where(i => Regex.IsMatch(i.FullPath, pattern)).ToList();
        }

        public List<MPath> GetPaths(Func<MPath,bool> condition) => GetAllPaths().Where(condition).ToList();

        public List<MPath> GetSubPaths(string parentPath)
        {
            if (parentPath == "Root" || parentPath == "")
                return GetAllPaths();

            MPath pPath = GetPath(parentPath);
            return Flatten(pPath.Children).OrderBy(i => i.FullPath).ToList();
        }
        //=> (parentPath == "Root")
        //    ? AllPaths.ToList()
        //    : AllPaths.Where(i => i.FullPath.StartsWith(parentPath) && i.FullPath != parentPath).ToList();

        /// <summary>
        /// Возвращает пути по заданным условиям.
        /// paths[*] id = 4, name = "myname"
        /// </summary>
        /// <param name="returnedPath">Путь возвращаемых путей</param>
        /// <param name="conditions">Список условий для поисков путей</param>
        /// <returns>Лист путей</returns>
        public IEnumerable<MPath> GetPathsByConditions(string returnedPath, List<KeyValuePair<string,string>> conditions)
        {
            List<MPath> commonPaths = GetPaths(returnedPath);
            foreach (KeyValuePair<string, string> condition in conditions)
            {
                commonPaths = commonPaths.Where(i => i.FindInChildren(condition.Key, condition.Value) != null).ToList();
            }
            return commonPaths;
        }
        #endregion

        /// <summary>
        /// Ищет значения по заданным путям и подставляет их список параметров
        /// </summary>
        /// <param name="paths">список путей</param>
        /// <returns>Возвращает список подставляемых параметров</returns>
        public List<List<string>> CreateUrlParamsFromPaths(string[] paths)
        {
            if (paths == null || paths.Count() == 0)
                return null;

            List<List<string>> allUrlParams = new List<List<string>>();
            paths = paths.Select(i => StringUtil.ClearDataPath(i)).ToArray();

            IEnumerable<IGrouping<string, string>> arraysGroups = paths
                .GroupBy(i =>
                {
                    int index = i.LastIndexOf(StringUtil.ARRAY_PATTERN) + StringUtil.ARRAY_PATTERN.Length;
                    return i.Substring(0, index);
                })
                .OrderByDescending(i =>
                {
                    return (i.Key.Length - i.Key.Replace(StringUtil.ARRAY_PATTERN, "").Length) / StringUtil.ARRAY_PATTERN.Length;
                });

            List<MPath> lowerPaths = GetPaths(arraysGroups.First().Key);

            foreach (MPath lowerPath in lowerPaths)
            {
                List<string> urlParams = new List<string>();
                List<int> addedPathsOrder = new List<int>();
                MPath currentParent = lowerPath;

                foreach (var arraysGroup in arraysGroups)
                {
                    int parentStrLen = arraysGroup.Key.Length;
                    currentParent = currentParent.FindInParent(arraysGroup.Key);

                    foreach(string fullPatnName in arraysGroup)
                    {
                        string subpath = fullPatnName.Substring(parentStrLen + 1);
                        MPath searchedPath = currentParent.FindInChildren(subpath);
                        urlParams.Add(searchedPath?.Value ?? "");
                        addedPathsOrder.Add(Array.IndexOf(paths, fullPatnName));
                    }
                }

                urlParams.OrderBy(i =>
                    {
                        int index = urlParams.IndexOf(i);
                        return addedPathsOrder[index];
                    });

                allUrlParams.Add(urlParams);
            }

            return allUrlParams;
        }

        public List<MPath> GetAllPaths()
        {
            return Flatten(RootPath.Children).ToList();
        }

        public MContainer GetCopy()
        {
            MPath copyPath = RootPath.GetCopy();
            MContainer copyCon = new MContainer();

            foreach (MPath child in copyPath.Children)
                copyCon.AddPath(child, "Root");

            return copyCon;
        }

        /// <summary>
        /// Проверяет строку на наличие в ней ссылки на элемент массива
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="pathName"></param>
        /// <param name="prevPath"></param>
        /// <returns>
        /// null - массив в строке не найдет
        /// >= 0 - номер элемента, найденного в строке
        /// -1 - строка содержит новый элемент массива, помеченный как [new]
        /// -2 - строка содержит символ * для поиска всех элементов массива
        /// -3 - строка содержит имя пути, содержащее спецсимвол
        /// </returns>
        private int? CheckArrayElement(string fullPath, string pathName, ref MPath prevPath)
        {
            if (!fullPath.EndsWith("]"))
                return null;

            int openingBracketIndex = pathName.LastIndexOf("[");
            int indexLength = pathName.Length - openingBracketIndex - 2;
            string indexString = pathName.Substring(openingBracketIndex + 1, indexLength);

            bool indexNew = (indexString == "new");
            bool indexEvery = (indexString == "*");
            bool specialName = (indexString.StartsWith("\'") && indexString.EndsWith("\'"));

            if (indexString.All(Char.IsDigit) || indexNew || indexEvery || specialName)
            {
                int fPathWithoutIndexLength = fullPath.Length - indexLength - 2;
                string nameWithoutIndex = pathName.Substring(0, openingBracketIndex);
                string fPathWithoutIndex = fullPath.Substring(0, fPathWithoutIndexLength);

                int? index = CheckArrayElement(fPathWithoutIndex, nameWithoutIndex, ref prevPath);
                SetPath(fPathWithoutIndex, nameWithoutIndex, ref prevPath, index);

                if (indexNew)
                    return -1;
                else if (indexEvery)
                    return -2;
                else if (specialName)
                    return -3;
                else
                    return Int32.Parse(indexString);
            }

            return null;
        }

        private string ChangeArrayIndex(string path, int index)
        {
            int bracketIndex = path.LastIndexOf('[');
            return path.Substring(0, bracketIndex + 1) + index.ToString() + "]";
        }

        private void SetPath(string fullPath, string pathName, ref MPath prevPath, int? index = null)
        {
            if (fullPath == "" && prevPath.IsRootPath)
                return;

            MPath existingPath = prevPath.Children.FirstOrDefault(i => i.FullPath == fullPath);

            if (existingPath == null)
            {
                MPath p = new MPath(pathName, prevPath, index);
                prevPath = p;
            }
            else
            {
                prevPath = existingPath;
            }

            //if(index != null)
            //    prevPath.Index = index;
        }
    }

    [Flags]
    public enum ResolverOptions
    {
        NoOptions = 0,
        ExpandArray = 1, //Если на пути встречается массив, то он расширяется
        ReplaceValues = 2, //Заменяет значения у конечных путей, если они уже существуют
        CreateArray = 4 //Преобразует объекты одного пути в массив по заданному пути
    }
}
