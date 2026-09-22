namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using System;

    public struct Texture3DDescription : IEquatable<Texture3DDescription>
    {
        public Format Format;
        public int Width;
        public int Height;
        public int Depth;
        public int MipLevels;
        public GpuAccessFlags GpuAccessFlags;
        public CpuAccessFlags CpuAccessFlags;
        public ResourceMiscFlag MiscFlags;

        public Texture3DDescription(Format format, int width, int height, int depth, int mipLevels, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.RW, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag miscFlags = 0)
        {
            Format = format;
            Width = width;
            Height = height;
            Depth = depth;
            MipLevels = mipLevels;
            GpuAccessFlags = gpuAccessFlags;
            CpuAccessFlags = cpuAccessFlags;
            MiscFlags = miscFlags;
        }

        public Texture3DDescription(Format format, int width, int height, int depth, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.RW, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag miscFlags = 0)
        {
            Format = format;
            Width = width;
            Height = height;
            Depth = depth;
            MipLevels = 1;
            GpuAccessFlags = gpuAccessFlags;
            CpuAccessFlags = cpuAccessFlags;
            MiscFlags = miscFlags;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is Texture3DDescription description && Equals(description);
        }

        public readonly bool Equals(Texture3DDescription other)
        {
            return Format == other.Format &&
                   Width == other.Width &&
                   Height == other.Height &&
                   Depth == other.Depth &&
                   MipLevels == other.MipLevels &&
                   GpuAccessFlags == other.GpuAccessFlags &&
                   CpuAccessFlags == other.CpuAccessFlags &&
                   MiscFlags == other.MiscFlags;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Format, Width, Height, Depth, MipLevels, GpuAccessFlags, CpuAccessFlags, MiscFlags);
        }

        public static bool operator ==(Texture3DDescription left, Texture3DDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Texture3DDescription left, Texture3DDescription right)
        {
            return !(left == right);
        }
    }
}