namespace App.Renderers
{
    using Hexa.NET.Mathematics;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.D3D11;

    public interface IPostFx : IDisposable
    {
        public string Name { get; }

        public bool Enabled { get; set; }

        public PostFxFlags Flags { get; }

        public void SetInput(IShaderResourceView srv, Viewport viewport);

        public void SetOutput(IRenderTargetView rtv, Viewport viewport);

        public void Update(GraphicsContext context);

        public void PreDraw(GraphicsContext context);

        public void Draw(GraphicsContext context);
    }
}