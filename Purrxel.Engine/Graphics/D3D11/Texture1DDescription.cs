namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using System;

    public struct Texture1DDescription : IEquatable<Texture1DDescription>
    {
        public int Width;
        public int ArraySize;
        public int MipLevels;
        public Format Format;
        public GpuAccessFlags GpuAccessFlags;
        public CpuAccessFlags CpuAccessFlags;
        public ResourceMiscFlag MiscFlags;

        public Texture1DDescription(Format format, int width, int arraySize, int mipLevels, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.RW, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag miscFlags = 0)
        {
            Width = width;
            ArraySize = arraySize;
            MipLevels = mipLevels;
            Format = format;
            GpuAccessFlags = gpuAccessFlags;
            CpuAccessFlags = cpuAccessFlags;
            MiscFlags = miscFlags;
        }

        public Texture1DDescription(Format format, int width, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.RW, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag miscFlags = 0)
        {
            Width = width;
            ArraySize = 1;
            MipLevels = 1;
            Format = format;
            GpuAccessFlags = gpuAccessFlags;
            CpuAccessFlags = cpuAccessFlags;
            MiscFlags = miscFlags;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is Texture1DDescription description && Equals(description);
        }

        public readonly bool Equals(Texture1DDescription other)
        {
            return Width == other.Width &&
                   ArraySize == other.ArraySize &&
                   MipLevels == other.MipLevels &&
                   Format == other.Format &&
                   GpuAccessFlags == other.GpuAccessFlags &&
                   CpuAccessFlags == other.CpuAccessFlags &&
                   MiscFlags == other.MiscFlags;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Width, ArraySize, MipLevels, Format, GpuAccessFlags, CpuAccessFlags, MiscFlags);
        }

        public static bool operator ==(Texture1DDescription left, Texture1DDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Texture1DDescription left, Texture1DDescription right)
        {
            return !(left == right);
        }
    }
}