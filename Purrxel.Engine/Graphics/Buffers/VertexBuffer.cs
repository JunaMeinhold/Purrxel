namespace VoxelEngine.Graphics.Buffers
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Resources;

    public unsafe class VertexBuffer<T> : Resource, IBuffer where T : unmanaged
    {
        private BufferDesc desc;
        private int vertexCount;
        private ComPtr<ID3D11Buffer> vertexBuffer;
        private readonly uint stride;
        private readonly bool canWrite;
        private readonly bool canRead;

        public VertexBuffer(CpuAccessFlags cpuAccessFlags, int capacity)
        {
            var device = D3D11DeviceManager.Device;
            vertexCount = capacity;
            stride = (uint)sizeof(T);
            if (cpuAccessFlags == 0) throw new ArgumentException("Vertex Buffer cannot be immutable", nameof(cpuAccessFlags));

            Usage usage = Usage.Immutable;
            if ((cpuAccessFlags & CpuAccessFlags.Write) != 0)
            {
                usage = Usage.Dynamic;
                canWrite = true;
            }
            if ((cpuAccessFlags & CpuAccessFlags.Read) != 0)
            {
                usage = Usage.Staging;
                canRead = true;
            }

            desc = new((uint)(capacity * sizeof(T)), usage, (uint)BindFlag.VertexBuffer, (uint)cpuAccessFlags, structureByteStride: (uint)sizeof(T));
            device.CreateBuffer(ref desc, null, out vertexBuffer);
        }

        public VertexBuffer(CpuAccessFlags cpuAccessFlags, Span<T> vertices)
        {
            var device = D3D11DeviceManager.Device;
            stride = (uint)sizeof(T);

            Usage usage = Usage.Immutable;
            if ((cpuAccessFlags & CpuAccessFlags.Write) != 0)
            {
                usage = Usage.Dynamic;
                canWrite = true;
            }
            if ((cpuAccessFlags & CpuAccessFlags.Read) != 0)
            {
                usage = Usage.Staging;
                canRead = true;
            }

            desc = new((uint)(vertices.Length * sizeof(T)), usage, (uint)BindFlag.VertexBuffer, (uint)cpuAccessFlags, structureByteStride: (uint)sizeof(T));

            fixed (T* pVertices = vertices)
            {
                SubresourceData subresource = new(pVertices);
                device.CreateBuffer(ref desc, ref subresource, out vertexBuffer);
            }
            vertexCount = vertices.Length;
        }

        public int Count => vertexCount;

        public unsafe int Stride { get; } = sizeof(T);

        public nint NativePointer => (nint)vertexBuffer.Handle;

        public bool CanWrite => canWrite;

        public bool CanRead => canRead;

        public void Resize(int size)
        {
            var device = D3D11DeviceManager.Device;
            if (vertexBuffer.Handle != null)
            {
                vertexBuffer.Release();
                vertexBuffer = default;
            }
            desc.ByteWidth = (uint)(size * sizeof(T));
            device.CreateBuffer(ref desc, null, out vertexBuffer);
            vertexCount = size;
        }

        public void Write(GraphicsContext context, T* verts, int count)
        {
            context.Write(this, verts, count);
        }

        public void Bind(GraphicsContext context)
        {
            Bind(context, 0);
        }

        public void Bind(GraphicsContext context, int slot)
        {
            context.SetVertexBuffer((uint)slot, this, stride);
        }

        public static implicit operator ComPtr<ID3D11Buffer>(VertexBuffer<T> buffer) => buffer.vertexBuffer;

        protected override void DisposeCore()
        {
            if (vertexBuffer.Handle != null)
            {
                vertexBuffer.Release();
                vertexBuffer = null;
            }
        }
    }
}