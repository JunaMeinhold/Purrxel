namespace VoxelEngine.Voxel.Serialization
{
    using Hexa.NET.Mathematics;
    using HexaGen.Runtime.COM;
    using K4os.Compression.LZ4.Streams;
    using System.IO;

    internal interface IVoxelRegionFileInternal
    {
        DateTime LastAccess { get; set; }

        int CurrentLockCount { get; }

        Point2 Id { get; }

        internal void Dispose(bool full);

        internal void Reset(Point2 id, Stream stream, bool newFile, StreamMode mode);

        internal void Lock(StreamMode mode);
    }

    public class VoxelRegionFile : IVoxelRegionFileInternal
    {
        private readonly SemaphoreSlim semaphore = new(1);
        private Point2 id;
        private DateTime lastAccess;
        private VoxelRegion chunkRegion = new();
        private Stream Stream;
        private UnsafeLZ4Stream LZ4Stream;

        public VoxelRegionFile()
        {
            Stream = null!;
            LZ4Stream = null!;
        }

        public Point2 Id => id;

        public int CurrentLockCount => semaphore.CurrentCount;

        DateTime IVoxelRegionFileInternal.LastAccess { get => lastAccess; set => lastAccess = value; }

        void IVoxelRegionFileInternal.Dispose(bool full)
        {
            chunkRegion.Release();
            Stream?.Dispose();
            Stream = null!;
            if (full)
            {
                LZ4Stream?.Dispose();
                LZ4Stream = null!;
                semaphore.Dispose();
            }
        }

        void IVoxelRegionFileInternal.Reset(Point2 id, Stream stream, bool newFile, StreamMode mode)
        {
            this.id = id;
            Stream = stream;
            LZ4Stream ??= new(stream, 8192, mode, K4os.Compression.LZ4.LZ4Level.L10_OPT);
            LZ4Stream.Reset(stream, mode);
            lastAccess = DateTime.UtcNow;
            chunkRegion.Release();
            if (newFile)
            {
                stream.Position = 0;
                chunkRegion = new();
                chunkRegion.Serialize(stream);
            }
            else
            {
                stream.Position = 0;
                chunkRegion = new();
                chunkRegion.Deserialize(stream);
            }
        }

        void IVoxelRegionFileInternal.Lock(StreamMode mode)
        {
            semaphore.Wait();
            LZ4Stream?.Reset(Stream, mode);
        }

        public void Dispose(bool write)
        {
            if (write)
            {
                chunkRegion.Flush(Stream);
            }

            semaphore.Release();
        }

        public unsafe void WriteSegment(ChunkSegment* segment)
        {
            Point2 pointInRegion = new(segment->Position.X & 31, segment->Position.Y & 31);
            chunkRegion.WriteSegment(Stream, LZ4Stream, segment, pointInRegion);
        }

        public unsafe void ReadSegment(World world, ChunkSegment* segment)
        {
            Point2 pointInRegion = new(segment->Position.X & 31, segment->Position.Y & 31);
            chunkRegion.ReadSegment(Stream, LZ4Stream, world, segment, pointInRegion);
        }

        public bool Exists(ChunkSegment segment)
        {
            Point2 pointInRegion = new(segment.Position.X & 31, segment.Position.Y & 31);
            return chunkRegion.Exists(pointInRegion);
        }
    }
}