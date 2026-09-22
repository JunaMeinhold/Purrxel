namespace VoxelEngine.Voxel.Serialization
{
    using VoxelEngine.IO;

    public readonly struct ChunkRegionHeader
    {
        public static readonly byte[] MagicNumber = [0x54, 0x72, 0x61, 0x6e, 0x73, 0x56, 0x6f, 0x78, 0x65, 0x6c, 0x52, 0x65, 0x67, 0x69, 0x6f, 0x6e, 0x0];
        public static readonly Version Version = new(1, 0, 0, 0);
        public static readonly Version MinVersion = new(1, 0, 0, 0);

        public static readonly int Size = 4 + MagicNumber.Length;

        public static void Write(Stream stream)
        {
            stream.Write(MagicNumber);
            stream.WriteUInt32(Version);
        }

        public static void Read(Stream stream)
        {
            if (!stream.ReadCompare(MagicNumber))
            {
                throw new FormatException("Invalid magic number");
            }

            Version version = stream.ReadUInt32();
            if (version > Version || version < MinVersion)
            {
                throw new NotSupportedException($"The version of the header is not supported {version} Max: {Version}, Min: {MinVersion}");
            }
        }
    }
}