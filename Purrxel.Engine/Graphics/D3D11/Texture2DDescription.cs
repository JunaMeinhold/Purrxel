namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using System;
    using System.Collections.Generic;

    public struct Texture2DDescription : IEquatable<Texture2DDescription>
    {
        public Format Format;
        public int Width;
        public int Height;
        public int ArraySize;
        public int MipLevels;
        public GpuAccessFlags GpuAccessFlags;
        public CpuAccessFlags CpuAccessFlags;
        public ResourceMiscFlag MiscFlags;
        public SampleDesc SampleDesc;

        public Texture2DDescription(Format format, int width, int height, int arraySize = 1, int mipLevels = 1, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.RW, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag miscFlags = 0, SampleDesc? sampleDesc = default)
        {
            Format = format;
            Width = width;
            Height = height;
            ArraySize = arraySize;
            MipLevels = mipLevels;
            GpuAccessFlags = gpuAccessFlags;
            CpuAccessFlags = cpuAccessFlags;
            MiscFlags = miscFlags;
            SampleDesc = sampleDesc ?? new SampleDesc(1, 0);
        }

        public Texture2DDescription(Format format, int width, int height, GpuAccessFlags gpuAccessFlags = GpuAccessFlags.RW, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None, ResourceMiscFlag miscFlags = 0, SampleDesc? sampleDesc = default)
        {
            Format = format;
            Width = width;
            Height = height;
            ArraySize = 1;
            MipLevels = 1;
            GpuAccessFlags = gpuAccessFlags;
            CpuAccessFlags = cpuAccessFlags;
            MiscFlags = miscFlags;
            SampleDesc = sampleDesc ?? new SampleDesc(1, 0);
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is Texture2DDescription description && Equals(description);
        }

        public readonly bool Equals(Texture2DDescription other)
        {
            return Format == other.Format &&
                   Width == other.Width &&
                   Height == other.Height &&
                   ArraySize == other.ArraySize &&
                   MipLevels == other.MipLevels &&
                   GpuAccessFlags == other.GpuAccessFlags &&
                   CpuAccessFlags == other.CpuAccessFlags &&
                   MiscFlags == other.MiscFlags &&
                   SampleDesc.Count == other.SampleDesc.Count &&
                   SampleDesc.Quality == other.SampleDesc.Quality;
        }

        public override readonly int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(Format);
            hash.Add(Width);
            hash.Add(Height);
            hash.Add(ArraySize);
            hash.Add(MipLevels);
            hash.Add(GpuAccessFlags);
            hash.Add(CpuAccessFlags);
            hash.Add(MiscFlags);
            hash.Add(SampleDesc.Count);
            hash.Add(SampleDesc.Quality);
            return hash.ToHashCode();
        }

        public static bool operator ==(Texture2DDescription left, Texture2DDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Texture2DDescription left, Texture2DDescription right)
        {
            return !(left == right);
        }
    }
}