namespace VoxelEngine.Voxel.Blocks
{
    public struct BlockDescription
    {
        public byte XP;
        public byte XN;
        public byte YP;
        public byte YN;
        public byte ZP;
        public byte ZN;

        public BlockDescription(byte xP, byte xN, byte yP, byte yN, byte zP, byte zN)
        {
            XP = xP;
            XN = xN;
            YP = yP;
            YN = yN;
            ZP = zP;
            ZN = zN;
        }

        public BlockDescription(byte texture)
        {
            YP = texture;
            YN = texture;
            XP = texture;
            XN = texture;
            ZP = texture;
            ZN = texture;
        }

        public static implicit operator BlockDescriptionPacked(BlockDescription description)
        {
            return new BlockDescriptionPacked(description);
        }
    }
}