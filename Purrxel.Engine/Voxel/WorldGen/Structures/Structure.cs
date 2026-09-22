namespace VoxelEngine.Voxel.WorldGen.Structures
{
    using VoxelEngine.Voxel.Metadata;

    public enum StructurePlacementStrategy
    {
        /// <summary>
        /// Just replaces any block with the structure.
        /// </summary>
        Replace,

        /// <summary>
        /// Casts an ray downwards and places the structure on top, replaces any intersecting block
        /// </summary>
        RaycastReplace,

        /// <summary>
        /// Casts an ray downwards and places the structure on top, checks for any intersecting block
        /// </summary>
        RaycastCheck,
    }

    public class Structure
    {
        public string Name { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Depth { get; set; }

        public StructurePlacementStrategy PlacementStrategy { get; set; }

        public float Frequency { get; set; }

        public List<Block> Blocks { get; } = new();

        public BlockMetadataCollection Metadata { get; } = new();
    }
}