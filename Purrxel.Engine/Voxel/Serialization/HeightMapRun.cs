namespace VoxelEngine.Voxel.Serialization
{
    using System.IO;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HeightMapRun : IBinarySerializable
    {
        public byte Index;
        public byte Count;
        public byte Value;

        public void Read(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[3];
            stream.ReadExactly(buffer);
            Index = buffer[0];
            Count = buffer[1];
            Value = buffer[2];
        }

        public readonly void Write(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[3];
            buffer[0] = Index;
            buffer[1] = Count;
            buffer[2] = Value;
            stream.Write(buffer);
        }

        public int Read(ReadOnlySpan<byte> buffer)
        {
            Index = buffer[0];
            Count = buffer[1];
            Value = buffer[2];
            return 3;
        }

        public readonly int Write(Span<byte> buffer)
        {
            buffer[0] = Index;
            buffer[1] = Count;
            buffer[2] = Value;
            return 3;
        }
    }
}