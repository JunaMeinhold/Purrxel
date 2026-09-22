namespace VoxelEngine.Voxel.Blocks
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct BlockDescriptionPacked
    {
        public int packedX;
        public int packedY;
        public int packedZ;
        public int padd;

        public BlockDescriptionPacked(BlockDescription description)
        {
            packedX = description.XP << 8 | description.XN;
            packedY = description.YP << 8 | description.YN;
            packedZ = description.ZP << 8 | description.ZN;
            padd = 0;
        }

        public BlockDescriptionPacked(int packedX, int packedY, int packedZ)
        {
            this.packedX = packedX;
            this.packedY = packedY;
            this.packedZ = packedZ;
            padd = 0;
        }
    }
}