namespace VoxelEngine.Graphics.D3D11
{
    public interface IRenderTargetView : IDeviceChild
    {
        public new nint NativePointer { get; }
    }
}