namespace App.Renderers
{
    using Hexa.NET.Mathematics;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.D3D11;

    public abstract class PostFxBase : IPostFx
    {
        private bool disposedValue;

        public abstract string Name { get; }

        public bool Enabled { get; set; }

        public abstract PostFxFlags Flags { get; }

        public virtual void Update(GraphicsContext context)
        {
        }

        public virtual void Draw(GraphicsContext context)
        {
        }

        public virtual void PreDraw(GraphicsContext context)
        {
        }

        public virtual void SetInput(IShaderResourceView srv, Viewport viewport)
        {
        }

        public virtual void SetOutput(IRenderTargetView rtv, Viewport viewport)
        {
        }

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
}