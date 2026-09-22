namespace VoxelEngine.Voxel.Serialization
{
    using System.Buffers.Binary;
    using System.IO;

    public struct BlockRun : IBinarySerializable
    {
        public ushort Index;
        public ushort Count;
        public ushort Type;

        public void Read(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[6];
            stream.ReadExactly(buffer);
            Index = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            Count = BinaryPrimitives.ReadUInt16LittleEndian(buffer[2..]);
            Type = BinaryPrimitives.ReadUInt16LittleEndian(buffer[4..]);
        }

        public readonly void Write(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[6];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, Index);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..], Count);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[4..], Type);
            stream.Write(buffer);
        }

        public readonly int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, Index);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..], Count);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[4..], Type);
            return 6;
        }

        public int Read(ReadOnlySpan<byte> buffer)
        {
            Index = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            Count = BinaryPrimitives.ReadUInt16LittleEndian(buffer[2..]);
            Type = BinaryPrimitives.ReadUInt16LittleEndian(buffer[4..]);
            return 6;
        }
    }
}