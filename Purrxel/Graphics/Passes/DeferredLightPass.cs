namespace App.Pipelines.Deferred
{
    using App.Graphics.Graph;
    using Hexa.NET.D3DCommon;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Scenes;

    public abstract class RenderPass : IDisposable
    {
        private bool disposedValue;

        public abstract void Configure(GraphResourceBuilder creator);

        public virtual void Init(GraphResourceBuilder creator)
        {
        }

        public abstract void Execute(GraphicsContext context, Scene scene, Camera camera, GraphResourceBuilder creator);

        protected virtual void DisposeCore()
        {
        }

        public void Dispose()
        {
            if (disposedValue) return;
            DisposeCore();
            disposedValue = true;
            GC.SuppressFinalize(this);
        }
    }

    public class DeferredLightPass : RenderPass
    {
        private GraphicsPipelineState deferred = null!;
        private ResourceRef<Texture2D> lightBuffer = null!;
        private ResourceRef<DepthStencil> depthStencil = null!;

        public override void Configure(GraphResourceBuilder creator)
        {
            deferred = GraphicsPipelineState.Create(new GraphicsPipelineDesc()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "deferred/light/ps.hlsl",
            }, new()
            {
                Blend = BlendDescription.Additive,
                DepthStencil = DepthStencilDescription.None,
                Topology = PrimitiveTopology.Trianglestrip
            });
            lightBuffer = creator.GetTexture2D("LightBuffer");
            depthStencil = creator.GetDepthStencilBuffer("DepthStencil");
        }

        protected override void DisposeCore()
        {
            deferred.Dispose();
        }

        public override void Execute(GraphicsContext context, Scene scene, Camera camera, GraphResourceBuilder creator)
        {
            var lightBuffer = this.lightBuffer.Value!;

            context.ClearRenderTargetView(lightBuffer, default);
            context.SetRenderTarget(lightBuffer, depthStencil.Value);
            context.SetViewport(lightBuffer.Viewport);

            scene.RenderSystem.Draw(context, RenderQueueIndex.Background, PassIdentifer.ForwardPass, camera);

            context.SetRenderTarget(lightBuffer);

            context.SetGraphicsPipelineState(deferred);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipelineState(null);
        }
    }
}