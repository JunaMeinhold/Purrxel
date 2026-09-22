namespace App.Pipelines.Effects
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;

    public class ComposeEffect : DisposableBase
    {
        private readonly GraphicsPipelineState pso;
        private readonly ConstantBuffer<ComposeParams> cbOptions;
        private bool isDirty = true;
        private float fogStart = 900;
        private float fogEnd = 1000;
        private Vector3 fogColor = Vector3.One;

        public ComposeEffect() : base()
        {
            pso = GraphicsPipelineState.Create(new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "compose/ps.hlsl",
            }, GraphicsPipelineStateDesc.DefaultFullscreen);

            cbOptions = new(CpuAccessFlags.Write);
            pso.Bindings.SetCBV("Params", cbOptions);
        }

        private struct ComposeParams
        {
            public float BloomStrength;
            public float FogStart;
            public float FogEnd;
            public Vector3 FogColor;
            public float LUTAmountChroma;
            public float LUTAmountLuma;

            public ComposeParams(float bloomStrength = 1, float fogStart = 0.2f, float fogEnd = 1, Vector3 fogColor = default, float lutAmountChroma = 1, float lutAmountLuma = 1)
            {
                BloomStrength = bloomStrength;
                FogStart = fogStart;
                FogEnd = fogEnd;
                FogColor = fogColor;
                LUTAmountChroma = lutAmountChroma;
                LUTAmountLuma = lutAmountLuma;
            }
        }

        public IShaderResourceView Input { set => pso.Bindings.SetSRV("hdrTexture", value); }

        public IShaderResourceView Bloom { set => pso.Bindings.SetSRV("bloomTexture", value); }

        public float FogStart
        {
            get => fogStart;
            set
            {
                fogStart = value;
                isDirty = true;
            }
        }

        public float FogEnd
        {
            get => fogEnd;
            set
            {
                fogEnd = value;
                isDirty = true;
            }
        }

        public Vector3 FogColor
        {
            get => fogColor;
            set
            {
                fogColor = value;
                isDirty = true;
            }
        }

        public void Pass(GraphicsContext context)
        {
            if (isDirty)
            {
                ComposeParams composeParams = new(0.04f, fogStart, fogEnd, fogColor);
                cbOptions.Update(context, composeParams);
                isDirty = false;
            }
            context.SetGraphicsPipelineState(pso);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipelineState(null);
        }

        protected override void DisposeCore()
        {
            pso.Dispose();
            cbOptions.Dispose();
        }
    }
}