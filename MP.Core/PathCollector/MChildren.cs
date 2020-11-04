using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MP.Core.PathCollector
{
    public class MChildren : IEnumerable<MPath>, IList<MPath>
    {
        private const int ARRAY_SIZE = 16;

        private int _childrenCount = 0;
        private bool _filled = true;
        
        private MPath[] _children = null;

        public int Count => _childrenCount;

        public bool IsReadOnly => false;

        public MPath this[int index] 
        { 
            get 
            { 
                return (index > _childrenCount) ? throw new IndexOutOfRangeException() : _children[index]; 
            }

            set
            {
                if (index > _childrenCount)
                    throw new IndexOutOfRangeException();

                _children[index] = value;
            }
        }

        public MPath GetPath(object key)
        {
            if (_childrenCount < 1)
                return null;

            if (key is string)
                return _children.FirstOrDefault(i => i?.Name == (string)key);
            else if (key is int)
                return _children.FirstOrDefault(i => i?.Index == (int)key);
            else
                return null;
        }

        public IEnumerator<MPath> GetEnumerator()
        {
            if (_children == null)
                yield break;

            foreach (MPath p in _children)
                if (p != null)
                    yield return p;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public int IndexOf(MPath item) => Array.IndexOf(_children, item);
        public void Insert(int index, MPath item)
        {
            if (index > _childrenCount + 1)
                throw new IndexOutOfRangeException();

            if (_filled)
                ResizeArray();

            if (index  == _childrenCount + 1)
            {
                Add(item);
                return;
            }

            for (int i = _childrenCount - 1; i >= index; i--)
                _children[i + 1] = _children[i];

            _children[index] = item;
            _childrenCount++;
            CheckCount();
        }

        public void RemoveAt(int index)
        {
            if (index > _childrenCount)
                throw new IndexOutOfRangeException();

            for (int i = index; i < _childrenCount - 1; i++)
                _children[i] = _children[i + 1];

            _children[_childrenCount - 1] = null;
            if (_filled)
                _filled = false;

            _childrenCount--;
        }

        public void Add(MPath item)
        {
            if (_filled)
                ResizeArray();

            _children[_childrenCount] = item;
            _childrenCount++;
            CheckCount();
        }

        public void Clear()
        {
            _children = null;
            _childrenCount = 0;
            _filled = true;
        }

        public bool Contains(MPath item) => IndexOf(item) >= 0;

        public void CopyTo(MPath[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(MPath item)
        {
            int index = IndexOf(item);
            if (index == -1)
                return false;

            RemoveAt(index);
            return true;
        }

        private void ResizeArray()
        {
            if (_children == null)
                _children = new MPath[ARRAY_SIZE];
            else
                Array.Resize<MPath>(ref _children, _children.Length + ARRAY_SIZE);

            _filled = false;
        }

        private void CheckCount()
        {
            if (!_filled && _childrenCount % ARRAY_SIZE == 0)
                _filled = true;
        }
    }
}
