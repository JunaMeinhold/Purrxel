namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System;
    using System.Collections.Generic;

    public unsafe struct ShaderResourceView : IShaderResourceView, IEquatable<ShaderResourceView>
    {
        public ComPtr<ID3D11ShaderResourceView> SRV;

        public ShaderResourceView(ComPtr<ID3D11ShaderResourceView> srv)
        {
            SRV = srv;
        }

        public readonly nint NativePointer => (nint)SRV.Handle;

        public static implicit operator ShaderResourceView(ComPtr<ID3D11ShaderResourceView> srv) => new(srv);

        public static implicit operator ComPtr<ID3D11ShaderResourceView>(ShaderResourceView srv) => srv.SRV;

        public static bool operator ==(ShaderResourceView left, ShaderResourceView right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ShaderResourceView left, ShaderResourceView right)
        {
            return !(left == right);
        }

        public void Dispose()
        {
            if (SRV.Handle != null)
            {
                SRV.Dispose();
                SRV = default;
            }
        }

        public void Release()
        {
            Dispose();
        }

        public override bool Equals(object? obj)
        {
            return obj is ShaderResourceView view && Equals(view);
        }

        public bool Equals(ShaderResourceView other)
        {
            return EqualityComparer<ComPtr<ID3D11ShaderResourceView>>.Default.Equals(SRV, other.SRV) &&
                   NativePointer.Equals(other.NativePointer);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SRV, NativePointer);
        }
    }
}