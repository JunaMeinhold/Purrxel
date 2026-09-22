namespace App.Graphics.Passes
{
    using App.Graphics.Graph;
    using App.Pipelines.Deferred;
    using App.Pipelines.Effects;
    using Hexa.NET.DXGI;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Scenes;

    public class PostProcessingPass : RenderPass
    {
        private ResourceRef<Texture2D> lightBuffer = null!;

        private Texture2D fxaaBuffer = null!;
        private ComposeEffect compose = null!;
        private FXAAEffect fxaa = null!;
        private GodRaysEffect godRays = null!;
        private Bloom bloom = null!;

        public override void Configure(GraphResourceBuilder creator)
        {
            lightBuffer = creator.GetTexture2D("LightBuffer");

            int rendererWidth = (int)creator.Viewport.Width;
            int rendererHeight = (int)creator.Viewport.Height;

            fxaaBuffer = new(Format.R16G16B16A16Float, rendererWidth, rendererHeight, 1, 1, 0, GpuAccessFlags.RW);

            bloom = new(rendererWidth, rendererHeight);

            compose = new();
            compose.Bloom = bloom.Output;

            fxaa = new();
            fxaa.Input = fxaaBuffer;

            godRays = new(rendererWidth, rendererHeight);
        }

        protected override void DisposeCore()
        {
            fxaaBuffer.Dispose();
        }

        public override void Init(GraphResourceBuilder creator)
        {
            compose.Input = lightBuffer.Value!;
        }

        public override void Execute(GraphicsContext context, Scene scene, Camera camera, GraphResourceBuilder creator)
        {
            var lightBuffer = this.lightBuffer.Value!;

            bloom.Update(context);
            bloom.Pass(context, lightBuffer);

            context.SetRenderTarget(lightBuffer);
            context.SetViewport(lightBuffer.Viewport);
            godRays.Pass(context);

            context.ClearRenderTargetView(fxaaBuffer, default);
            context.SetRenderTarget(fxaaBuffer);
            context.SetViewport(fxaaBuffer.Viewport);
            compose.Pass(context);

            context.ClearRenderTargetView(creator.Output!, default);
            context.SetRenderTarget(creator.Output, null);
            context.SetViewport(creator.OutputViewport);
            fxaa.Pass(context);
        }
    }
}