namespace VoxelEngine.Voxel.Serialization
{
    public struct FreeListEntry
    {
        public long Start;
        public long End;

        public FreeListEntry(long start, long end)
        {
            Start = start;
            End = end;
        }

        public readonly long Length => End - Start;
    }
}