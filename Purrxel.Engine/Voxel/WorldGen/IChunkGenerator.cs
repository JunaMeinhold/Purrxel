namespace VoxelEngine.Voxel.WorldGen
{
    using Hexa.NET.Mathematics;
    using System.Numerics;
    using VoxelEngine.Voxel;

    public interface IChunkGenerator : IDisposable
    {
        public void GenerateBatch(ref ChunkSegment.ChunkArray chunks, World world, Point3 position);
    }
}