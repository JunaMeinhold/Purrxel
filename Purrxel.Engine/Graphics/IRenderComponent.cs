namespace Purrxel.Engine.Graphics
{
    using Purrxel.Engine.Scenes;

    public interface IRenderComponent : IComponent
    {
        public int QueueIndex { get; }

        public void Draw(GraphicsContext context, PassIdentifer pass, Camera camera, object? parameter);
    }
}