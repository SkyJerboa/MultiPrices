using System;
using System.Collections.Generic;
using System.Linq;

namespace MP.Core.PathCollector
{
    public class MPath
    {
        public MPath this[object key]
        {
            get { return Children.GetPath(key); }
        }

        public string Name { get; set; }
        public string Value
        {
            get
            {
                return (_value == null) 
                    ? null
                    : _value.ToString();
            }
            set
            {
                _value = value;
                _valueType = typeof(string);
            }
        }

        public MPath Parent { get; private set; }
        public MChildren Children { get; } = new MChildren();

        public string FullPath { get; private set; }
        //Если true, то при появление элемента с таким же индексом, данный путь изменит индекс на свободный
        public bool IsNewArrayElement { get; private set; }
        public bool IsRootPath { get { return Name == "Root" && Parent == null; } }
        public bool IsArray { get { return Children.Any(i => i.Index != null); } }
        public bool HasChildren { get { return Children.Count > 0; } }

        public int? Index
        {
            get { return _index; }
            set
            {
                if (value != null)
                {
                    if (value >= 0)
                    {
                        MPath sameIndexP = Parent.Children.FirstOrDefault(i => i.Index == value && i.IsNewArrayElement);
                        if (sameIndexP != null)
                        {
                            int? index = Parent.MaxChildIndex;
                            sameIndexP._index = index;
                        }
                        _index = value;
                    }
                    else if (value == -1)
                    {
                        IsNewArrayElement = true;
                        _index = (Parent.MaxChildIndex ?? -1) + 1;
                    }

                    Name = Parent.Name + "[" + Index.ToString() + "]";
                }

                UpdateFullPath();
            }
        }

        public int? MaxChildIndex
        {
            get
            {
                if (Children.Count > 0 && Children.All(i => i.Index != null))
                    return Children.Max(i => (int)i.Index);
                else
                    return null;
            }
        }

        public MPath RootPath
        {
            get
            {
                MPath pPath = this;
                while (!pPath.IsRootPath)
                    pPath = pPath.Parent;

                return pPath;
            }
        }

        private int? _index;
        private object _value;
        private Type _valueType;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="pathName">Локальное имя пути</param>
        /// <param name="fullPath">Полное имя пути</param>
        /// <param name="parent">Родительский путь</param>
        /// <param name="index">Индекс элемента</param>
        public MPath(string pathName, MPath parent = null, int? index = null)
        {
            Name = pathName;
            Parent = parent;

            if (index != null)
                _index = -1;

            if (Parent != null)
            {
                Parent.Children.Add(this);
            }

            Index = index;
            //UpdateFullPath();
        }

        protected void ChangeParent(MPath parent)
        {
            Parent.RemoveChild(this);
            Parent = parent;
            Parent.Children.Add(this);
            UpdateFullPath();
        }

        public void UpdateIndex(int? index)
        {
            if (Index == null)
                return;

            int oBracketIndex = Name.LastIndexOf('[');
            int cBracketIndex = Name.Length - 1;
            string beforeIndex = Name.Substring(0, oBracketIndex + 1);
            string newIndexString = index.ToString();
            string pathWithNewIndex = beforeIndex + newIndexString + "]";

            Name = pathWithNewIndex;
            Index = index;
            //UpdateFullPath();
        }

        /// <summary>
        /// Добавить нового чилда со смещением всех текущих чилдов вниз по иерархии
        /// </summary>
        /// <param name="child"></param>
        public void InsertChild(MPath child)
        {
            if (Children.Count > 0)
            {
                foreach (MPath c in Children.ToList())
                    c.ChangeParent(child);

                Children.Clear();
            }

            Children.Add(child);
            child.Parent = this;
            child.UpdateFullPath();
        }

        public void AddChild(string name, object value)
        {
            MPath findedPath = this[name];
            if (findedPath != null)
            {
                findedPath.SetValue(value);
                return;
            }

            MPath path = new MPath(name, this);
            path.SetValue(value);
            path.UpdateFullPath();
        }

        public void MoveChild(MPath child)
        {
            if (!Children.Contains(child))
            {
                if (child.Parent != null)
                    child.Parent.RemoveChild(child);

                Children.Add(child);
                child.Parent = this;
            }
            else
            {
                child.Parent?.RemoveChild(child);
            }
            child.UpdateFullPath();
        }

        public void RemoveChild(MPath child)
        {
            if (Children.Contains(child))
                Children.Remove(child);

            child.Parent = null;
            child.UpdateFullPath();
        }

        public void SetSameParent(MPath newParent)
        {
            if (Parent.FullPath != newParent.FullPath)
                return;

            Parent.Children.Remove(this);
            Parent = newParent;
            Parent.Children.Add(this);
        }

        public MPath FindInChildren(string subpath, string value = null)
        {
            string[] names = StringUtil.GetPathNames(subpath);
            MPath path = this;
            foreach(string name in names)
            {
                path = path[name];
                if (path == null)
                    return null;
            }

            if (value != null)
                return (path.Value == value) ? path : null;
            else
                return path;
        }

