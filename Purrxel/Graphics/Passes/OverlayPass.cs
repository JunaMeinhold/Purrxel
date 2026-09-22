namespace Purrxel.Graphics.Passes
{
    using Purrxel.Engine.Graphics;
    using Purrxel.Engine.Graphics.D3D11;
    using Purrxel.Engine.Scenes;
    using Purrxel.Graphics.Graph;

    public class OverlayPass : RenderPass
    {
        private ResourceRef<DepthStencil> depthStencil = null!;

        public override void Configure(GraphResourceBuilder creator)
        {
            depthStencil = creator.GetDepthStencilBuffer("DepthStencil");
        }

        public override void Execute(GraphicsContext context, Scene scene, Camera camera, GraphResourceBuilder creator)
        {
            context.SetRenderTarget(creator.Output, null);
            scene.RenderSystem.Draw(context, RenderQueueIndex.Overlay, PassIdentifer.ForwardPass, camera);
        }
    }
}