namespace VoxelEngine.Graphics.D3D11
{
    public interface IShaderResourceView : IDeviceChild
    {
        public new nint NativePointer { get; }
    }
}