namespace VoxelEngine.Voxel.Serialization
{
    using Hexa.NET.Mathematics;
    using System.Runtime.CompilerServices;

    public class VoxelRegionFileManager
    {
        private const int maxReader = 32;
        private const int maxWriter = 1;

        private readonly ManualResetEventSlim writeLock = new(true);
        private readonly ManualResetEventSlim readLock = new(true);
        private readonly SemaphoreSlim readSemaphore = new(maxReader);
        private readonly SemaphoreSlim writeSemaphore = new(maxWriter);

        private readonly Dictionary<Point2, IVoxelRegionFileInternal> idToRegions = [];
        private readonly List<IVoxelRegionFileInternal> regions = [];

        public int MaxOpenFiles { get; private set; } = 32;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginRead()
        {
            writeLock.Wait();
            readLock.Reset();
            readSemaphore.Wait();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EndRead()
        {
            if (readSemaphore.Release() == maxReader - 1)
            {
                readLock.Set();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginWrite()
        {
            readLock.Wait();
            writeLock.Reset();
            writeSemaphore.Wait();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EndWrite()
        {
            if (writeSemaphore.Release() == maxWriter - 1)
            {
                writeLock.Set();
            }
        }

        public VoxelRegionFile? AcquireRegionStream(Point2 regionPos, string worldPath, bool write)
        {
            BeginRead();

            try
            {
                if (idToRegions.TryGetValue(regionPos, out IVoxelRegionFileInternal? region))
                {
                    region.Lock(write ? StreamMode.Write : StreamMode.Read);
                    region.LastAccess = DateTime.UtcNow;
                    return (VoxelRegionFile)region;
                }
            }
            finally
            {
                EndRead();
            }

            BeginWrite();
            try
            {
                if (!idToRegions.TryGetValue(regionPos, out var region))
                {
                    TryEvict(out region);

                    string filename = Path.Combine(worldPath, $"r.{regionPos.X}.{regionPos.Y}.vxr");

                    bool exists = File.Exists(filename);
                    if (!exists && !write)
                    {
                        return null;
                    }

                    FileStream stream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                    if (region is null)
                    {
                        region = new VoxelRegionFile();

                        regions.Add(region);
                    }

                    region.Lock(write ? StreamMode.Write : StreamMode.Read);
                    region.Reset(regionPos, stream, !exists, write ? StreamMode.Write : StreamMode.Read);

                    idToRegions[regionPos] = region;
                }
                else
                {
                    region.Lock(write ? StreamMode.Write : StreamMode.Read);
                }

                return (VoxelRegionFile)region;
            }
            finally
            {
                EndWrite();
            }
        }

        private void TryEvict(out IVoxelRegionFileInternal? old)
        {
            if (idToRegions.Count < MaxOpenFiles)
            {
                old = null;
                return;
            }

            IVoxelRegionFileInternal? leastUsed = null;

            for (int i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                if (region.CurrentLockCount == 1)
                {
                    if (leastUsed == null || region.LastAccess < leastUsed.LastAccess)
                    {
                        leastUsed = region;
                    }
                }
            }

            if (leastUsed != null)
            {
                leastUsed.Dispose(false);
                idToRegions.Remove(leastUsed.Id);
            }

            old = leastUsed;
        }
    }
}