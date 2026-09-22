namespace VoxelEngine.Graphics.D3D11
{
    public interface IDeviceChild : IDisposable
    {
        public nint NativePointer { get; }
    }
}