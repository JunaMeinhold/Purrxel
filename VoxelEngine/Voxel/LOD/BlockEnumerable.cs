namespace VoxelEngine.Voxel.LOD
{
    using System.Runtime.CompilerServices;

    public unsafe struct BlockEnumerable
    {
        public Block* Begin;
        public Block* End;
        public ushort StartIndex;
        public byte MinY;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlockEnumerable(HeightMapEntry entry)
        {
            StartIndex = (ushort)Extensions.MapToIndex(entry.X, entry.MinY, entry.Z);
            Begin = entry.Chunk->Data + StartIndex;
            End = Begin + entry.MaxY - entry.MinY;
            MinY = entry.MinY;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BlockEnumerator GetEnumerator()
        {
            return new(Begin, End, StartIndex, MinY);
        }
    }
}