namespace VoxelEngine.Voxel
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    public class BlockingQueue<T>
    {
        private readonly Queue<T> queue = [];
        private readonly Lock _lock = new();
        private volatile int count;

        public int Count => count;

        public bool IsEmpty => count == 0;

        public void Lock()
        {
            _lock.Enter();
        }

        public void ReleaseLock()
        {
            _lock.Exit();
        }

        public bool Contains(T item)
        {
            Lock();
            var result = queue.Contains(item);
            ReleaseLock();
            return result;
        }

        public void EnqueueUnsafe(T item)
        {
            queue.Enqueue(item);
            count++;
        }

        public void EnqueueRange(T[] values, int offset, int count)
        {
            if (count == 0) return;
            lock (_lock)
            {
                queue.EnsureCapacity(queue.Count + count);
                for (int i = offset; i < count; i++)
                {
                    queue.Enqueue(values[i]);
                }
                this.count += count;
            }
        }

        public void EnqueueRange(IList<T> values)
        {
            lock (_lock)
            {
                int count = values.Count;
                queue.EnsureCapacity(queue.Count + count);
                foreach (T item in values)
                {
                    queue.Enqueue(item);
                }
                this.count += count;
            }
        }

        public void Enqueue(T item)
        {
            lock (_lock)
            {
                queue.Enqueue(item);
                count++;
            }
        }

        public T Dequeue()
        {
            Lock();
            T item = queue.Dequeue();
            count--;
            ReleaseLock();
            return item;
        }

        public int TryDequeueRange(T[] values)
        {
            int batchIndex = 0;
            lock (_lock)
            {
                while (batchIndex < values.Length && queue.TryDequeue(out var result))
                {
                    values[batchIndex++] = result;
                    count--;
                }
            }
            return batchIndex;
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T result)
        {
            bool item;
            lock (_lock)
            {
                item = queue.TryDequeue(out result);
                if (item)
                {
                    count--;
                }
            }
            return item;
        }

        public bool TryDequeueUnsafe([MaybeNullWhen(false)] out T result)
        {
            bool item = queue.TryDequeue(out result);
            if (item)
            {
                count--;
            }

            return item;
        }
    }
}