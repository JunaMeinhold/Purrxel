namespace VoxelEngine.Graphics.D3D11
{
    public interface IDepthStencilView : IDeviceChild
    {
        public new nint NativePointer { get; }
    }
}