namespace App.Pipelines.Effects
{
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.D3D11;

    public class FXAAEffect : DisposableBase
    {
        private readonly GraphicsPipelineState pso;
        private readonly SamplerState samplerState;

        public FXAAEffect()
        {
            pso = GraphicsPipelineState.Create(new()
            {
                VertexShader = "quad.hlsl",
                PixelShader = "fxaa/ps.hlsl",
            }, GraphicsPipelineStateDesc.DefaultFullscreen);
            samplerState = new(SamplerStateDescription.LinearClamp);
            pso.Bindings.SetSampler("g_samLinear", samplerState);
        }

        public IShaderResourceView Input { set => pso.Bindings.SetSRV("g_txProcessed", value); }

        public void Pass(GraphicsContext context)
        {
            context.SetGraphicsPipelineState(pso);
            context.DrawInstanced(4, 1, 0, 0);
            context.SetGraphicsPipelineState(null);
        }

        protected override void DisposeCore()
        {
            pso.Dispose();
            samplerState.Dispose();
        }
    }
}