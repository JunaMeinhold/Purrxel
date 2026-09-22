namespace VoxelEngine.Graphics.D3D11
{
    public interface IPipeline : IDisposableRef
    {
        event Action<IPipeline>? OnCompile;
    }
}