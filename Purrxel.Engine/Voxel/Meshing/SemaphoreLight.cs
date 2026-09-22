namespace VoxelEngine.Voxel.Meshing
{
    using System.Runtime.CompilerServices;

    public struct SemaphoreLight
    {
        private volatile int count;
        private readonly int maxCount;

        public SemaphoreLight(int initialCount, int maxCount)
        {
            if (initialCount < 0 || maxCount <= 0 || initialCount > maxCount)
            {
                throw new ArgumentException("Invalid semaphore initialization values.");
            }

            count = initialCount;
            this.maxCount = maxCount;
        }

        public readonly int CurrentCount => count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait()
        {
            while (true)
            {
                int oldCount = count;
                if (count > 0 && Interlocked.CompareExchange(ref count, oldCount - 1, oldCount) == oldCount)
                {
                    return;
                }

                Thread.Yield();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            if (count >= maxCount)
            {
                throw new SemaphoreFullException("Semaphore already at maximum count.");
            }

            Interlocked.Increment(ref count);
        }
    }
}