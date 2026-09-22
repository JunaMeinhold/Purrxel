namespace Purrxel.Engine.Voxel.WorldGen
{
    using Hexa.NET.Mathematics;
    using Purrxel.Engine.Voxel;
    using VoxelEngine.Voxel;

    public interface IChunkGenerator : IDisposable
    {
        public void GenerateBatch(ref ChunkSegment.ChunkArray chunks, World world, Point3 position);
    }
}