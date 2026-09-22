namespace Purrxel.Engine.Voxel
{
    using Hexa.NET.Mathematics;
    using System.Runtime.CompilerServices;

    public static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MapToIndex(this Point2 vector)
        {
            return vector.X + (vector.Y << 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MapToIndex(this Point3 vector)
        {
            return (vector.Z << Chunk.CHUNK_SHIFT_Z) + (vector.X << Chunk.CHUNK_SHIFT_Y) + vector.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MapToIndex(int x, int y, int z)
        {
            return (z << Chunk.CHUNK_SHIFT_Z) + (x << Chunk.CHUNK_SHIFT_Y) + y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point2 MapToRegions(this Point2 chunkPos)
        {
            return new(chunkPos.X >> 5, chunkPos.Y >> 5);
        }
    }
}