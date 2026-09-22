namespace VoxelEngine.Voxel.LOD
{
    using Hexa.NET.Mathematics;

    public unsafe struct HeightMapEntry
    {
        public Chunk* Chunk;
        public byte Index;
        public byte X;
        public byte Z;
        public byte MinY;
        public byte MaxY;

        public readonly bool IsValid => MinY <= MaxY;

        public readonly Point2 AsPoint2() => new(X, Z);
    }
}