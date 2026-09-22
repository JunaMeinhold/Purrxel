namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.DXGI;
    using System;
    using System.Collections.Generic;

    public struct GBufferDescription : IEquatable<GBufferDescription>
    {
        public int Width;
        public int Height;
        public Format[] Formats;

        public GBufferDescription(int width, int height, Format[] formats)
        {
            Width = width;
            Height = height;
            Formats = formats;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is GBufferDescription description && Equals(description);
        }

        public readonly bool Equals(GBufferDescription other)
        {
            return Width == other.Width &&
                   Height == other.Height &&
                   EqualityComparer<Format[]>.Default.Equals(Formats, other.Formats);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Width, Height, Formats);
        }

        public static bool operator ==(GBufferDescription left, GBufferDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GBufferDescription left, GBufferDescription right)
        {
            return !(left == right);
        }
    }
}