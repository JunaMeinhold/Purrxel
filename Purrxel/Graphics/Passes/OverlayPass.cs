namespace App.Graphics.Passes
{
    using App.Graphics.Graph;
    using App.Pipelines.Deferred;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Scenes;

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