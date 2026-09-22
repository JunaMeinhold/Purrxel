namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;

    public abstract class D3D11PipelineState : DisposableRefBase
    {
        internal abstract void SetState(ComPtr<ID3D11DeviceContext3> context);

        internal abstract void UnsetState(ComPtr<ID3D11DeviceContext3> context);
    }
}