namespace VoxelEngine.Graphics
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Graphics.D3D11;
    using Viewport = Hexa.NET.Mathematics.Viewport;

    public unsafe class GraphicsContext
    {
        private ComPtr<ID3D11DeviceContext3> context;
        private D3D11PipelineState? lastState;

        public ComPtr<ID3D11DeviceContext3> NativeContext => context;

        public GraphicsContext(ComPtr<ID3D11DeviceContext3> context)
        {
            this.context = context;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateSubresource(IResource buffer, uint dstSubresource, Box* box, void* srcData, uint srcRowPitch, uint srcDepthPitch)
        {
            context.UpdateSubresource((ID3D11Resource*)buffer.NativePointer, dstSubresource, box, srcData, srcRowPitch, srcDepthPitch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearDepthStencilView(IDepthStencilView depthStencilView, ClearFlag flags, float depth, byte stencil)
        {
            context.ClearDepthStencilView((ID3D11DepthStencilView*)depthStencilView.NativePointer, (uint)flags, depth, stencil);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearRenderTargetView(IRenderTargetView renderTargetView, Vector4 value)
        {
            context.ClearRenderTargetView((ID3D11RenderTargetView*)renderTargetView.NativePointer, (float*)&value);
        }

        public void SetRenderTarget(IRenderTargetView? rtv, IDepthStencilView? dsv = null)
        {
            ID3D11RenderTargetView* pRtv = (ID3D11RenderTargetView*)(rtv?.NativePointer ?? 0);
            ID3D11DepthStencilView* pDsv = (ID3D11DepthStencilView*)(dsv?.NativePointer ?? 0);
            context.OMSetRenderTargets(1, &pRtv, pDsv);
        }

        public void SetRenderTargets<T>(Span<T> rtvs, IDepthStencilView? dsv = null) where T : IRenderTargetView
        {
            ID3D11RenderTargetView** ppRtv = stackalloc ID3D11RenderTargetView*[Hexa.NET.D3D11.D3D11.D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT];
            ID3D11DepthStencilView* pDsv = (ID3D11DepthStencilView*)(dsv?.NativePointer ?? 0);

            for (int i = 0; i < rtvs.Length; i++)
            {
                ppRtv[i] = (ID3D11RenderTargetView*)rtvs[i].NativePointer;
            }

            context.OMSetRenderTargets((uint)rtvs.Length, ppRtv, pDsv);
        }

        public void SetPipelineState(D3D11PipelineState? state)
        {
            if (lastState == state) return;
            lastState?.UnsetState(context);
            state?.SetState(context);
            lastState = state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispatch(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
        {
            context.Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
        }

        public void DispatchIndirect(IBuffer dispatchArgs, uint offset)
        {
            context.DispatchIndirect((ID3D11Buffer*)dispatchArgs.NativePointer, offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawIndexedInstanced(uint indexCount, uint instanceCount, uint indexOffset, int vertexOffset, uint instanceOffset)
        {
            context.DrawIndexedInstanced(indexCount, instanceCount, indexOffset, vertexOffset, instanceOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawIndexedInstancedIndirect(IBuffer bufferForArgs, uint alignedByteOffsetForArgs)
        {
            context.DrawIndexedInstancedIndirect((ID3D11Buffer*)bufferForArgs.NativePointer, alignedByteOffsetForArgs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawIndexedInstancedIndirect(void* bufferForArgs, uint alignedByteOffsetForArgs)
        {
            context.DrawIndexedInstancedIndirect((ID3D11Buffer*)bufferForArgs, alignedByteOffsetForArgs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawInstanced(uint vertexCount, uint instanceCount, uint vertexOffset, uint instanceOffset)
        {
            context.DrawInstanced(vertexCount, instanceCount, vertexOffset, instanceOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawInstancedIndirect(IBuffer bufferForArgs, uint alignedByteOffsetForArgs)
        {
            context.DrawInstancedIndirect((ID3D11Buffer*)bufferForArgs.NativePointer, alignedByteOffsetForArgs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawInstancedIndirect(void* bufferForArgs, uint alignedByteOffsetForArgs)
        {
            context.DrawInstancedIndirect((ID3D11Buffer*)bufferForArgs, alignedByteOffsetForArgs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetIndexBuffer(IBuffer? indexBuffer, Format format, int offset)
        {
#nullable disable
            ID3D11Buffer* buffer = (ID3D11Buffer*)indexBuffer?.NativePointer;
#nullable enable
            context.IASetIndexBuffer(buffer, format, (uint)offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MappedSubresource Map(IResource resource, int subresourceIndex, Map mode, MapFlag flags)
        {
            MappedSubresource data;
            context.Map((ID3D11Resource*)resource.NativePointer, (uint)subresourceIndex, mode, (uint)flags, &data);
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unmap(IResource resource, int subresourceIndex)
        {
            context.Unmap((ID3D11Resource*)resource.NativePointer, (uint)subresourceIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetScissorRect(int x, int y, int z, int w)
        {
            Rect32 rect = new(x, y, z, w);
            context.RSSetScissorRects(1, &rect);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertexBuffer(IBuffer? vertexBuffer, uint stride)
        {
            uint ustride = stride;
            uint uoffset = 0;
#nullable disable
            ID3D11Buffer* buffer = (ID3D11Buffer*)vertexBuffer?.NativePointer;
#nullable enable
            context.IASetVertexBuffers(0, 1, &buffer, &ustride, &uoffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertexBuffer(IBuffer? vertexBuffer, uint stride, uint offset)
        {
            uint ustride = stride;
            uint uoffset = offset;
#nullable disable
            ID3D11Buffer* buffer = (ID3D11Buffer*)vertexBuffer?.NativePointer;
#nullable enable
            context.IASetVertexBuffers(0, 1, &buffer, &ustride, &uoffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertexBuffer(uint slot, IBuffer? vertexBuffer, uint stride)
        {
            uint uslot = slot;
            uint ustride = stride;
            uint uoffset = 0;
#nullable disable
            ID3D11Buffer* buffer = (ID3D11Buffer*)vertexBuffer?.NativePointer;
#nullable enable
            context.IASetVertexBuffers(uslot, 1, &buffer, &ustride, &uoffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertexBuffers<T>(uint slot, Span<T> vertexBuffers, uint stride, uint offset) where T : IBuffer
        {
            ID3D11Buffer** buffers = stackalloc ID3D11Buffer*[vertexBuffers.Length];

            for (int i = 0; i < vertexBuffers.Length; i++)
            {
                buffers[i] = (ID3D11Buffer*)vertexBuffers[i].NativePointer;
            }

            uint uslot = slot;
            uint ustride = stride;
            uint uoffset = offset;

            context.IASetVertexBuffers(uslot, (uint)vertexBuffers.Length, buffers, &ustride, &uoffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertexBuffer(uint slot, IBuffer? vertexBuffer, uint stride, uint offset)
        {
            uint uslot = slot;
            uint ustride = stride;
            uint uoffset = offset;
#nullable disable
            ID3D11Buffer* buffer = (ID3D11Buffer*)vertexBuffer?.NativePointer;
#nullable enable
            context.IASetVertexBuffers(uslot, 1, &buffer, &ustride, &uoffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetViewport(Viewport viewport)
        {
            var vp = Helper.Convert(viewport);
            context.RSSetViewports(1, &vp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetViewports(uint count, Viewport* viewports)
        {
            Hexa.NET.D3D11.Viewport* vps = stackalloc Hexa.NET.D3D11.Viewport[(int)count];
            Helper.Convert(viewports, vps, count);
            context.RSSetViewports(count, vps);
        }

        public void SetGraphicsPipelineState(GraphicsPipelineState? pso)
        {
            SetPipelineState(pso);
        }

        public void SetComputePipelineState(ComputePipelineState? pso)
        {
            SetPipelineState(pso);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IBuffer buffer, void* value, int size)
        {
            Hexa.NET.D3D11.MappedSubresource data;
            ID3D11Resource* resource = (ID3D11Resource*)buffer.NativePointer;
            context.Map(resource, 0, Hexa.NET.D3D11.Map.WriteDiscard, 0, &data).ThrowIf();
            Buffer.MemoryCopy(value, data.PData, data.RowPitch, size);
            context.Unmap(resource, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IBuffer buffer, void* value, int size, Map flags)
        {
            Hexa.NET.D3D11.MappedSubresource data;
            ID3D11Resource* resource = (ID3D11Resource*)buffer.NativePointer;
            context.Map(resource, 0, flags, 0, &data).ThrowIf();
            Buffer.MemoryCopy(value, data.PData, data.RowPitch, size);
            context.Unmap(resource, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(IBuffer buffer, T value) where T : unmanaged
        {
            MappedSubresource data;
            ID3D11Resource* resource = (ID3D11Resource*)buffer.NativePointer;
            context.Map(resource, 0, Hexa.NET.D3D11.Map.WriteDiscard, 0, &data).ThrowIf();

            Buffer.MemoryCopy(&value, data.PData, data.RowPitch, sizeof(T));

            context.Unmap(resource, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(IBuffer buffer, T* value, int size) where T : unmanaged
        {
            MappedSubresource data;
            ID3D11Resource* resource = (ID3D11Resource*)buffer.NativePointer;
            context.Map(resource, 0, Hexa.NET.D3D11.Map.WriteDiscard, 0, &data).ThrowIf();
            Buffer.MemoryCopy(value, data.PData, data.RowPitch, size * sizeof(T));
            context.Unmap(resource, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(IBuffer buffer, T* value, int size, Map flags) where T : unmanaged
        {
            MappedSubresource data;
            ID3D11Resource* resource = (ID3D11Resource*)buffer.NativePointer;
            context.Map(resource, 0, flags, 0, &data).ThrowIf();
            Buffer.MemoryCopy(value, data.PData, data.RowPitch, size * sizeof(T));
            context.Unmap(resource, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearState()
        {
            context.ClearState();
        }
    }

    public class Helper
    {
        public static Hexa.NET.D3D11.Viewport Convert(Viewport viewport)
        {
            return new()
            {
                TopLeftX = viewport.X,
                TopLeftY = viewport.Y,
                Width = viewport.Width,
                Height = viewport.Height,
                MinDepth = viewport.MinDepth,
                MaxDepth = viewport.MaxDepth
            };
        }

        public static unsafe void Convert(Viewport* viewports, Hexa.NET.D3D11.Viewport* vps, uint count)
        {
            for (int i = 0; i < count; i++)
            {
                vps[i] = Convert(viewports[i]);
            }
        }
    }
}