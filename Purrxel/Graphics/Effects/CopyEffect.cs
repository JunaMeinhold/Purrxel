namespace App.Graphics.Effects
{
    using Hexa.NET.D3D11;
    using System.Numerics;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using Viewport = Hexa.NET.Mathematics.Viewport;

    public enum CopyFilter
    {
        None = 0,
        Point,
        Bilinear,
        Trilinear,
        Anisotropic,
    }

    public class CopyEffect : IDisposable
    {
        private readonly GraphicsPipelineState pipeline;
        private readonly SamplerState samplerState;
        private readonly ConstantBuffer<Vector4> paramBuffer;
        private bool disposedValue;

        public CopyEffect(CopyFilter filter)
        {
            ShaderMacro[] macros = filter != CopyFilter.None ? [new("SAMPLED", 1)] : [];
            pipeline = GraphicsPipelineState.Create(new GraphicsPipelineDesc()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "effects/copy/ps.hlsl",
                Macros = macros
            }, GraphicsPipelineStateDesc.DefaultFullscreen);

            SamplerStateDescription description = SamplerStateDescription.PointClamp;

            switch (filter)
            {
                case CopyFilter.Point:
                    description = new(Filter.MinMagMipPoint, TextureAddressMode.Clamp);
                    break;

                case CopyFilter.Bilinear:
                    description = new(Filter.MinMagLinearMipPoint, TextureAddressMode.Clamp);
                    break;

                case CopyFilter.Trilinear:
                    description = new(Filter.MinMagMipLinear, TextureAddressMode.Clamp);
                    break;

                case CopyFilter.Anisotropic:
                    description = new(Filter.Anisotropic, TextureAddressMode.Clamp);
                    break;
            }

            samplerState = new(description);
            paramBuffer = new(CpuAccessFlags.Write);

            SetupState();
        }

        private void SetupState()
        {
            pipeline.Bindings.SetCBV("params", paramBuffer);
            pipeline.Bindings.SetSampler("samplerState", samplerState);
        }

        public void Copy(GraphicsContext context, IShaderResourceView source, IRenderTargetView destination, Vector2 srcSize)
        {
            Copy(context, source, destination, new Viewport(Vector2.Zero, srcSize));
        }

        public void Copy(GraphicsContext context, IShaderResourceView source, IRenderTargetView destination, Vector2 srcOffset, Vector2 srcSize)
        {
            Copy(context, source, destination, new Viewport(srcOffset, srcSize));
        }

        public void Copy(GraphicsContext context, IShaderResourceView source, IRenderTargetView destination, Viewport srcViewport)
        {
            Copy(context, source, destination, srcViewport, srcViewport);
        }

        public void Copy(GraphicsContext context, IShaderResourceView source, IRenderTargetView destination, Vector2 srcOffset, Vector2 srcSize, Vector2 dstOffset, Vector2 dstSize)
        {
            Copy(context, source, destination, new Viewport(srcOffset, srcSize), new Viewport(dstOffset, dstSize));
        }

        public void Copy(GraphicsContext context, IShaderResourceView source, IRenderTargetView destination, Viewport srcViewport, Viewport dstViewport)
        {
            Vector4 vector = new(srcViewport.X, srcViewport.Y, srcViewport.Width - srcViewport.X, srcViewport.Height - srcViewport.Y);
            paramBuffer.Update(context, vector);
            context.SetRenderTarget(destination, null);
            context.SetViewport(dstViewport);
            pipeline.Bindings.SetSRV("sourceTex", source);
            context.SetGraphicsPipelineState(pipeline);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipelineState(null);
            context.SetRenderTarget(null, null);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                pipeline.Dispose();
                samplerState.Dispose();
                paramBuffer.Dispose();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}