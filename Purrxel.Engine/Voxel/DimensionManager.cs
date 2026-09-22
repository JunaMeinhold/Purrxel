namespace VoxelEngine.Voxel
{
    using System.Collections.Generic;

    public static class DimensionManager
    {
        private static readonly Dictionary<int, World> dimensions = [];
        private static readonly Lock _lock = new();

        public static World GetWorld(int id)
        {
            lock (_lock)
            {
                return dimensions[id];
            }
        }

        internal static void AddWorld(World world)
        {
            lock (_lock)
            {
                dimensions[world.DimId] = world;
            }
        }

        internal static void RemoveWorld(World world)
        {
            lock (_lock)
            {
                dimensions.Remove(world.DimId);
            }
        }
    }
}