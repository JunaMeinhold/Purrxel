namespace App.Graphics.Effects
{
    using Hexa.NET.Mathematics;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;

    public class ClearSliceEffect : DisposableBase
    {
        private readonly ComputePipelineState computePipelineState;
        private readonly ConstantBuffer<UPoint4> paramBuffer;

        public ClearSliceEffect()
        {
            computePipelineState = ComputePipelineState.Create(new ComputePipelineDesc()
            {
                Path = "effects/clear/cs.hlsl"
            });
            paramBuffer = new(CpuAccessFlags.Write);
            computePipelineState.Bindings.SetCBV("CBParams", paramBuffer);
        }

        public unsafe void Clear(GraphicsContext context, IUnorderedAccessView uav, uint width, uint height, uint slices, uint mask)
        {
            UPoint4 maskParams = default;
            maskParams.X = mask;
            paramBuffer.Update(context, maskParams);

            computePipelineState.Bindings.SetUAV("inputTex", uav);
            context.SetComputePipelineState(computePipelineState);
            context.Dispatch((uint)MathF.Ceiling(width / 32f), (uint)MathF.Ceiling(height / 32f), slices);
            context.SetComputePipelineState(null);
        }

        protected override void DisposeCore()
        {
            computePipelineState.Dispose();
            paramBuffer.Dispose();
        }
    }
}