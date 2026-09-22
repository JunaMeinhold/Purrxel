namespace App.Graphics.Effects
{
    using System.Numerics;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;

    public enum ReprojectFlags
    {
        None,
        VSM
    }

    public class ReprojectEffect : DisposableBase
    {
        private readonly ComputePipelineState computePipelineState;
        private readonly ConstantBuffer<ReprojectParams> paramBuffer;

        private struct ReprojectParams
        {
            public Matrix4x4 PrevViewProjInv;
            public Matrix4x4 ViewProj;
            public Vector2 TexelSize;
            public uint VSM;
            public float Padd;
        }

        public ReprojectEffect()
        {
            computePipelineState = ComputePipelineState.Create(new ComputePipelineDesc()
            {
                Path = "effects/reproject/cs.hlsl"
            });
            paramBuffer = new(CpuAccessFlags.Write);
            computePipelineState.Bindings.SetCBV("CBParams", paramBuffer);
        }

        public unsafe void Reproject(GraphicsContext context, IUnorderedAccessView uav, uint width, uint height, Matrix4x4 prevViewProjInv, Matrix4x4 viewProj, ReprojectFlags flags)
        {
            ReprojectParams reprojectParams = default;
            reprojectParams.PrevViewProjInv = prevViewProjInv;
            reprojectParams.ViewProj = viewProj;
            reprojectParams.TexelSize = new(1f / width, 1f / height);
            reprojectParams.VSM = (flags & ReprojectFlags.VSM) != 0 ? 1u : 0u;

            paramBuffer.Update(context, reprojectParams);
            computePipelineState.Bindings.SetUAV("inputTex", uav);
            context.SetComputePipelineState(computePipelineState);
            context.Dispatch((uint)MathF.Ceiling(width / 32f), (uint)MathF.Ceiling(height / 32f), 1);
            context.SetComputePipelineState(null);
        }

        protected override void DisposeCore()
        {
            computePipelineState.Dispose();
            paramBuffer.Dispose();
        }
    }
}