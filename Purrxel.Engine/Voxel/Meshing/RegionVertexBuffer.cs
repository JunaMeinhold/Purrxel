namespace VoxelEngine.Voxel.Meshing
{
    using Hexa.NET.D3D11;
    using Hexa.NET.Mathematics;
    using System;
    using System.Numerics;
    using System.Threading;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.Buffers;

    public unsafe class RegionVertexBuffer : IDisposable
    {
        private VertexBuffer<VoxelVertex>? vertexBuffer;
        private int count;
        private readonly uint stride = (uint)sizeof(VoxelVertex);

        private bool dirty = false;

        private readonly Lock _lock = new();

        private int vertexCount;
        private MappedSubresource mappedResource;

        public RegionVertexBuffer()
        {
        }

        public int Count => count;

        public int VertexCount => vertexCount;

        public void Map(GraphicsContext context, int size)
        {
            _lock.Enter();

            count = 0;

            dirty = true;

            if (vertexBuffer == null || vertexBuffer.Count < size)
            {
                if (vertexBuffer != null)
                {
                    VertexBufferPool<VoxelVertex>.Shared.Return(vertexBuffer);
                    vertexBuffer = default;
                }

                vertexBuffer = VertexBufferPool<VoxelVertex>.Shared.Rent(size);
            }

            mappedResource = context.Map(vertexBuffer, 0, Hexa.NET.D3D11.Map.WriteDiscard, 0);
        }

        public void Unmap(GraphicsContext context)
        {
            mappedResource = default;
            context.Unmap(vertexBuffer!, 0);
            vertexCount = count;
            dirty = false;
            _lock.Exit();
        }

        private bool AppendRange(VoxelVertex* values, int count, uint chunkPosition)
        {
            int newCount = this.count + count;
            if (newCount > vertexBuffer!.Count) return false;
            VoxelVertex* vertices = (VoxelVertex*)mappedResource.PData;
            for (int i = 0, j = this.count; i < count; i++, j++)
            {
                var v = values[i];
                v.ChunkPosition = chunkPosition;
                vertices[j] = v;
            }
            this.count = newCount;
            return true;
        }

        public bool BufferData(ChunkVertexBuffer vertexBuffer, Point3 offset)
        {
            if (vertexBuffer.Count == 0 || vertexBuffer.Data == null) return true;
            bool result = AppendRange(vertexBuffer.Data, vertexBuffer.Count, VoxelVertex.PackChunkPosition(offset));
            dirty = true;
            return result;
        }

        public bool Bind(GraphicsContext context)
        {
            if (vertexBuffer == null)
            {
                return false;
            }

            context.SetVertexBuffer(vertexBuffer, stride);
            return true;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (vertexBuffer != null)
                {
                    VertexBufferPool<VoxelVertex>.Shared.Return(vertexBuffer);
                    vertexBuffer = default;
                }

                count = 0;
                GC.SuppressFinalize(this);
            }
        }

        public void Lock()
        {
            throw new NotImplementedException();
        }

        public void ReleaseLock()
        {
            throw new NotImplementedException();
        }
    }
}