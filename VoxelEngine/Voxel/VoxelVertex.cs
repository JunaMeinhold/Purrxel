namespace VoxelEngine.Voxel
{
    using Hexa.NET.Mathematics;

    public struct VoxelVertex
    {
        public uint ChunkPosition;
        public int Data;
        public uint Color;

        public VoxelVertex(int data, uint color = uint.MaxValue)
        {
            Data = data;
            Color = color;
        }

        /// <summary>
        /// Packs the chunk position within the render region.
        /// Packs chunk coordinates into a 32-bit value using 8 bits per axis.
        /// </summary>
        /// <remarks>
        /// Current usage: Chunks are 0-15 per axis (requires only 4 bits each).
        /// Format uses 8 bits per axis for future flexibility.
        ///
        /// Bit layout:
        ///   bits 0-7:   X coordinate (currently 0-15, supports up to 255)
        ///   bits 8-15:  Y coordinate (currently 0-15, supports up to 255)
        ///   bits 16-23: Z coordinate (currently 0-15, supports up to 255)
        ///   bits 24-31: Reserved (currently unused)
        /// </remarks>
        public static uint PackChunkPosition(in Point3 point)
        {
            byte x = (byte)(point.X & 0xFF);
            byte y = (byte)(point.Y & 0xFF);
            byte z = (byte)(point.Z & 0xFF);
            return (uint)(x | (y << 8) | (z << 16));
        }
    }
}