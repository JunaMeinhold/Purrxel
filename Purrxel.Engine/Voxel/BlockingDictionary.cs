namespace VoxelEngine.Voxel
{
    using Hexa.NET.Mathematics;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    public class BlockingDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> values = [];
        private readonly Lock _lock = new();

        public TValue this[TKey key]
        {
            get
            {
                lock (_lock)
                {
                    return values[key];
                }
            }
            set
            {
                lock (_lock)
                {
                    values[key] = value;
                }
            }
        }

        public ICollection<TKey> Keys => values.Keys;

        public ICollection<TValue> Values => values.Values;

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

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => ((IReadOnlyDictionary<TKey, TValue>)values).Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => ((IReadOnlyDictionary<TKey, TValue>)values).Values;

        public void Add(TKey key, TValue value)
        {
            lock (_lock)
            {
                values.Add(key, value);
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            lock (_lock)
            {
                ((ICollection<KeyValuePair<TKey, TValue>>)values).Add(item);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                values.Clear();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            lock (_lock)
            {
                return values.Contains(item);
            }
        }

        public bool ContainsKey(TKey key)
        {
            lock (_lock)
            {
                return values.ContainsKey(key);
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            lock (_lock)
            {
                ((ICollection<KeyValuePair<TKey, TValue>>)values).CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(TKey key)
        {
            lock (_lock)
            {
                return values.Remove(key);
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            lock (_lock)
            {
                return ((ICollection<KeyValuePair<TKey, TValue>>)values).Remove(item);
            }
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            lock (_lock)
            {
                return values.TryGetValue(key, out value);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new BlockingEnumerator<KeyValuePair<TKey, TValue>>(values.GetEnumerator(), _lock);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddUnsafe(TKey key, TValue value)
        {
            values.Add(key, value);
        }
    }

    public static class DictEx
    {
        public static void AddRange(this BlockingDictionary<Point2, ChunkSegment> values, ChunkSegment[] batch, int offset, int count)
        {
            lock (values.SyncRoot)
            {
                for (int i = offset; i < count; i++)
                {
                    values.AddUnsafe(batch[i].Position, batch[i]);
                }
            }
        }
    }
}