namespace App.Graphics.Passes
{
    using App.Graphics.Graph;
    using App.Pipelines.Deferred;
    using App.Pipelines.Effects;
    using Hexa.NET.DXGI;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Scenes;

    public class AOPass : RenderPass
    {
        private HBAOEffect hbao = null!;
        private ResourceRef<Texture2D> aoBuffer = null!;

        public override void Configure(GraphResourceBuilder creator)
        {
            hbao = new();
            aoBuffer = creator.CreateTexture2D("AOBuffer", new(Format.R32Float, (int)creator.Viewport.Width, (int)creator.Viewport.Height, 1, 1, GpuAccessFlags.RW));
        }

        public override void Init(GraphResourceBuilder creator)
        {
            D3D11GlobalResourceList.SetSRV("AOBuffer", aoBuffer.Value!);
        }

        public override void Execute(GraphicsContext context, Scene scene, Camera camera, GraphResourceBuilder creator)
        {
            var aoBuffer = this.aoBuffer.Value!;
            hbao.Update(context, camera, aoBuffer.Viewport);
            context.SetRenderTarget(aoBuffer);
            context.SetViewport(aoBuffer.Viewport);
            hbao.Pass(context);
        }

        protected override void DisposeCore()
        {
            D3D11GlobalResourceList.SetSRV("AOBuffer", null);
            hbao.Dispose();
        }
    }
}