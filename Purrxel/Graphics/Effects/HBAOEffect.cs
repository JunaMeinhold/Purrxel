namespace App.Pipelines.Effects
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using Hexa.NET.Mathematics;
    using System.Numerics;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Scenes;
    using Viewport = Hexa.NET.Mathematics.Viewport;

    public class HBAOEffect : DisposableBase
    {
        private readonly GraphicsPipelineState pipeline;
        private readonly ConstantBuffer<HBAOParams> paramsBuffer;
        private readonly Texture2D noiseTex;

        private readonly SamplerState samplerState;

        private readonly float samplingRadius = 0.02f;
        private readonly uint numSamplingDirections = 8;
        private readonly float samplingStep = 0.004f;
        private readonly uint numSamplingSteps = 4;
        private readonly float power = 1;
        private bool isDirty = true;
        private const int NoiseSize = 4;
        private const int NoiseStride = 4;

        private struct HBAOParams
        {
            public float SamplingRadius;
            public float SamplingRadiusToScreen;
            public uint NumSamplingDirections;
            public float SamplingStep;

            public uint NumSamplingSteps;
            public float Power;
            public Vector2 NoiseScale;

            public HBAOParams()
            {
                SamplingRadius = 0.5f;
                SamplingRadiusToScreen = 0;
                NumSamplingDirections = 8;
                SamplingStep = 0.004f;
                NumSamplingSteps = 4;
                Power = 1;
            }
        }

        public HBAOEffect()
        {
            pipeline = GraphicsPipelineState.Create(new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "hbao/ps.hlsl",
            }, GraphicsPipelineStateDesc.DefaultFullscreen);
            paramsBuffer = new(CpuAccessFlags.Write);
            unsafe
            {
                float* pixelData = AllocT<float>(NoiseSize * NoiseSize * NoiseStride);

                SubresourceData initialData = default;
                initialData.PSysMem = pixelData;
                initialData.SysMemPitch = NoiseSize * NoiseStride;

                int idx = 0;
                for (int i = 0; i < NoiseSize * NoiseSize; i++)
                {
                    float rand = Random.Shared.NextSingle() * float.Pi * 2.0f;
                    pixelData[idx++] = MathF.Sin(rand);
                    pixelData[idx++] = MathF.Cos(rand);
                    pixelData[idx++] = Random.Shared.NextSingle();
                    pixelData[idx++] = 1.0f;
                }

                noiseTex = new(initialData, Format.R32G32B32A32Float, NoiseSize, NoiseSize, 1, 1, gpuAccessFlags: GpuAccessFlags.Read);

                Free(pixelData);
            }

            samplerState = new(SamplerStateDescription.LinearClamp);

            pipeline.Bindings.SetCBV("ConfigBuffer", paramsBuffer);
            pipeline.Bindings.SetSRV("noiseTex", noiseTex);
            pipeline.Bindings.SetSampler("samplerState", samplerState);
        }

        public IShaderResourceView Depth { set => pipeline.Bindings.SetSRV("depthTex", value); }

        public IShaderResourceView Normal { set => pipeline.Bindings.SetSRV("normalTex", value); }

        public void Update(GraphicsContext context, Camera camera, Viewport viewport)
        {

            if (isDirty)
            {
                HBAOParams hbaoParams = default;
                hbaoParams.SamplingRadius = samplingRadius;
                hbaoParams.SamplingRadiusToScreen = samplingRadius * 0.5f * viewport.Height / (MathF.Tan(MathUtil.ToRad(camera.Fov) * 0.5f) * 2.0f); ;
                hbaoParams.SamplingStep = samplingStep;
                hbaoParams.NumSamplingSteps = numSamplingSteps;
                hbaoParams.NumSamplingDirections = numSamplingDirections;
                hbaoParams.Power = power;
                hbaoParams.NoiseScale = new Vector2(viewport.Width, viewport.Height) / NoiseSize;

                paramsBuffer.Update(context, hbaoParams);
                isDirty = false;
            }
        }

        public void Pass(GraphicsContext context)
        {
            context.SetGraphicsPipelineState(pipeline);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipelineState(null);
        }

        protected override void DisposeCore()
        {
            pipeline.Dispose();
            paramsBuffer.Dispose();
            noiseTex.Dispose();
            samplerState.Dispose();
        }
    }
}