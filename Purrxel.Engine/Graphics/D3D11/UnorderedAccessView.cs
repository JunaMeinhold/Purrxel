namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System;
    using System.Collections.Generic;

    public unsafe struct UnorderedAccessView : IUnorderedAccessView, IEquatable<UnorderedAccessView>
    {
        public ComPtr<ID3D11UnorderedAccessView> UAV;

        public UnorderedAccessView(ComPtr<ID3D11UnorderedAccessView> rtv)
        {
            UAV = rtv;
        }

        public readonly nint NativePointer => (nint)UAV.Handle;

        public static implicit operator UnorderedAccessView(ComPtr<ID3D11UnorderedAccessView> uav) => new(uav);

        public static implicit operator ComPtr<ID3D11UnorderedAccessView>(UnorderedAccessView uav) => uav.UAV;

        public static bool operator ==(UnorderedAccessView left, UnorderedAccessView right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnorderedAccessView left, UnorderedAccessView right)
        {
            return !(left == right);
        }

        public void Dispose()
        {
            if (UAV.Handle != null)
            {
                UAV.Dispose();
                UAV = default;
            }
        }

        public void Release()
        {
            Dispose();
        }

        public override bool Equals(object? obj)
        {
            return obj is UnorderedAccessView view && Equals(view);
        }

        public bool Equals(UnorderedAccessView other)
        {
            return EqualityComparer<ComPtr<ID3D11UnorderedAccessView>>.Default.Equals(UAV, other.UAV) &&
                   NativePointer.Equals(other.NativePointer);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UAV, NativePointer);
        }
    }
}