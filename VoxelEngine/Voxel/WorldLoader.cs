namespace VoxelEngine.Voxel
{
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using VoxelEngine.Core;
    using VoxelEngine.Debugging;
    using VoxelEngine.Graphics;
    using VoxelEngine.Threading;
    using VoxelEngine.Voxel.Serialization;

    public unsafe class WorldLoader : IDisposable
    {
        private readonly VoxelRegionFileManager regionManager = new();
        private Point2[] indicesRenderCache;
        private HashSet<Point2> indicesSimulationCache;

        private readonly HashSet<Point2> loadedRegionIds = [];

        private readonly BlockingQueue<Point3> loadIOQueue = new();

        private readonly BlockingQueue<ChunkSegment> generationQueue = new();

        private readonly BlockingQueue<ChunkSegment> loadQueue = new();

        // Contains chunks that are marked as dirty from an block update.
        private readonly BlockingQueue<ChunkSegment> updateQueue = new();

        // Contains chunks that needed to upload data to the gpu.
        private readonly BlockingQueue<RenderRegion> uploadQueue = new();

        // Contains chunks that will be unloaded from the gpu and will be send to unloadQueue.
        private readonly BlockingQueue<ChunkSegment> unloadIOQueue = new();

        private readonly BlockingQueue<ChunkSegment> saveIOQueue = new();

        // Contains chunks that are marked as loaded internal to prevent loading chunks multiple times.
        private readonly BlockingDictionary<Point2, ChunkSegment> loadedInternal = [];

        private readonly Dictionary<Point2, RenderRegion> pointToRenderRegions = [];
        private readonly BlockingList<RenderRegion> renderRegions = [];

        private readonly Worker[] workers;
        private readonly Worker[] ioWorkers;
        private bool running = true;
        private int idle;
        private int ioIdle;

        private readonly SemaphoreSlim semaphore = new(1);

        public static readonly WorldLoaderProfiler Profiler = new();

        public class Worker
        {
            public int Id;
            public Thread Thread;
            public AutoResetEvent Handle;
        }

        public WorldLoader(World world, int threads = 8, int ioThreads = 2)
        {
            World = world;
            workers = new Worker[threads];
            idle = threads;
            ioIdle = ioThreads;
            for (int i = 0; i < threads; i++)
            {
                workers[i] = new Worker()
                {
                    Id = i,
                    Handle = new AutoResetEvent(false),
                    Thread = new Thread(LoadVoid)
                    {
                        Name = $"ChunkLoader Worker {i}"
                    },
                };

                workers[i].Thread.Start(i);
            }

            ioWorkers = new Worker[ioThreads];

            for (int i = 0; i < ioThreads; i++)
            {
                ioWorkers[i] = new Worker()
                {
                    Id = i,
                    Handle = new AutoResetEvent(false),
                    Thread = new Thread(IOVoid)
                    {
                        Name = $"ChunkLoader IO Worker {i}"
                    },
                };

                ioWorkers[i].Thread.Start(i);
            }

            RegenerateCache();
        }

        public World World { get; }

        public IReadOnlyDictionary<Point2, ChunkSegment> LoadedChunkSegments => loadedInternal;

        public BlockingList<RenderRegion> LoadedRenderRegions => renderRegions;

        public bool Idle => idle == 0;

        public bool IOIdle => ioIdle == 0;

        public int UpdateQueueCount => updateQueue.Count;

        public int UploadQueueCount => uploadQueue.Count;

        public int GenerationQueueCount => generationQueue.Count;

        public int LoadQueueCount => loadQueue.Count;

        public int UnloadIOQueueCount => unloadIOQueue.Count;

        public int LoadIOQueueCount => loadIOQueue.Count;

        public int SaveIOQueueCount => saveIOQueue.Count;

        public int RenderRegionCount => renderRegions.Count;

        public int ChunkSegmentCount => loadedInternal.Count;

        public bool DoNotSave { get; set; } = false;

        public int RenderDistance { get; private set; } = Config.Default.ChunkRenderDistance;

        public int RenderRegionSize { get; set; } = Config.Default.RenderRegionSize;

        private static IEnumerable<Point2> GetIndices(Point3 center, int radius)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    yield return new Point2(x, z) + new Point2(center.X, center.Z);
                }
            }
        }

        private void SignalWorkers()
        {
            for (int i = 0; i < workers.Length; i++)
            {
                workers[i].Handle.Set();
            }
        }

        private void SignalIOWorkers()
        {
            for (int i = 0; i < ioWorkers.Length; i++)
            {
                ioWorkers[i].Handle.Set();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegenerateCache()
        {
            indicesRenderCache = GetIndices(Point3.Zero, Config.Default.ChunkRenderDistance).ToArray();

            static int compare(Point2 a, Point2 b)
            {
                float da = Point2.Distance(Point2.Zero, a);
                float db = Point2.Distance(Point2.Zero, b);

                if (da < db)
                {
                    return -1;
                }
                else if (db < da)
                {
                    return 1;
                }

                return 0;
            }
            indicesSimulationCache = [.. GetIndices(Point3.Zero, Config.Default.ChunkSimulationDistance).Order(SpatialSorter.Default)];
            Array.Sort(indicesRenderCache, compare);
        }

        private static int RegionFloor(int value, int size)
        {
            return (value >= 0)
                ? (value / size) * size  // Floor for positives
                : ((value - size + 1) / size) * size;  // Ceiling for negatives
        }

        private RenderRegion FindRenderRegion(Point2 pos)
        {
            Point2 regionKey = new(RegionFloor(pos.X, RenderRegionSize), RegionFloor(pos.Y, RenderRegionSize));

            semaphore.Wait();
            try
            {
                if (pointToRenderRegions.TryGetValue(regionKey, out var region))
                {
                    return region;
                }

                region = new(regionKey, new(RenderRegionSize));
                pointToRenderRegions.Add(regionKey, region);
                renderRegions.Add(region);

                return region;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private RenderRegion FindRenderRegionUnsafe(Point2 pos)
        {
            Point2 regionKey = new(RegionFloor(pos.X, RenderRegionSize), RegionFloor(pos.Y, RenderRegionSize));

            if (pointToRenderRegions.TryGetValue(regionKey, out var region))
            {
                return region;
            }

            region = new(regionKey, new(RenderRegionSize));
            pointToRenderRegions.Add(regionKey, region);
            renderRegions.Add(region);

            return region;
        }

        private Point3 lastPos = new();

        public void Reset()
        {
            first = true;
        }

        private unsafe void DispatchInitial(Point3 pos)
        {
            lastPos = pos;

            Profiler.Begin("Dispatch.Total");
            Profiler.Begin("Dispatch.Load");
            loadedChunksIndices.Clear();

            for (int i = 0; i < indicesRenderCache.Length; i++)
            {
                Point2 vector = indicesRenderCache[i];
                Point2 vec = new(vector.X + pos.X, vector.Y + pos.Z);
                loadedChunksIndices.Add(vec);
                Point3 chunkVGlobal = new(vec.X, 0, vec.Y);

                if (!loadedRegionIds.Add(vec))
                {
                    continue;
                }

                if (!World.InWorldLimits(chunkVGlobal.X, 0, chunkVGlobal.Z))
                {
                    continue;
                }

                loadIOQueue.Enqueue(chunkVGlobal);
            }

            Profiler.End("Dispatch.Load");

            Profiler.Begin("Dispatch.Unload");

            // Unload chunks
            lock (loadedInternal.SyncRoot)
            {
                unloadIOQueue.Lock();
                foreach (var segment in loadedInternal)
                {
                    if (!loadedChunksIndices.Contains(segment.Key))
                    {
                        unloadIOQueue.EnqueueUnsafe(segment.Value);
                    }
                    loadedRegionIds.Remove(segment.Key);
                }
                unloadIOQueue.ReleaseLock();
            }

            Profiler.End("Dispatch.Unload");

            SignalIOWorkers();

            Profiler.End("Dispatch.Total");
        }

        public enum Direction
        {
            None, Left, Right
        }

        private bool first = true;

        /// <summary>
        /// Called by main thread.
        /// </summary>
        public void Dispatch(Point3 pos)
        {
            if (first)
            {
                first = false;
                DispatchInitial(pos);
                return;
            }
            Profiler.Begin("Dispatch.Total");
            Profiler.Begin("Dispatch.Load");
            loadedChunksIndices.Clear();
            unloadChunksIndices.Clear();

            Point3 delta = lastPos - pos;

            int dX = delta.X;
            int dY = delta.Z;
            int xAbs = Math.Abs(dX);
            int yAbs = Math.Abs(dY);
            loadIOQueue.Lock();
            if (dX != 0 || dY != 0)
            {
                int dirX = dX > 0 ? -1 : 1;
                int dirY = dY > 0 ? -1 : 1;

                for (int x = 0; x < xAbs; x++)
                {
                    int unloadX = lastPos.X - dirX * (RenderDistance + x);
                    for (int z = lastPos.Z - RenderDistance; z <= lastPos.Z + RenderDistance; z++)
                    {
                        Point2 chunkToUnload = new(unloadX, z);
                        unloadChunksIndices.Add(chunkToUnload);
                    }
                }

                for (int y = 0; y < yAbs; y++)
                {
                    int unloadZ = lastPos.Z - dirY * (RenderDistance + y);
                    for (int x = lastPos.X - RenderDistance; x <= lastPos.X + RenderDistance; x++)
                    {
                        Point2 chunkToUnload = new(x, unloadZ);
                        unloadChunksIndices.Add(chunkToUnload);
                    }
                }

                for (int x = 0; x < xAbs; x++)
                {
                    int loadX = pos.X + dirX * (RenderDistance - x);
                    for (int z = pos.Z - RenderDistance; z <= pos.Z + RenderDistance; z++)
                    {
                        Point2 chunkToLoad = new(loadX, z);
                        if (World.InWorldLimits(chunkToLoad.X, 0, chunkToLoad.Y) && loadedChunksIndices.Add(chunkToLoad))
                        {
                            Point3 chunkVGlobal = new(chunkToLoad.X, 0, chunkToLoad.Y);
                            loadIOQueue.EnqueueUnsafe(chunkVGlobal);
                            loadedRegionIds.Add(chunkToLoad);
                        }
                    }
                }

                for (int y = 0; y < yAbs; y++)
                {
                    int loadZ = pos.Z + dirY * (RenderDistance - y);
                    for (int x = pos.X - RenderDistance; x <= pos.X + RenderDistance; x++)
                    {
                        Point2 chunkToLoad = new(x, loadZ);
                        if (World.InWorldLimits(chunkToLoad.X, 0, chunkToLoad.Y) && loadedChunksIndices.Add(chunkToLoad))
                        {
                            Point3 chunkVGlobal = new(chunkToLoad.X, 0, chunkToLoad.Y);
                            loadIOQueue.EnqueueUnsafe(chunkVGlobal);
                            loadedRegionIds.Add(chunkToLoad);
                        }
                    }
                }
            }
            loadIOQueue.ReleaseLock();

            Profiler.End("Dispatch.Load");

            Profiler.Begin("Dispatch.Unload");

            // Unload chunks
            lock (loadedInternal.SyncRoot)
            {
                unloadIOQueue.Lock();

                foreach (var segment in loadedInternal)
                {
                    if (unloadChunksIndices.Contains(segment.Key))
                    {
                        unloadIOQueue.EnqueueUnsafe(segment.Value);
                    }
                    loadedRegionIds.Remove(segment.Key);
                }

                unloadIOQueue.ReleaseLock();
            }

            Profiler.End("Dispatch.Unload");

            SignalIOWorkers();

            Profiler.End("Dispatch.Total");

            lastPos = pos;
        }

        /// <summary>
        /// Called by main thread.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispatch(Chunk* chunk, bool save)
        {
            ChunkSegment segment = World.GetSegment(chunk->Position);
            if (updateQueue.Contains(segment))
            {
                return;
            }
            updateQueue.Enqueue(segment);
            if (save)
            {
                saveIOQueue.Enqueue(segment);
                SignalIOWorkers();
            }
            SignalWorkers();
        }

        /// <summary>
        /// Called by main thread.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Upload(GraphicsContext context)
        {
            if (uploadQueue.IsEmpty)
            {
                return;
            }

            Profiler.Begin("Upload.Total");

            Profiler.Begin("Upload.Load");
            long start = Stopwatch.GetTimestamp();
            uploadQueue.Lock();
            double timeBudgetMs = uploadQueue.Count > 100 ? 5.0 : 4.0;
            while (uploadQueue.TryDequeueUnsafe(out var region))
            {
                region.Update(context);
                long end = Stopwatch.GetTimestamp();
                double delta = ((end - start) / (double)Stopwatch.Frequency) * 1000;
                if (delta >= timeBudgetMs) break;
            }
            uploadQueue.ReleaseLock();
            Profiler.End("Upload.Load");

            Profiler.End("Upload.Total");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadIORegion(ChunkSegment segment)
        {
            if (loadedInternal.ContainsKey(segment.Position))
            {
                return;
            }

            if (segment.IsEmpty || !segment.InMemory)
            {
                var file = regionManager.AcquireRegionStream(segment.Position.MapToRegions(), "world", false);
                if (file != null)
                {
                    try
                    {
                        if (file.Exists(segment))
                        {
                            file.ReadSegment(World, &segment);
                            loadQueue.Enqueue(segment);
                        }
                        else
                        {
                            generationQueue.Enqueue(segment);
                        }
                    }
                    finally
                    {
                        file.Dispose(false);
                    }
                }
                else
                {
                    generationQueue.Enqueue(segment);
                }
            }
        }

        private void GenerateRegionBatch(int batchSize, ChunkSegment[] batch)
        {
            for (int i = 0; i < batchSize; i++)
            {
                ref var segment = ref batch[i];
                segment.Generate(World);
            }
            saveIOQueue.EnqueueRange(batch, 0, batchSize);
            loadQueue.EnqueueRange(batch, 0, batchSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadRegionBatch(int batchSize, ChunkSegment[] batch, RenderRegion[] renderRegionBatch)
        {
            int batchIndex = 0;
            for (int i = 0; i < batchSize; i++)
            {
                ref var segment = ref batch[i];
                if (!segment.InMemory)
                {
                    continue;
                }

                segment.Update(true);
                batch[batchIndex++] = segment;
            }
            loadedInternal.AddRange(batch, 0, batchIndex);

            semaphore.Wait();
            try
            {
                for (int i = 0; i < batchIndex; i++)
                {
                    var segment = batch[i];
                    RenderRegion renderRegion = FindRenderRegionUnsafe(segment.Position);

                    renderRegion.AddRegion(segment);
                    renderRegionBatch[i] = renderRegion;
                    renderRegion.SetDirty();
                }
            }
            finally
            {
                semaphore.Release();
            }

            uploadQueue.EnqueueRange(renderRegionBatch, 0, batchIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateRegionBatch(int batchSize, ChunkSegment[] batch, RenderRegion[] renderRegionBatch)
        {
            int batchIndex = 0;
            for (int i = 0; i < batchSize; i++)
            {
                ref var segment = ref batch[i];
                if (!segment.InMemory)
                {
                    continue;
                }

                segment.Update(true);
                RenderRegion renderRegion = FindRenderRegion(segment.Position);
                renderRegion.SetDirty();
                renderRegionBatch[batchIndex++] = renderRegion;
            }
            uploadQueue.EnqueueRange(renderRegionBatch, 0, batchIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UnloadIORegion(ChunkSegment segment)
        {
            if (segment.IsEmpty)
            {
                return;
            }

            uploadQueue.Lock();
            RenderRegion renderRegion = FindRenderRegion(segment.Position);
            renderRegion.RemoveRegion(segment);

            if (renderRegion.RegionCount == 0)
            {
                renderRegions.Remove(renderRegion);
                pointToRenderRegions.Remove(renderRegion.Offset);
                renderRegion.Release();
            }
            else
            {
                uploadQueue.EnqueueUnsafe(renderRegion);
            }

            segment.Unload();

            uploadQueue.ReleaseLock();

            loadedInternal.Remove(segment.Position);
        }

        private UnsafeHashSet<Point2> loadedChunksIndices = new(Config.Default.ChunkRenderDistance * 2 * Config.Default.ChunkRenderDistance * 2);
        private UnsafeHashSet<Point2> unloadChunksIndices = new(Config.Default.ChunkRenderDistance * 2 * Config.Default.ChunkRenderDistance * 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadVoid(object param)
        {
            int id = (int)param;
            Worker worker = workers[id];
            ChunkSegment[] segmentBatch = new ChunkSegment[16];
            RenderRegion[] renderRegionBatch = new RenderRegion[16];
            while (running)
            {
                {
                    int batchSize = updateQueue.TryDequeueRange(segmentBatch);
                    if (batchSize > 0)
                    {
                        UpdateRegionBatch(batchSize, segmentBatch, renderRegionBatch);
                    }
                }

                {
                    int batchSize = generationQueue.TryDequeueRange(segmentBatch);
                    if (batchSize > 0)
                    {
                        GenerateRegionBatch(batchSize, segmentBatch);
                        SignalIOWorkers();
                    }
                }

                {
                    int batchSize = loadQueue.TryDequeueRange(segmentBatch);
                    if (batchSize > 0)
                    {
                        LoadRegionBatch(batchSize, segmentBatch, renderRegionBatch);
                    }
                }

                if (running && updateQueue.IsEmpty && generationQueue.IsEmpty && loadQueue.IsEmpty)
                {
                    Interlocked.Decrement(ref idle);
                    worker.Handle.WaitOne();
                    Interlocked.Increment(ref idle);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IOVoid(object param)
        {
            int id = (int)param;
            Worker worker = ioWorkers[id];
            ChunkSegment[] segments = new ChunkSegment[16];
            while (running)
            {
                bool signal = false;

                {
                    signal |= !unloadIOQueue.IsEmpty;
                    if (unloadIOQueue.TryDequeue(out ChunkSegment segment) && running)
                    {
                        UnloadIORegion(segment);
                    }
                }

                {
                    if (saveIOQueue.TryDequeue(out ChunkSegment segment) && running)
                    {
                        if (!DoNotSave)
                        {
                            var file = regionManager.AcquireRegionStream(segment.Position.MapToRegions(), "world", true)!;
                            file.WriteSegment(&segment);
                            file.Dispose(true);
                        }
                    }
                }

                {
                    signal |= !loadIOQueue.IsEmpty;
                    if (loadIOQueue.TryDequeue(out Point3 chunkVGlobal) && running)
                    {
                        ChunkSegment segment = World.GetSegment(chunkVGlobal);
                        LoadIORegion(segment);
                        RegenerateNeighbours(segment);
                    }
                }

                if (signal)
                {
                    SignalWorkers();
                }

                if (running && unloadIOQueue.IsEmpty && saveIOQueue.IsEmpty && loadIOQueue.IsEmpty)
                {
                    Interlocked.Decrement(ref ioIdle);
                    worker.Handle.WaitOne();
                    Interlocked.Increment(ref ioIdle);
                }
            }
        }

        private void RegenerateNeighbours(ChunkSegment segment)
        {
            void DispatchNeighbours(Point2 point)
            {
                if (loadedInternal.TryGetValue(point, out var n) && n.MissingNeighbours)
                {
                    updateQueue.Enqueue(segment);
                }
            }

            DispatchNeighbours(segment.Position + new Point2(1, 0));
            DispatchNeighbours(segment.Position + new Point2(0, 1));
            DispatchNeighbours(segment.Position + new Point2(-1, 0));
            DispatchNeighbours(segment.Position + new Point2(0, -1));
        }

        public void Dispose()
        {
            running = false;

            for (int i = 0; i < workers.Length; i++)
            {
                workers[i].Handle.Set();
                workers[i].Thread.Join();
            }

            for (int i = 0; i < ioWorkers.Length; i++)
            {
                ioWorkers[i].Handle.Set();
                ioWorkers[i].Thread.Join();
            }

            Parallel.ForEach(loadedInternal, segment => { segment.Value.Unload(); });
            Parallel.ForEach(renderRegions, region => { region.Release(); });

            loadedInternal.Clear();
            renderRegions.Clear();
            GC.SuppressFinalize(this);
        }
    }
}