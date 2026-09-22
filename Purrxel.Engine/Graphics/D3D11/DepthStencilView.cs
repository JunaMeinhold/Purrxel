namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;

    public unsafe struct DepthStencilView : IDepthStencilView, IEquatable<DepthStencilView>
    {
        public ComPtr<ID3D11DepthStencilView> DSV;

        public DepthStencilView(ComPtr<ID3D11DepthStencilView> srv)
        {
            DSV = srv;
        }

        public readonly nint NativePointer => (nint)DSV.Handle;

        public static implicit operator DepthStencilView(ComPtr<ID3D11DepthStencilView> srv) => new(srv);

        public static implicit operator ComPtr<ID3D11DepthStencilView>(DepthStencilView srv) => srv.DSV;

        public static bool operator ==(DepthStencilView left, DepthStencilView right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DepthStencilView left, DepthStencilView right)
        {
            return !(left == right);
        }

        public void Dispose()
        {
            if (DSV.Handle != null)
            {
                DSV.Dispose();
                DSV = default;
            }
        }

        public void Release()
        {
            Dispose();
        }

        public override bool Equals(object? obj)
        {
            return obj is DepthStencilView view && Equals(view);
        }

        public bool Equals(DepthStencilView other)
        {
            return EqualityComparer<ComPtr<ID3D11DepthStencilView>>.Default.Equals(DSV, other.DSV) &&
                   NativePointer.Equals(other.NativePointer);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DSV, NativePointer);
        }
    }
}