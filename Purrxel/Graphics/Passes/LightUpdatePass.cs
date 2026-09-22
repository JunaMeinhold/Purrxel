namespace App.Graphics.Passes
{
    using App.Graphics.Graph;
    using App.Pipelines.Deferred;
    using VoxelEngine.Graphics;
    using VoxelEngine.Scenes;

    public class LightUpdatePass : RenderPass
    {
        public override void Configure(GraphResourceBuilder creator)
        {
        }

        public override void Execute(GraphicsContext context, Scene scene, Camera camera, GraphResourceBuilder creator)
        {
            scene.LightSystem.Update(context);
        }
    }
}