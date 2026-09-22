namespace VoxelEngine.Voxel.Serialization
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [InlineArray(VoxelRegion.CHUNK_REGION_SIZE_SQUARED)]
    public struct VoxelRegionSeekTable
    {
        private VoxelRegionSeekTableEntry _element0;

        public Span<VoxelRegionSeekTableEntry> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref _element0, VoxelRegion.CHUNK_REGION_SIZE_SQUARED);
        }
    }
}