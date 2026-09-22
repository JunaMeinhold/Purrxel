namespace Purrxel.Engine.Voxel
{
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities;
    using System.Runtime.CompilerServices;

    public unsafe class ChunkArray
    {
        private readonly Dictionary<Point3, Pointer<Chunk>> chunks = [];

        private readonly ManualResetEventSlim writeLock = new(true);
        private readonly ManualResetEventSlim readLock = new(true);

        private readonly SemaphoreSlim readSemaphore = new(maxReader);
        private readonly SemaphoreSlim writeSemaphore = new(maxWriter);
        private const int maxReader = 32;
        private const int maxWriter = 1;

        public ChunkArray()
        {
        }

        public Chunk* this[int x, int y, int z]
        {
            get => Get(new(x, y, z));
            set => Set(new(x, y, z), value);
        }

        public Chunk* this[Point3 pos]
        {
            get => Get(pos);
            set => Set(pos, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginRead()
        {
            writeLock.Wait();

            readLock.Reset();

            readSemaphore.Wait();
        }

        public struct LockGuard : IDisposable
        {
            private ChunkArray? chunkArray;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public LockGuard(ChunkArray chunkArray)
            {
                this.chunkArray = chunkArray;
                chunkArray.BeginRead();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                if (chunkArray == null) return;
                chunkArray.EndRead();
                chunkArray = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LockGuard ReadLock()
        {
            return new LockGuard(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndRead()
        {
            var value = readSemaphore.Release();
            if (value == maxReader - 1)
            {
                readLock.Set();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginWrite()
        {
            readLock.Wait();

            writeLock.Reset();

            writeSemaphore.Wait();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndWrite()
        {
            var value = writeSemaphore.Release();
            if (value == maxWriter - 1)
            {
                writeLock.Set();
            }
        }

        public int Count
        {
            get
            {
                BeginRead();
                try
                {
                    return chunks.Count;
                }
                finally
                {
                    EndRead();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Chunk* Get(Point3 pos)
        {
            BeginRead();
            try
            {
                return GetUnsafe(pos);
            }
            finally
            {
                EndRead();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Chunk* GetUnsafe(Point3 pos)
        {
            chunks.TryGetValue(pos, out Pointer<Chunk> chunk);
            return chunk;
        }

        public void Set(Point3 pos, Chunk* value)
        {
            BeginWrite();
            try
            {
                SetUnsafe(pos, value);
            }
            finally
            {
                EndWrite();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetUnsafe(Point3 pos, Chunk* value)
        {
            if (value == null)
            {
                chunks.Remove(pos);
            }
            else
            {
                chunks[pos] = value;
            }
        }

        public void Remove(Point3 pos)
        {
            BeginWrite();
            try
            {
                chunks.Remove(pos, out _);
            }
            finally
            {
                EndWrite();
            }
        }

        public void Remove(Chunk* value)
        {
            BeginWrite();
            try
            {
                chunks.Remove(value->Position);
            }
            finally
            {
                EndWrite();
            }
        }

        public void Clear()
        {
            BeginWrite();
            try
            {
                chunks.Clear();
            }
            finally
            {
                EndWrite();
            }
        }
    }
}