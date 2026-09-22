namespace Purrxel.Engine.Voxel
{
    using System;
    using System.Buffers.Binary;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public interface IBinarySerializable
    {
        public int Write(Span<byte> buffer);

        public int Read(ReadOnlySpan<byte> buffer);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Block : IEquatable<Block>, IBinarySerializable
    {
        public ushort Type;

        public Block(ushort type)
        {
            Type = type;
        }

        public static readonly Block Air = new(0);

        public readonly bool IsAir => Type == 0;

        public static implicit operator ushort(Block block) => block.Type;

        public static implicit operator Block(ushort type) => new(type);

        public readonly int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, Type);
            return 2;
        }

        public int Read(ReadOnlySpan<byte> buffer)
        {
            Type = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            return 2;
        }

        public override bool Equals(object? obj)
        {
            return obj is Block block && Equals(block);
        }

        public readonly bool Equals(Block other)
        {
            return Type == other.Type;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Block left, Block right)
        {
            return left.Type == right.Type;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Block left, Block right)
        {
            return !(left == right);
        }
    }
}