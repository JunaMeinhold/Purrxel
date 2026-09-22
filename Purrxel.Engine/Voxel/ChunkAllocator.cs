namespace VoxelEngine.Voxel
{
    using Hexa.NET.Utilities;

    public static unsafe class ChunkAllocator
    {
        private static UnsafeStack<Pointer<Chunk>> pool;
        private static readonly SemaphoreSlim semaphore = new(1);

        public static int FreeThreshold { get; set; } = 64;

        public static int AllocatedAmount { get; private set; }

        public static Chunk* New(World map, int x, int y, int z, bool generated = false)
        {
            semaphore.Wait();
            try
            {
                AllocatedAmount++;
                Chunk* result;
                if (pool.TryPop(out var chunk))
                {
                    result = chunk;
                    *result = new(map, x, y, z, generated);
                    return result;
                }

                result = AllocT<Chunk>();
                ZeroMemoryT(result);
                *result = new(map, x, y, z, generated);
                return result;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public static void Free(Chunk* chunk)
        {
            semaphore.Wait();
            try
            {
                AllocatedAmount--;
                chunk->Unload(chunk);
                if (pool.Size < FreeThreshold)
                {
                    pool.Push(chunk);
                }
                else
                {
                    Utils.Free(chunk);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        public static void Dispose()
        {
            while (pool.TryPop(out var chunk))
            {
                chunk.Data->Unload(chunk);
                Utils.Free(chunk);
            }
        }
    }
}