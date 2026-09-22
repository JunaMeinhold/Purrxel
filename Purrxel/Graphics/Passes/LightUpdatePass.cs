namespace Purrxel.Graphics.Passes
{
    using Purrxel.Engine.Graphics;
    using Purrxel.Engine.Scenes;
    using Purrxel.Graphics.Graph;

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