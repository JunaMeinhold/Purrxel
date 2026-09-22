namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.DXGI;
    using System;

    public struct DepthStencilBufferDescription : IEquatable<DepthStencilBufferDescription>
    {
        public Format Format;
        public int Width;
        public int Height;
        public int ArraySize;

        public DepthStencilBufferDescription(Format format, int width, int height, int arraySize)
        {
            Format = format;
            Width = width;
            Height = height;
            ArraySize = arraySize;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is DepthStencilBufferDescription description && Equals(description);
        }

        public readonly bool Equals(DepthStencilBufferDescription other)
        {
            return Format == other.Format &&
                   Width == other.Width &&
                   Height == other.Height &&
                   ArraySize == other.ArraySize;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Format, Width, Height, ArraySize);
        }

        public static bool operator ==(DepthStencilBufferDescription left, DepthStencilBufferDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DepthStencilBufferDescription left, DepthStencilBufferDescription right)
        {
            return !(left == right);
        }
    }
}