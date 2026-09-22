namespace VoxelEngine.Voxel.Metadata
{
    using System.Buffers.Binary;
    using VoxelEngine.IO;
    using VoxelEngine.Voxel.Serialization;

    public readonly struct BlockMetadataCollectionHeader
    {
        public static readonly Version Version = new(1, 0, 0, 0);
        public static readonly Version MinVersion = new(1, 0, 0, 0);

        public const int Size = 8;

        public static void Write(Stream stream, int metadataCount)
        {
            stream.WriteUInt32(Version);
            stream.WriteInt32(metadataCount);
        }

        public static void Read(Stream stream, out int metadataCount)
        {
            Version version = stream.ReadUInt32();
            if (version > Version || version < MinVersion)
            {
                throw new NotSupportedException($"The version of the header is not supported {version} Max: {Version}, Min: {MinVersion}");
            }

            stream.ReadInt32(out metadataCount);
        }

        public static bool TryRead(Stream stream, out int metadataCount)
        {
            Version version = stream.ReadUInt32();
            if (version > Version || version < MinVersion)
            {
                metadataCount = -1;
                return false;
            }

            stream.ReadInt32(out metadataCount);
            return true;
        }

        public static int Read(ReadOnlySpan<byte> data, out int metadataCount)
        {
            Version version = BinaryPrimitives.ReadUInt32LittleEndian(data);
            if (version > Version || version < MinVersion)
            {
                throw new NotSupportedException($"The version of the header is not supported {version} Max: {Version}, Min: {MinVersion}");
            }

            metadataCount = BinaryPrimitives.ReadInt32LittleEndian(data);

            return 8;
        }

        public static bool TryRead(ReadOnlySpan<byte> data, out int read, out int metadataCount)
        {
            Version version = BinaryPrimitives.ReadUInt32LittleEndian(data);
            if (version > Version || version < MinVersion)
            {
                metadataCount = -1;
                read = 4;
                return false;
            }

            metadataCount = BinaryPrimitives.ReadInt32LittleEndian(data);
            read = 8;
            return true;
        }
    }
}