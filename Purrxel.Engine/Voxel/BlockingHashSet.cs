namespace VoxelEngine.Voxel
{
    using System.Collections;
    using System.Collections.Generic;

    public class BlockingHashSet<T> : ISet<T>, IReadOnlySet<T>
    {
        private readonly HashSet<T> values = [];
        private readonly Lock _lock = new();

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return values.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        public Lock SyncRoot => _lock;

        public bool Add(T item)
        {
            lock (_lock)
            {
                return values.Add(item);
            }
        }

        public void AddRange(T[] items, int offset, int count)
        {
            lock (_lock)
            {
                for (int i = offset; i < count; i++)
                {
                    values.Add(items[i]);
                }
            }
        }

        void ICollection<T>.Add(T item)
        {
            lock (_lock)
            {
                values.Add(item);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                values.Clear();
            }
        }

        public bool Remove(T item)
        {
            lock (_lock)
            {
                return values.Remove(item);
            }
        }

        public bool Contains(T item)
        {
            lock (_lock)
            {
                return values.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_lock)
            {
                values.CopyTo(array, arrayIndex);
            }
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            lock (_lock)
            {
                values.ExceptWith(other);
            }
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            lock (_lock)
            {
                values.IntersectWith(other);
            }
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            lock (_lock)
            {
                return values.IsProperSubsetOf(other);
            }
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            lock (_lock)
            {
                return values.IsProperSupersetOf(other);
            }
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            lock (_lock)
            {
                return values.IsSubsetOf(other);
            }
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            lock (_lock)
            {
                return values.IsSupersetOf(other);
            }
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            lock (_lock)
            {
                return values.Overlaps(other);
            }
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            lock (_lock)
            {
                return values.SetEquals(other);
            }
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            lock (_lock)
            {
                values.SymmetricExceptWith(other);
            }
        }

        public void UnionWith(IEnumerable<T> other)
        {
            lock (_lock)
            {
                values.UnionWith(other);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new BlockingEnumerator<T>(values.GetEnumerator(), _lock);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new BlockingEnumerator<T>(values.GetEnumerator(), _lock);
        }
    }

    public readonly struct BlockingEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> enumerator;
        private readonly Lock _lock;

        public BlockingEnumerator(IEnumerator<T> enumerator, Lock _lock)
        {
            this.enumerator = enumerator;
            this._lock = _lock;
            _lock.Enter();
        }

        public readonly object? Current => enumerator.Current;

        readonly T IEnumerator<T>.Current => enumerator.Current;

        public readonly void Dispose()
        {
            _lock.Exit();
        }

        public readonly bool MoveNext()
        {
            return enumerator.MoveNext();
        }

        public readonly void Reset()
        {
            enumerator.Reset();
        }
    }
}