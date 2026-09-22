namespace HexaEngine.Graphics.Effects.Blur
{
    using Hexa.NET.D3D11;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;

    public class BoxBlur
    {
        private readonly GraphicsPipelineState pso;
        private readonly ConstantBuffer<BoxBlurParams> paramsBuffer;
        private readonly SamplerState linearClampSampler;
        private bool disposedValue;
        private int size;

        private struct BoxBlurParams
        {
            public Vector2 TextureDimentions;
            public int Size;
            public float padd;
        }

        public BoxBlur([CallerFilePath] string filename = "", [CallerLineNumber] int lineNumber = 0)
        {
            pso = GraphicsPipelineState.Create(new GraphicsPipelineDesc()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "effects/blur/box.hlsl",
            }, GraphicsPipelineStateDesc.DefaultFullscreen);

            paramsBuffer = new(CpuAccessFlags.Write, filename + "-BoxBlur", lineNumber);
            linearClampSampler = new(SamplerStateDescription.LinearClamp);

            SetupState();
        }

        private void SetupState()
        {
            pso.Bindings.SetCBV("params", paramsBuffer);
            pso.Bindings.SetSampler("state", linearClampSampler);
        }

        public int Size { get => size; set => size = value; }

        public BlurType Type => BlurType.Box;

        public void Blur(GraphicsContext context, IShaderResourceView src, IRenderTargetView dst, float width, float height)
        {
            BoxBlurParams boxBlurParams = default;
            boxBlurParams.TextureDimentions = new(width, height);
            boxBlurParams.Size = size;
            paramsBuffer.Update(context, boxBlurParams);

            context.SetRenderTarget(dst, null);
            context.SetViewport(new(width, height));
            pso.Bindings.SetSRV("tex", src);
            context.SetGraphicsPipelineState(pso);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipelineState(null);
            context.SetRenderTarget(null, null);
        }

        public void Blur(GraphicsContext context, IShaderResourceView src, IRenderTargetView dst, float srcWidth, float srcHeight, float dstWidth, float dstHeight)
        {
            BoxBlurParams boxBlurParams = default;
            boxBlurParams.TextureDimentions = new(srcWidth, srcHeight);
            boxBlurParams.Size = size;
            paramsBuffer.Update(context, boxBlurParams);

            context.SetRenderTarget(dst, null);
            context.SetViewport(new(dstWidth, dstHeight));
            pso.Bindings.SetSRV("tex", src);
            context.SetGraphicsPipelineState(pso);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipelineState(null);
            context.SetRenderTarget(null, null);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                pso.Dispose();
                paramsBuffer.Dispose();
                linearClampSampler.Dispose();
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