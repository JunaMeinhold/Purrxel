namespace Purrxel.Engine.Voxel.LOD
{
    using Purrxel.Engine.Voxel;
    using System.Runtime.CompilerServices;

    public static unsafe class ChunkIterators
    {
        extension(ref Chunk chunk)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public HeightMapEnumerable AsHeightMapEnumerable()
            {
                return new((Chunk*)Unsafe.AsPointer(ref chunk));
            }
        }

        extension(HeightMapEntry entry)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public BlockEnumerable AsBlockEnumerable()
            {
                return new(entry);
            }
        }
    }
}