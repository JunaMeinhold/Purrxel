namespace Purrxel.Engine.Voxel.LOD
{
    using Purrxel.Engine.Voxel;
    using System.Runtime.CompilerServices;

    public unsafe struct HeightMapEnumerable
    {
        public Chunk* Chunk;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HeightMapEnumerable(Chunk* chunk)
        {
            Chunk = chunk;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HeightMapEnumerator GetEnumerator()
        {
            return new(Chunk);
        }
    }
}