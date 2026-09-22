namespace VoxelEngine.Voxel.Blocks
{
    using VoxelEngine.Voxel;

    public struct BlockEntry
    {
        public ushort Id;
        public string Name;
        public BlockTextureDescription Description;
        public bool AlphaTest;
        public bool Transparent;

        public BlockEntry(string name, BlockTextureDescription description, bool alphaTest = false, bool transparent = false)
        {
            Id = 0;
            Name = name;
            Description = description;
            AlphaTest = alphaTest;
            Transparent = transparent;
        }

        public static implicit operator Block(BlockEntry entry)
        {
            return new() { Type = entry.Id };
        }
    }
}