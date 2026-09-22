namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;

    public interface IRenderTarget
    {
        void ClearAndSetTarget(ID3D11DeviceContext context);

        void ClearTarget(ID3D11DeviceContext context);

        void SetTarget(ID3D11DeviceContext context);
    }
}