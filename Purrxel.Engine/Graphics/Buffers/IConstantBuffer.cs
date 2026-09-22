namespace VoxelEngine.Graphics.Buffers
{
    using VoxelEngine.Graphics.D3D11;

    public interface IConstantBuffer<T> : IConstantBuffer where T : unmanaged
    {
        void Resize(int length);

        void Update(GraphicsContext context, T value);

        void UpdateRange(GraphicsContext context, T[] value);

        unsafe void UpdateRange(GraphicsContext context, T* value, int length);
    }

    public interface IConstantBuffer : IBuffer, IDisposable
    {
        void Update(GraphicsContext context);
    }
}