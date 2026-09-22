namespace VoxelEngine.Voxel.Serialization
{
    using System.Buffers.Binary;
    using System.Runtime.CompilerServices;

    public enum ChunkCompression : ushort
    {
        Raw,
        RLE,
    }

    public struct ChunkHeader
    {
        public ushort BlockCount;
        public long Length;

        public const int Size = 10;

        public const int RLEBreakevenPoint = 1365;

        public const int RLEHeightMapBreakevenPoint = 85;

        public readonly void Write(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[10];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, BlockCount);
            BinaryPrimitives.WriteInt64LittleEndian(buffer[2..], Length);
            stream.Write(buffer);
        }

        public static ChunkHeader ReadFrom(Stream stream)
        {
            Unsafe.SkipInit(out ChunkHeader header);
            header.Read(stream);
            return header;
        }

        public void Read(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[10];
            stream.ReadExactly(buffer);
            BlockCount = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            Length = BinaryPrimitives.ReadInt64LittleEndian(buffer[2..]);
        }
    }
}