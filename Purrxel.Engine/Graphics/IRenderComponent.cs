namespace VoxelEngine.Scenes
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Graphics;

    public interface IRenderComponent : IComponent
    {
        public int QueueIndex { get; }

        public void Draw(GraphicsContext context, PassIdentifer pass, Camera camera, object? parameter);
    }
}