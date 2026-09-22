namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;

    public static unsafe class DeviceContextExtensions
    {
        public static Span<T> AsSpan<T>(this MappedSubresource mappedSubresource, int length) where T : unmanaged
        {
            return new Span<T>(mappedSubresource.PData, length);
        }

        public static void SetRenderTarget(this ComPtr<ID3D11DeviceContext> context, IRenderTargetView? rtv, IDepthStencilView? dsv = null)
        {
            ID3D11RenderTargetView* pRtv = (ID3D11RenderTargetView*)(rtv?.NativePointer ?? 0);
            ID3D11DepthStencilView* pDsv = (ID3D11DepthStencilView*)(dsv?.NativePointer ?? 0);
            context.OMSetRenderTargets(1, &pRtv, pDsv);
        }

        public static void RSSetViewport(this ComPtr<ID3D11DeviceContext> context, Viewport viewport)
        {
            context.RSSetViewports(1, &viewport);
        }

        public static void RSSetViewport(this ComPtr<ID3D11DeviceContext> context, Hexa.NET.Mathematics.Viewport viewport)
        {
            Viewport viewport1 = new(viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth);
            context.RSSetViewports(1, &viewport1);
        }

        public static void IAUnsetVertexBuffers(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11Buffer** buffer = stackalloc ID3D11Buffer*[(int)numViews];
            uint stride = 0;
            uint offset = 0;
            context.IASetVertexBuffers(startSlot, numViews, buffer, &stride, &offset);
        }

        public static void IAUnsetIndexBuffer(this ComPtr<ID3D11DeviceContext> context)
        {
            context.IASetIndexBuffer((ID3D11Buffer*)null, 0, 0);
        }

        public static void VSUnsetShaderResources(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11ShaderResourceView** ppSrv = stackalloc ID3D11ShaderResourceView*[(int)numViews];
            context.VSSetShaderResources(startSlot, numViews, ppSrv);
        }

        public static void HSUnsetShaderResources(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11ShaderResourceView** ppSrv = stackalloc ID3D11ShaderResourceView*[(int)numViews];
            context.HSSetShaderResources(startSlot, numViews, ppSrv);
        }

        public static void DSUnsetShaderResources(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11ShaderResourceView** ppSrv = stackalloc ID3D11ShaderResourceView*[(int)numViews];
            context.DSSetShaderResources(startSlot, numViews, ppSrv);
        }

        public static void GSUnsetShaderResources(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11ShaderResourceView** ppSrv = stackalloc ID3D11ShaderResourceView*[(int)numViews];
            context.GSSetShaderResources(startSlot, numViews, ppSrv);
        }

        public static void PSUnsetShaderResources(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11ShaderResourceView** ppSrv = stackalloc ID3D11ShaderResourceView*[(int)numViews];
            context.PSSetShaderResources(startSlot, numViews, ppSrv);
        }

        public static void CSUnsetShaderResources(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11ShaderResourceView** ppSrv = stackalloc ID3D11ShaderResourceView*[(int)numViews];
            context.CSSetShaderResources(startSlot, numViews, ppSrv);
        }

        public static void VSUnsetSamplers(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11SamplerState** ppSrv = stackalloc ID3D11SamplerState*[(int)numViews];
            context.VSSetSamplers(startSlot, numViews, ppSrv);
        }

        public static void HSUnsetSamplers(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11SamplerState** ppSrv = stackalloc ID3D11SamplerState*[(int)numViews];
            context.HSSetSamplers(startSlot, numViews, ppSrv);
        }

        public static void DSUnsetSamplers(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11SamplerState** ppSrv = stackalloc ID3D11SamplerState*[(int)numViews];
            context.DSSetSamplers(startSlot, numViews, ppSrv);
        }

        public static void GSUnsetSamplers(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11SamplerState** ppSrv = stackalloc ID3D11SamplerState*[(int)numViews];
            context.GSSetSamplers(startSlot, numViews, ppSrv);
        }

        public static void PSUnsetSamplers(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11SamplerState** ppSrv = stackalloc ID3D11SamplerState*[(int)numViews];
            context.PSSetSamplers(startSlot, numViews, ppSrv);
        }

        public static void CSUnsetSamplers(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11SamplerState** ppSrv = stackalloc ID3D11SamplerState*[(int)numViews];
            context.CSSetSamplers(startSlot, numViews, ppSrv);
        }

        public static void VSUnsetConstantBuffers(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11Buffer** ppSrv = stackalloc ID3D11Buffer*[(int)numViews];
            context.VSSetConstantBuffers(startSlot, numViews, ppSrv);
        }

        public static void HSUnsetConstantBuffers(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11Buffer** ppSrv = stackalloc ID3D11Buffer*[(int)numViews];
            context.HSSetConstantBuffers(startSlot, numViews, ppSrv);
        }

        public static void DSUnsetConstantBuffers(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11Buffer** ppSrv = stackalloc ID3D11Buffer*[(int)numViews];
            context.DSSetConstantBuffers(startSlot, numViews, ppSrv);
        }

        public static void GSUnsetConstantBuffers(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11Buffer** ppSrv = stackalloc ID3D11Buffer*[(int)numViews];
            context.GSSetConstantBuffers(startSlot, numViews, ppSrv);
        }

        public static void PSUnsetConstantBuffers(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11Buffer** ppSrv = stackalloc ID3D11Buffer*[(int)numViews];
            context.PSSetConstantBuffers(startSlot, numViews, ppSrv);
        }

        public static void CSUnsetConstantBuffers(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11Buffer** ppSrv = stackalloc ID3D11Buffer*[(int)numViews];
            context.CSSetConstantBuffers(startSlot, numViews, ppSrv);
        }

        public static void CSUnsetUnorderedAccessViews(this ComPtr<ID3D11DeviceContext> context, uint startSlot, uint numViews)
        {
            ID3D11UnorderedAccessView** ppSrv = stackalloc ID3D11UnorderedAccessView*[(int)numViews];
            context.CSSetUnorderedAccessViews(startSlot, numViews, ppSrv, null);
        }
    }
}