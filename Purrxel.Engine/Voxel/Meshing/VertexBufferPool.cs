namespace VoxelEngine.Voxel.Meshing
{
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;

    public unsafe class VertexBufferPool<T> where T : unmanaged
    {
        private readonly List<VertexBuffer<T>> buffers = [];
        private readonly List<VertexBuffer<T>> freeBuffers = [];
        private readonly SemaphoreSlim semaphore = new(1);

        public VertexBufferPool()
        {
        }

        public static VertexBufferPool<T> Shared { get; } = new();

        public const int MaxFreeBuffers = 64;
        public const int ResizeSmallerAt = 32;
        public const int MinCapacity = 4096 * 8;

        public VertexBuffer<T> Rent(int minCapacity)
        {
            minCapacity = Math.Max(minCapacity, MinCapacity);

            semaphore.Wait();
            try
            {
                for (int i = 0; i < freeBuffers.Count; i++)
                {
                    VertexBuffer<T> buffer = freeBuffers[i];
                    if (buffer.Count >= minCapacity)
                    {
                        freeBuffers.RemoveAt(i);
                        return buffer;
                    }
                }

                if (freeBuffers.Count > ResizeSmallerAt)
                {
                    VertexBuffer<T> buffer = freeBuffers[0];
                    freeBuffers.RemoveAt(0);
                    buffer.Resize(minCapacity);
                    return buffer;
                }

                VertexBuffer<T> newBuffer = new(CpuAccessFlags.Write, minCapacity);

                buffers.Add(newBuffer);

                return newBuffer;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void Return(VertexBuffer<T> buffer)
        {
            semaphore.Wait();
            try
            {
                freeBuffers.Add(buffer);
                if (freeBuffers.Count > MaxFreeBuffers)
                {
                    VertexBuffer<T> freeBuffer = freeBuffers[0];
                    freeBuffers.RemoveAt(0);
                    freeBuffer.Dispose();
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}