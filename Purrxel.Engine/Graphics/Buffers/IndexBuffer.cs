namespace VoxelEngine.Graphics.Buffers
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Resources;

    public unsafe class IndexBuffer<T> : Resource, IBuffer where T : unmanaged
    {
        private BufferDesc desc;
        private int indexCount;
        private ComPtr<ID3D11Buffer> buffer;
        private readonly Format format;
        private readonly IndexFormat indexFormat;
        private readonly bool canWrite;
        private readonly bool canRead;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IndexBuffer(CpuAccessFlags cpuAccessFlags, int capacity)
        {
            if (typeof(T) == typeof(uint))
            {
                indexFormat = IndexFormat.UInt32;
            }
            else if (typeof(T) == typeof(ushort))
            {
                indexFormat = IndexFormat.UInt16;
            }
            else
            {
                throw new("Index buffers can only be type of uint or ushort");
            }
            format = typeof(T) == typeof(uint) ? Format.R32Uint : Format.R16Uint;

            var device = D3D11DeviceManager.Device;
            desc = new((uint)(sizeof(T) * capacity), Usage.Default, (uint)BindFlag.IndexBuffer, (uint)cpuAccessFlags);
            indexCount = capacity;
            if ((cpuAccessFlags & CpuAccessFlags.Write) != 0)
            {
                desc.Usage = Usage.Dynamic;
                canWrite = true;
            }
            if ((cpuAccessFlags & CpuAccessFlags.Read) != 0)
            {
                desc.Usage = Usage.Staging;
                canRead = true;
            }
            if (cpuAccessFlags == 0)
            {
                throw new InvalidOperationException("If cpu access flags are none initial data must be provided");
            }

            device.CreateBuffer(ref desc, null, out buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IndexBuffer(CpuAccessFlags cpuAccessFlags, Span<T> indices)
        {
            if (typeof(T) == typeof(uint))
            {
                indexFormat = IndexFormat.UInt32;
            }
            else if (typeof(T) == typeof(ushort))
            {
                indexFormat = IndexFormat.UInt16;
            }
            else
            {
                throw new("Index buffers can only be type of uint or ushort");
            }
            format = typeof(T) == typeof(uint) ? Format.R32Uint : Format.R16Uint;

            var device = D3D11DeviceManager.Device;
            desc = new((uint)(sizeof(T) * indices.Length), Usage.Default, (uint)BindFlag.IndexBuffer, (uint)cpuAccessFlags);
            indexCount = indices.Length;
            if ((cpuAccessFlags & CpuAccessFlags.Write) != 0)
            {
                desc.Usage = Usage.Dynamic;
                canWrite = true;
            }
            if ((cpuAccessFlags & CpuAccessFlags.Read) != 0)
            {
                desc.Usage = Usage.Staging;
                canRead = true;
            }

            fixed (T* pData = indices)
            {
                SubresourceData subresourceData = new(pData);
                device.CreateBuffer(ref desc, ref subresourceData, out buffer);
            }
        }

        public int Count => indexCount;

        public nint NativePointer => (nint)buffer.Handle;

        public IndexFormat IndexFormat => indexFormat;

        public bool CanWrite => canWrite;

        public bool CanRead => canRead;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Resize(int size)
        {
            if (buffer.Handle != null)
            {
                buffer.Release();
                buffer = null;
            }

            var device = D3D11DeviceManager.Device;

            desc.ByteWidth = (uint)(size * sizeof(T));
            device.CreateBuffer(ref desc, null, out buffer);
            indexCount = size;
        }

        public void Write(GraphicsContext context, T* verts, int count)
        {
            context.Write(this, verts, count);
        }

        public void Bind(GraphicsContext context)
        {
            context.SetIndexBuffer(this, format, 0);
        }

        public static implicit operator ComPtr<ID3D11Buffer>(IndexBuffer<T> buffer) => buffer.buffer;

        protected override void DisposeCore()
        {
            if (buffer.Handle != null)
            {
                buffer.Release();
                buffer = null;
            }
        }
    }
}