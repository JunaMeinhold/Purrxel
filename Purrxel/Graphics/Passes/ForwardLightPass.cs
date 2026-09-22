namespace App.Pipelines.Deferred
{
    using App.Graphics.Graph;
    using Hexa.NET.DXGI;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Scenes;

    public class ForwardLightPass : RenderPass
    {
        private ResourceRef<Texture2D> lightBuffer = null!;
        private ResourceRef<DepthStencil> depthStencil = null!;

        public override void Configure(GraphResourceBuilder creator)
        {
            lightBuffer = creator.CreateTexture2D("LightBuffer", new(Format.R16G16B16A16Float, (int)creator.Viewport.Width, (int)creator.Viewport.Height, 1, 1, GpuAccessFlags.RW));
            depthStencil = creator.GetDepthStencilBuffer("DepthStencil");
        }

        public override void Execute(GraphicsContext context, Scene scene, Camera camera, GraphResourceBuilder creator)
        {
            var lightBuffer = this.lightBuffer.Value!;

            context.SetRenderTarget(lightBuffer, depthStencil.Value);
            context.SetViewport(lightBuffer.Viewport);

            scene.RenderSystem.Draw(context, RenderQueueIndex.GeometryLast, PassIdentifer.ForwardPass, camera);
            scene.RenderSystem.Draw(context, RenderQueueIndex.Transparent, PassIdentifer.ForwardPass, camera);
        }
    }
}