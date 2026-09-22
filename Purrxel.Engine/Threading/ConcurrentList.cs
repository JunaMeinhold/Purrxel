namespace VoxelEngine.Threading
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class BlockingList<T> : IList<T>, IReadOnlyList<T>
    {
        private readonly List<T> _list = new();

        public T this[int index]
        {
            get
            {
                lock (SyncRoot)
                {
                    return ((IList<T>)_list)[index];
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    ((IList<T>)_list)[index] = value;
                }
            }
        }

        public int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return ((ICollection<T>)_list).Count;
                }
            }
        }

        public bool IsReadOnly => ((ICollection<T>)_list).IsReadOnly;

        public bool IsFixedSize => ((IList)_list).IsFixedSize;

        public bool IsSynchronized => ((ICollection)_list).IsSynchronized;

        public readonly Lock SyncRoot = new();

        public void AddRange(IEnumerable<T> values)
        {
            lock (SyncRoot)
            {
                _list.AddRange(values);
            }
        }

        public void AddRange(T[] values, int offset, int count)
        {
            if (count == 0) return;
            lock (SyncRoot)
            {
                for (int i = offset; i < count; i++)
                {
                    _list.Add(values[i]);
                }
            }
        }

        public void Add(T item)
        {
            lock (SyncRoot)
            {
                ((ICollection<T>)_list).Add(item);
            }
        }

        public int Add(object value)
        {
            lock (SyncRoot)
            {
                return ((IList)_list).Add(value);
            }
        }

        public void Clear()
        {
            lock (SyncRoot)
            {
                ((ICollection<T>)_list).Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (SyncRoot)
            {
                return ((ICollection<T>)_list).Contains(item);
            }
        }

        public bool Contains(object value)
        {
            lock (SyncRoot)
            {
                return ((IList)_list).Contains(value);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (SyncRoot)
            {
                ((ICollection<T>)_list).CopyTo(array, arrayIndex);
            }
        }

        public void CopyTo(Array array, int index)
        {
            lock (SyncRoot)
            {
                ((ICollection)_list).CopyTo(array, index);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (SyncRoot)
            {
                return ((IEnumerable<T>)_list).GetEnumerator();
            }
        }

        public int IndexOf(T item)
        {
            lock (SyncRoot)
            {
                return ((IList<T>)_list).IndexOf(item);
            }
        }

        public int IndexOf(object value)
        {
            lock (SyncRoot)
            {
                return ((IList)_list).IndexOf(value);
            }
        }

        public void Insert(int index, T item)
        {
            lock (SyncRoot)
            {
                ((IList<T>)_list).Insert(index, item);
            }
        }

        public void Insert(int index, object value)
        {
            lock (SyncRoot)
            {
                ((IList)_list).Insert(index, value);
            }
        }

        public bool Remove(T item)
        {
            lock (SyncRoot)
            {
                return ((ICollection<T>)_list).Remove(item);
            }
        }

        public void Remove(object value)
        {
            lock (SyncRoot)
            {
                ((IList)_list).Remove(value);
            }
        }

        public void RemoveAt(int index)
        {
            lock (SyncRoot)
            {
                ((IList<T>)_list).RemoveAt(index);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (SyncRoot)
            {
                return ((IEnumerable)_list).GetEnumerator();
            }
        }

        public void Sort(IComparer<T> comparer)
        {
            lock (SyncRoot)
            {
                _list.Sort(comparer);
            }
        }
    }
}