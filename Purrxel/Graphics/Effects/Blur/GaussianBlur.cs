namespace HexaEngine.Graphics.Effects.Blur
{
    using Hexa.NET.D3D11;
    using Hexa.NET.D3DCommon;
    using Hexa.NET.DXGI;
    using Hexa.NET.Mathematics;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using ShaderMacro = VoxelEngine.Graphics.D3D11.ShaderMacro;

    public enum GaussianRadius
    {
        Radius3x3 = 3,
        Radius5x5 = 5,
        Radius7x7 = 7,
    }

    public class GaussianBlur
    {
        private readonly GraphicsPipelineState horizontal;
        private readonly GraphicsPipelineState vertical;
        private readonly ConstantBuffer<GaussianBlurParams> paramsBuffer;
        private readonly SamplerState linearClampSampler;
        private readonly Texture2D intermediateTex;
        private bool disposedValue;

        private struct GaussianBlurParams
        {
            public Vector2 TextureDimentions;
            public Vector2 padd;
        }

        public GaussianBlur(Format format, int width, int height, GaussianRadius radius = GaussianRadius.Radius3x3, bool alphaBlend = false, bool additive = false, bool scissors = false, [CallerFilePath] string filename = "", [CallerLineNumber] int lineNumber = 0)
        {
            Format = format;
            Width = width;
            Height = height;

            RasterizerDescription rasterizerDescription = scissors ? RasterizerDescription.CullBackScissors : RasterizerDescription.CullBack;

            horizontal = GraphicsPipelineState.Create(new GraphicsPipelineDesc()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "effects/blur/horizontal.hlsl",
                Macros = [new ShaderMacro("GAUSSIAN_RADIUS", (int)radius)]
            }, new()
            {
                Rasterizer = rasterizerDescription,
                Topology = PrimitiveTopology.Trianglestrip
            });

            vertical = GraphicsPipelineState.Create(new GraphicsPipelineDesc()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "effects/blur/vertical.hlsl",
                Macros = [new ShaderMacro("GAUSSIAN_RADIUS", (int)radius)]
            }, new()
            {
                Rasterizer = rasterizerDescription,
                Blend = additive ? BlendDescription.Additive : alphaBlend ? BlendDescription.AlphaBlend : BlendDescription.Opaque,
                BlendFactor = Vector4.One,
                Topology = PrimitiveTopology.Trianglestrip
            });

            paramsBuffer = new(CpuAccessFlags.Write, filename + "_GAUSSIAN_BLUR_CONSTANT_BUFFER", lineNumber);
            linearClampSampler = new(SamplerStateDescription.LinearClamp);

            intermediateTex = new(format, width, height, 1, 1, gpuAccessFlags: GpuAccessFlags.RW);

            SetupState();
        }

        private void SetupState()
        {
            horizontal.Bindings.SetSampler("state", linearClampSampler);
            horizontal.Bindings.SetCBV("params", paramsBuffer);

            vertical.Bindings.SetSampler("state", linearClampSampler);
            vertical.Bindings.SetCBV("params", paramsBuffer);
        }

        public BlurType Type => BlurType.Gaussian;

        public Format Format { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public unsafe void Blur(GraphicsContext context, IShaderResourceView src, IRenderTargetView dst, float width, float height)
        {
            GaussianBlurParams gaussianBlurParams = default;
            gaussianBlurParams.TextureDimentions = new(width, height);
            paramsBuffer.Update(context, gaussianBlurParams);

            context.SetRenderTarget(intermediateTex, null);
            context.SetViewport(new(width, height));
            vertical.Bindings.SetSRV("tex", intermediateTex);
            horizontal.Bindings.SetSRV("tex", src);
            context.SetGraphicsPipelineState(horizontal);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipelineState(null);

            context.SetRenderTarget(dst, null);
            context.SetGraphicsPipelineState(vertical);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipelineState(null);
            context.SetRenderTarget(null, null);
        }

        public unsafe void Blur(GraphicsContext context, IShaderResourceView src, IRenderTargetView dst, float srcWidth, float srcHeight, float dstWidth, float dstHeight)
        {
            GaussianBlurParams gaussianBlurParams = default;
            gaussianBlurParams.TextureDimentions = new(srcWidth, srcHeight);
            paramsBuffer.Update(context, gaussianBlurParams);

            context.SetRenderTarget(intermediateTex, null);
            context.SetViewport(intermediateTex.Viewport);
            vertical.Bindings.SetSRV("tex", intermediateTex);
            horizontal.Bindings.SetSRV("tex", src);
            context.SetGraphicsPipelineState(horizontal);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipelineState(null);

            context.SetViewport(new(dstWidth, dstHeight));
            context.SetRenderTarget(dst, null);

            context.SetGraphicsPipelineState(vertical);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipelineState(null);

            context.SetRenderTarget(null, null);
        }

        public unsafe void Blur(GraphicsContext context, IShaderResourceView src, IRenderTargetView dst, float width, float height, Point4 scissors)
        {
            GaussianBlurParams gaussianBlurParams = default;
            gaussianBlurParams.TextureDimentions = new(width, height);
            paramsBuffer.Update(context, gaussianBlurParams);

            context.SetScissorRect(scissors.X, scissors.Y, scissors.Z, scissors.W);
            context.SetRenderTarget(intermediateTex, null);
            context.SetViewport(new(width, height));
            vertical.Bindings.SetSRV("tex", intermediateTex);
            horizontal.Bindings.SetSRV("tex", src);
            context.SetGraphicsPipelineState(horizontal);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipelineState(null);

            context.SetRenderTarget(dst, null);

            context.SetGraphicsPipelineState(vertical);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipelineState(null);

            context.SetRenderTarget(null, null);
            context.SetScissorRect(0, 0, 0, 0);
        }

        public void Resize(Format format, int width, int height)
        {
            Format = format;
            Width = width;
            Height = height;
            intermediateTex.Resize(format, width, height, 1, 1, 0, GpuAccessFlags.RW);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                vertical.Dispose();
                horizontal.Dispose();
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