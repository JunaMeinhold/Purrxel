namespace VoxelEngine.Graphics.Primitives
{
    using System.Numerics;
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Graphics.Buffers;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Mathematics;

    public unsafe class Plane : IPrimitive
    {
        private readonly VertexBuffer<MeshVertex> vertexBuffer;
        private readonly IndexBuffer<ushort> indexBuffer;
        private bool disposedValue;

        public Plane(float size)
        {
            CreatePlane(out vertexBuffer, out indexBuffer, size);
        }

        public static void CreatePlane(out VertexBuffer<MeshVertex> vertexBuffer, out IndexBuffer<ushort> indexBuffer, float size = 1)
        {
            vertexBuffer = new(0,
            [
             new(new(-1 * size, 1 * size, 0), new Vector2(0,0), new(0,0,-1), new(1,0,0), new(0,1,0)),
             new(new(-1 * size, -1 * size, 0), new Vector2(0,1), new(0,0,-1), new(1,0,0), new(0,1,0)),
             new(new(1 * size, 1 * size, 0), new Vector2(1,0), new(0,0,-1), new(1,0,0), new(0,1,0)),
             new(new(1 * size, -1 * size, 0), new Vector2(1,1), new(0,0,-1), new(1,0,0), new(0,1,0))
            ]);

            indexBuffer = new(0,
            [
             0, 3, 1,
             0, 2, 3
            ]);
        }

        public void DrawAuto(GraphicsContext context, GraphicsPipelineState pso)
        {
            context.SetGraphicsPipelineState(pso);
            vertexBuffer.Bind(context, 0);
            if (indexBuffer != null)
            {
                indexBuffer.Bind(context);
                context.DrawIndexedInstanced((uint)indexBuffer.Count, 1, 0, 0, 0);
                context.SetIndexBuffer(null, 0, 0);
            }
            else
            {
                context.DrawInstanced((uint)vertexBuffer.Count, 1, 0, 0);
            }
            context.SetVertexBuffer(null, 0);
            context.SetGraphicsPipelineState(null);
        }

        public void DrawAuto(GraphicsContext context)
        {
            vertexBuffer.Bind(context, 0);
            if (indexBuffer != null)
            {
                indexBuffer.Bind(context);
                context.DrawIndexedInstanced((uint)indexBuffer.Count, 1, 0, 0, 0);
                context.SetIndexBuffer(null, 0, 0);
            }
            else
            {
                context.DrawInstanced((uint)vertexBuffer.Count, 1, 0, 0);
            }
            context.SetVertexBuffer(null, 0);
        }

        public void Bind(GraphicsContext context, out int vertexCount, out int indexCount, out int instanceCount)
        {
            vertexBuffer.Bind(context, 0);
            vertexCount = vertexBuffer.Count;
            indexBuffer.Bind(context);
            indexCount = indexBuffer?.Count ?? 0;
            instanceCount = 1;
        }

        public void Unbind(GraphicsContext context)
        {
            context.SetVertexBuffer(null, 1);
            context.SetIndexBuffer(null, 0, 0);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                vertexBuffer?.Dispose();
                indexBuffer?.Dispose();
                disposedValue = true;
            }
        }

        ~Plane()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}