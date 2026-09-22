namespace App.Graphics.Passes
{
    using App.Graphics.Graph;
    using App.Pipelines.Deferred;
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Scenes;

    public class DeferredGeometryPass : RenderPass
    {
        private ResourceRef<GBuffer> gbuffer = null!;
        private ResourceRef<DepthStencil> depthStencil = null!;

        public override void Configure(GraphResourceBuilder creator)
        {
            gbuffer = creator.CreateGBuffer("GBuffer", new((int)creator.Viewport.Width, (int)creator.Viewport.Height, [Format.R16G16B16A16Float, Format.R8G8B8A8Unorm, Format.R16G16B16A16Float, Format.R16G16B16A16Float]));
            depthStencil = creator.GetDepthStencilBuffer("DepthStencil");
        }

        public override void Init(GraphResourceBuilder creator)
        {
            var gbuffer = this.gbuffer.Value!;
            D3D11GlobalResourceList.SetSRV("GBufferA", gbuffer.SRVs[0]);
            D3D11GlobalResourceList.SetSRV("GBufferB", gbuffer.SRVs[1]);
            D3D11GlobalResourceList.SetSRV("GBufferC", gbuffer.SRVs[2]);
            D3D11GlobalResourceList.SetSRV("GBufferD", gbuffer.SRVs[3]);
        }

        public override void Execute(GraphicsContext context, Scene scene, Camera camera, GraphResourceBuilder creator)
        {
            var gbuffer = this.gbuffer.Value!;
            var depthStencil = this.depthStencil.Value!;

            gbuffer.Clear(context, default);
            depthStencil.Clear(context, ClearFlag.Depth | ClearFlag.Stencil, 1, 0);

            gbuffer.SetTarget(context, depthStencil);
            context.SetViewport(gbuffer.Viewport);

            scene.RenderSystem.Draw(context, RenderQueueIndex.Geometry, PassIdentifer.DeferredPass, camera);

            context.ClearState();
        }
    }
}