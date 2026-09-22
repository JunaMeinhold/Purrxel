namespace VoxelEngine.Voxel.WorldGen.Biomes
{
    public class Biome
    {
        public string Name { get; set; }

        public int Octaves { get; set; }

        public float Persistence { get; set; }

        public float Amplitude { get; set; }

        public float Redistribution { get; set; }

        public int MaxHeight { get; set; }

        public int MinHeight { get; set; }

        public int WaterHeight { get; set; }

        public List<BlockDistribution> Blocks { get; } = new();
    }

    public enum DistributionMode
    {
        Less,
        Greater,
        Equals,
        Range,
        RandomRange,
        RandomPatch
    }

    public struct BlockDistribution
    {
        public string BlockName { get; set; }

        public int Height { get; set; }

        public int Min { get; set; }

        public int Max { get; set; }

        public int Priority { get; set; }

        public DistributionMode Mode { get; set; }
    }
}