        public MPath FindInParent(string parentPath)
        {
            if (IsRootPath)
            {
                if (parentPath == "" || parentPath == "Root")
                    return this;
                else
                    return null;
            }

            MPath searchedPath = this;
            bool isCurrentPath = StringUtil.IsCurrentPathPattern(FullPath, parentPath);

            if (isCurrentPath)
                return this;
            else
                return Parent.FindInParent(parentPath);
                
        }

        public void Merge(MPath path, bool replaceValues = false)
        {
            foreach(MPath child in path.Children)
            {
                if (child.Index == null)
                {
                    MPath samePath = this[child.Name];
                    if (samePath == null)
                    {
                        child.GetCopy(this);
                    }
                    else
                    {
                        if (replaceValues && samePath.Value != null)
                            Value = samePath.Value;

                        //foreach (Path cChild in child.Children)
                        samePath.Merge(child, replaceValues);
                    }
                }
                else
                {
                    if (!Children.Contains(child))
                    {
                        MPath newP = child.GetCopy(this);
                        newP.Index = MaxChildIndex + 1;
                        //newP.IsNewArrayElement = true;
                    }
                }
            }
        }

        public MPath GetCopy(MPath newParent = null)
        {
            MPath path = new MPath(Name, newParent, Index) {
                _value = this._value,
                _valueType = this._valueType,
                IsNewArrayElement = this.IsNewArrayElement
            };
            foreach (MPath child in Children)
                child.GetCopy(path);

            return path;
        }

        public T GetValue<T>()
        {
            if (typeof(T) != _valueType)
                return (T)Convert.ChangeType(_value, typeof(T));

            return (T)_value;
        }

        public object GetValue() => _value;

        public void SetValue(object value)
        {
            if (value != null)
            {
                _valueType = value.GetType();
                _value = value;
            }
            else
            {
                _value = null;
            }
        }

        public List<MPath> GetArrays()
        {
            List<MPath> arrayPaths = new List<MPath>();
            MPath pPath = Parent;
            while (!pPath.IsRootPath)
            {
                if (pPath.IsArray)
                    arrayPaths.Add(pPath);

                pPath = pPath.Parent;
            }

            return arrayPaths;
        }

        private void UpdateFullPath()
        {
            if (IsRootPath)
            {
                FullPath = "";
            }
            else
            {
                string fullName = ((Parent == null || Parent.IsRootPath) 
                    ? "" 
                    : Parent.FullPath) + ((Index == null) 
                        ? "." + Name 
                        : "[" + Index.ToString() + "]");

                if (fullName[0] == '.')
                    fullName = fullName.Remove(0, 1);

                FullPath = fullName;
            }

            foreach (MPath child in Children)
                child.UpdateFullPath();

            //string fullName = "";
            //Path lastPath = this;
            //while (!lastPath.IsRootPath)
            //{
            //    fullName = (lastPath.Index == null)
            //        ? "." + lastPath.Name + fullName
            //        : "[" + lastPath.Index + "]" + fullName;
            //    lastPath = lastPath.Parent;
            //}
            //if (fullName[0] == '.')
            //    fullName = fullName.Remove(0, 1);

        }

        public MPath CreateNewArrayElement()
        {
            int? maxIndex = MaxChildIndex;
            if (maxIndex == null && Children.Count > 0)
                throw new Exception("Can't add index to not array element");

            return new MPath(null, this, (maxIndex ?? -1) + 1);
        }

        public bool ContainsKey(string key) => Children.Any(i => i.Name == key && i.Value != null);

        #region overrides
        public override string ToString()
        {
            string str;

            if (IsRootPath)
            {
                str = "Root";
            }
            else
            {
                str = $"FullPath = {FullPath}; Name = {Name}";
                if (Value != null)
                    str += $"; Value = {Value}";
            }

            return str;
        }

        public static bool operator == (MPath p1, MPath p2)
        {
            if (p1 is null)
                return (p2 is null);
            else if (p2 is null)
                return false;

            if (p1.Value != p2.Value)
                return false;

            if (p1.Index != null)
            {
                if (p2.Index == null)
                    return false;

                if (p1.Parent.Name != p2.Parent.Name)
                    return false;
            }
            else
            {
                if (p1.Name != p2.Name)
                    return false;
            }

            int childCount = p1.Children.Count;
            if (childCount != p2.Children.Count)
                return false;

            for(int i = 0; i < childCount; i++)
            {
                if (p1.Children[i] != p2.Children[i])
                    return false;
            }

            return true;
        }

        public static bool operator != (MPath p1, MPath p2)
        {
            return !(p1 == p2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MPath))
                return false;

            return this == (obj as MPath);
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = unchecked((hash * 7) + Name.GetHashCode());

            foreach(MPath child in Children)
                hash = unchecked((hash * 7) + child.GetHashCode());

            return hash;
        }
        #endregion

    }
}
