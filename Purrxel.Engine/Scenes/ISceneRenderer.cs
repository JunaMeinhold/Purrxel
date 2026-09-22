namespace VoxelEngine.Scenes
{
    using VoxelEngine.Graphics;
    using VoxelEngine.Windows;

    /// <summary>
    /// The interface for render callbacks.
    /// </summary>
    public interface ISceneRenderer
    {
        void Initialize(GameWindow window);

        void Render(GraphicsContext context, Camera camera, Scene scene);

        void Resize(GameWindow window);

        void Dispose();
    }
}