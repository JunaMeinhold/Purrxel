namespace VoxelEngine.Voxel.Serialization
{
    using Hexa.NET.Utilities;

    public unsafe struct ChunkPreSerialized
    {
        public Chunk* Chunk;
        public ChunkHeader Header;
        public ChunkCompression CompressionMinY;
        public UnsafeList<HeightMapRun> MinYRuns;
        public ChunkCompression CompressionMaxY;
        public UnsafeList<HeightMapRun> MaxYRuns;
        public ChunkCompression Compression;
        public UnsafeList<BlockRun> Runs;
        public long Length;

        public void Release()
        {
            MinYRuns.Release();
            MaxYRuns.Release();
            Runs.Release();
        }
    }
}