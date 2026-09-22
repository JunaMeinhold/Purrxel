namespace VoxelEngine.Voxel.Serialization
{
    using System.Buffers.Binary;
    using System.IO;

    public struct VoxelRegionSeekTableEntry
    {
        public long Position;
        public long Length;

        public const int Size = 16;

        public void Read(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[Size];
            stream.ReadExactly(buffer);
            Position = BinaryPrimitives.ReadInt64LittleEndian(buffer);
            Length = BinaryPrimitives.ReadInt64LittleEndian(buffer[8..]);
        }

        public readonly void Write(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[Size];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, Position);
            BinaryPrimitives.WriteInt64LittleEndian(buffer[8..], Length);
            stream.Write(buffer);
        }
    }
}