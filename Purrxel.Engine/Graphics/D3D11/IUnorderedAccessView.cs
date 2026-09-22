namespace VoxelEngine.Graphics.D3D11
{
    public interface IUnorderedAccessView : IDeviceChild
    {
        public new nint NativePointer { get; }
    }
}