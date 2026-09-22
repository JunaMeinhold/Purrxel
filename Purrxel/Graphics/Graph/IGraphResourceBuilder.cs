namespace App.Graphics.Graph
{
    using Hexa.NET.Mathematics;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;

    public interface IGraphResourceBuilder
    {
        IReadOnlyList<GBuffer> GBuffers { get; }

        IRenderTargetView? Output { get; }

        Viewport OutputViewport { get; }

        IReadOnlyList<Texture2D> Textures { get; }

        Viewport Viewport { get; }
        GraphResourceContainer? Container { get; set; }

        ResourceRef AddResource(string name);

        ResourceRef<T> AddResource<T>(string name) where T : class, IDisposable;

        ResourceRef<ComputePipelineState> CreateComputePipelineState(ComputePipelineDesc description, ResourceCreationFlags flags = ResourceCreationFlags.All);

        ResourceRef<ComputePipelineState> CreateComputePipelineState(string name, ComputePipelineDesc description, ResourceCreationFlags flags = ResourceCreationFlags.All);

        ResourceRef<ConstantBuffer<T>> CreateConstantBuffer<T>(string name, CpuAccessFlags accessFlags, ResourceCreationFlags flags = ResourceCreationFlags.All) where T : unmanaged;

        ResourceRef<ConstantBuffer<T>> CreateConstantBuffer<T>(string name, T value, CpuAccessFlags accessFlags, ResourceCreationFlags flags = ResourceCreationFlags.All) where T : unmanaged;

        ResourceRef<DepthStencil> CreateDepthStencilBuffer(string name, DepthStencilBufferDescription description, ResourceCreationFlags flags = ResourceCreationFlags.All);

        ResourceRef<GBuffer> CreateGBuffer(string name, GBufferDescription description, ResourceCreationFlags flags = ResourceCreationFlags.All);

        ResourceRef<GraphicsPipelineState> CreateGraphicsPipelineState(GraphicsPipelineStateDescEx description, ResourceCreationFlags flags = ResourceCreationFlags.All);

        ResourceRef<GraphicsPipelineState> CreateGraphicsPipelineState(string name, GraphicsPipelineStateDescEx description, ResourceCreationFlags flags = ResourceCreationFlags.All);

        ResourceRef<TType> CreateResource<TType, TDesc>(string name, TDesc description, Func<TDesc, TType> constructor, IList<TType> group, IList<ResourceDescriptor<TDesc>> lazyDescs, ResourceCreationFlags flags) where TDesc : struct where TType : class, IDisposable;

        ResourceRef<SamplerState> CreateSamplerState(string name, SamplerStateDescription description, ResourceCreationFlags flags = ResourceCreationFlags.All);

        ResourceRef<StructuredBuffer<T>> CreateStructuredBuffer<T>(string name, CpuAccessFlags accessFlags, ResourceCreationFlags flags = ResourceCreationFlags.All) where T : unmanaged;

        ResourceRef<StructuredBuffer<T>> CreateStructuredBuffer<T>(string name, uint initialCapacity, CpuAccessFlags accessFlags, ResourceCreationFlags flags = ResourceCreationFlags.All) where T : unmanaged;

        ResourceRef<Texture1D> CreateTexture1D(string name, Texture1DDescription description, ResourceCreationFlags flags = ResourceCreationFlags.All);

        ResourceRef<Texture2D> CreateTexture2D(string name, Texture2DDescription description, ResourceCreationFlags flags = ResourceCreationFlags.All);

        ResourceRef<Texture3D> CreateTexture3D(string name, Texture3DDescription description, ResourceCreationFlags flags = ResourceCreationFlags.All);

        bool DisposeResource(string name);

        ResourceRef<ComputePipeline> GetComputePipeline(string name);

        ResourceRef<ConstantBuffer<T>> GetConstantBuffer<T>(string name) where T : unmanaged;

        ResourceRef<DepthStencil> GetDepthStencilBuffer(string name);

        ResourceRef<GBuffer> GetGBuffer(string name);

        ResourceRef<GraphicsPipelineState> GetGraphicsPipelineState(string name);

        ResourceRef GetOrAddResource(string name);

        ResourceRef<T> GetOrAddResource<T>(string name) where T : class, IDisposable;

        ResourceRef? GetResource(string name);

        ResourceRef<T> GetResource<T>(string name) where T : class, IDisposable;

        ResourceRef<ISamplerState> GetSamplerState(string name);

        ResourceRef<StructuredBuffer<T>> GetStructuredBuffer<T>(string name) where T : unmanaged;

        ResourceRef<Texture1D> GetTexture1D(string name);

        ResourceRef<Texture2D> GetTexture2D(string name);

        ResourceRef<Texture3D> GetTexture3D(string name);

        bool RemoveResource(string name);

        bool TryGetResource(string name, [NotNullWhen(true)] out ResourceRef? resourceRef);

        bool TryGetResource<T>(string name, [NotNullWhen(true)] out ResourceRef<T>? resourceRef) where T : class, IDisposable;

        void UpdateComputePipelineState(string name, ComputePipelineDesc desc);

        void UpdateDepthStencilBuffer(string name, DepthStencilBufferDescription description);

        void UpdateGBuffer(string name, GBufferDescription description);

        void UpdateGraphicsPipelineState(string name, GraphicsPipelineStateDescEx desc);

        void UpdateResource<TType, TDesc>(string name, TDesc desc, Func<TDesc, TType> constructor, IList<TType> group) where TType : class, IDisposable;

        void UpdateSamplerState(string name, SamplerStateDescription desc);

        void UpdateTexture1D(string name, Texture1DDescription description);

        void UpdateTexture2D(string name, Texture2DDescription description);

        void UpdateTexture3D(string name, Texture3DDescription description);
    }
}