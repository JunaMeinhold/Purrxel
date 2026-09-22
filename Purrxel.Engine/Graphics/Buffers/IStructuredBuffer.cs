namespace Purrxel.Engine.Graphics.Buffers
{
    using Hexa.NET.D3D11;
    using Purrxel.Engine.Graphics;
    using Purrxel.Engine.Graphics.D3D11;
    using System;

    public interface IStructuredBuffer : IBuffer
    {
        uint Capacity { get; set; }

        uint Count { get; }

        string? DebugName { get; }

        BufferDesc Description { get; }

        ResourceDimension Dimension { get; }
        bool IsDisposed { get; }

        int Length { get; }

        ShaderResourceView SRV { get; }

        event EventHandler<CapacityChangedEventArgs> Resize;

        void Clear();

        void EnsureCapacity(uint capacity);

        void Erase();

        void RemoveAt(int index);

        void ResetCounter();

        bool Update(GraphicsContext context);
    }
}