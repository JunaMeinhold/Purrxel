namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System;
    using System.Collections.Generic;

    public unsafe struct RenderTargetView : IRenderTargetView, IEquatable<RenderTargetView>
    {
        public ComPtr<ID3D11RenderTargetView> RTV;

        public RenderTargetView(ComPtr<ID3D11RenderTargetView> rtv)
        {
            RTV = rtv;
        }

        public readonly nint NativePointer => (nint)RTV.Handle;

        public static implicit operator RenderTargetView(ComPtr<ID3D11RenderTargetView> rtv) => new(rtv);

        public static implicit operator ComPtr<ID3D11RenderTargetView>(RenderTargetView rtv) => rtv.RTV;

        public static bool operator ==(RenderTargetView left, RenderTargetView right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RenderTargetView left, RenderTargetView right)
        {
            return !(left == right);
        }

        public void Dispose()
        {
            if (RTV.Handle != null)
            {
                RTV.Dispose();
                RTV = default;
            }
        }

        public void Release()
        {
            Dispose();
        }

        public override bool Equals(object? obj)
        {
            return obj is RenderTargetView view && Equals(view);
        }

        public bool Equals(RenderTargetView other)
        {
            return EqualityComparer<ComPtr<ID3D11RenderTargetView>>.Default.Equals(RTV, other.RTV) &&
                   NativePointer.Equals(other.NativePointer);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RTV, NativePointer);
        }
    }
}