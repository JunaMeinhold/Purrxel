namespace VoxelEngine.Voxel.Meshing
{
    using System.Runtime.CompilerServices;

    public unsafe struct ChunkVertexBuffer
    {
        private const int DefaultCapacity = 4096;
        private int count;
        private int capacity;
        private SemaphoreLight semaphore = new(1, 1);

        public VoxelVertex* Data;

        public ChunkVertexBuffer()
        {
        }

        public int Capacity
        {
            readonly get => capacity;
            set
            {
                if (Data == null)
                {
                    Data = AllocT<VoxelVertex>(value);
                    capacity = value;
                    return;
                }

                Data = ReAllocT(Data, value);
                capacity = value;
                count = capacity < count ? capacity : count;
            }
        }

        public readonly int Count => count;

        public void Lock()
        {
            semaphore.Wait();
        }

        public void ReleaseLock()
        {
            semaphore.Release();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow(int capacity)
        {
            int newcapacity = count == 0 ? DefaultCapacity : 2 * count;

            if (newcapacity < capacity)
            {
                newcapacity = capacity;
            }

            Capacity = newcapacity;
        }

        public void EnsureCapacity(int capacity)
        {
            if (this.capacity < capacity)
            {
                Grow(capacity);
            }
        }

        public void Reset(int length)
        {
            EnsureCapacity(length);
            count = 0;
        }

        public VoxelVertex* Increase(int count)
        {
            if (count <= 0) return null;

            int oldCount = this.count;
            int newSize = oldCount + count;

            if (newSize > capacity)
            {
                int newCapacity = Math.Max(capacity * 2, newSize);
                VoxelVertex* tmp = AllocT<VoxelVertex>(newCapacity);
                MemcpyT(Data, tmp, oldCount);
                Free(Data);
                Data = tmp;
                capacity = newCapacity;
            }

            VoxelVertex* ptr = Data + oldCount;
            this.count = newSize;

            return ptr;
        }

        public void Dispose()
        {
            Lock();
            if (Data != null)
            {
                Free(Data);
                Data = null;
                capacity = 0;
                count = 0;
            }
            ReleaseLock();
        }
    }
}