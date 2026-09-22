namespace VoxelEngine.Graphics.Buffers
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Resources;

    public unsafe class ConstantBuffer<T> : Resource, IConstantBuffer<T> where T : unmanaged
    {
        private readonly string dbgName;
        private BufferDesc description;
        public ComPtr<ID3D11Buffer> Buffer;

        private T* items;
        private int count;

        public ConstantBuffer(T* value, int count, CpuAccessFlags accessFlags, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"ConstantBuffer: {Path.GetFileNameWithoutExtension(file)}, Line:{line}";
            var device = D3D11DeviceManager.Device;
            Usage usage = accessFlags switch
            {
                CpuAccessFlags.Write => Usage.Dynamic,
                CpuAccessFlags.Read => Usage.Staging,
                CpuAccessFlags.None => Usage.Immutable,
                _ => throw new NotImplementedException(),
            };

            description = new((uint)(sizeof(T) * count), usage, (uint)BindFlag.ConstantBuffer, (uint)accessFlags);
            if (accessFlags != 0)
            {
                items = AllocCopyT(value, count);
                this.count = count;
            }

            var subresourceData = new SubresourceData(value);
            device.CreateBuffer(ref description, ref subresourceData, out Buffer);

            //Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public ConstantBuffer(T value, CpuAccessFlags accessFlags, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"ConstantBuffer: {Path.GetFileNameWithoutExtension(file)}, Line:{line}";
            var device = D3D11DeviceManager.Device;
            Usage usage = accessFlags switch
            {
                CpuAccessFlags.Write => Usage.Dynamic,
                CpuAccessFlags.Read => Usage.Staging,
                CpuAccessFlags.None => Usage.Immutable,
                _ => throw new NotImplementedException(),
            };

            description = new((uint)sizeof(T), usage, (uint)BindFlag.ConstantBuffer, (uint)accessFlags);
            if (accessFlags != 0)
            {
                items = AllocT<T>(); ZeroMemoryT(items);
                count = 1;
            }
            var subresourceData = new SubresourceData(&value);
            device.CreateBuffer(ref description, ref subresourceData, out Buffer);
            //Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public ConstantBuffer(int count, CpuAccessFlags accessFlags, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"ConstantBuffer: {Path.GetFileNameWithoutExtension(file)}, Line:{line}";
            var device = D3D11DeviceManager.Device;
            Usage usage = accessFlags switch
            {
                CpuAccessFlags.Write => Usage.Dynamic,
                CpuAccessFlags.Read => Usage.Staging,
                0 => throw new NotSupportedException("Immutable buffers need initial data"),
                _ => throw new NotImplementedException(),
            };

            description = new((uint)(sizeof(T) * count), usage, (uint)BindFlag.ConstantBuffer, (uint)accessFlags);
            items = AllocT<T>(count); ZeroMemoryT(items, count);
            this.count = count;
            device.CreateBuffer(ref description, null, out Buffer);
            //Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public ConstantBuffer(CpuAccessFlags accessFlags, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            dbgName = $"ConstantBuffer: {Path.GetFileNameWithoutExtension(file)}, Line:{line}";
            var device = D3D11DeviceManager.Device;
            Usage usage = accessFlags switch
            {
                CpuAccessFlags.Write => Usage.Dynamic,
                CpuAccessFlags.Read => Usage.Staging,
                0 => throw new NotSupportedException("Immutable buffers need initial data"),
                _ => throw new NotImplementedException(),
            };

            description = new((uint)sizeof(T), usage, (uint)BindFlag.ConstantBuffer, (uint)accessFlags);
            items = AllocT<T>(); ZeroMemoryT(items);
            count = 1;
            device.CreateBuffer(ref description, null, out Buffer);
            //Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public T this[int index]
        {
            get { return items[index]; }
            set
            {
                items[index] = value;
            }
        }

        public T* Local => items;

        public ref T Data => ref items[0];

        public nint NativePointer => (nint)Buffer.Handle;

        public void Update(GraphicsContext context)
        {
            context.Write(this, items, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(GraphicsContext context, T value)
        {
            *items = value;
            context.Write(this, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void UpdateRange(GraphicsContext context, T* values, int length)
        {
            MemcpyT(values, items, length);
            context.Write(this, values, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateRange(GraphicsContext context, T[] values)
        {
            fixed (T* pValues = values)
            {
                UpdateRange(context, pValues, values.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int length)
        {
            var device = D3D11DeviceManager.Device;
            if (Buffer.Handle != null)
            {
                Buffer.Release();
                Buffer = null;
            }
            description.ByteWidth = (uint)(sizeof(T) * length);
            device.CreateBuffer(ref description, null, out Buffer);
            //Buffer.DebugName = nameof(ConstantBuffer<T>);
        }

        public static implicit operator ComPtr<ID3D11Buffer>(ConstantBuffer<T> value)
        {
            return value.Buffer;
        }

        protected override void DisposeCore()
        {
            if (Buffer.Handle != null)
            {
                Buffer.Release();
                Buffer = null;
            }

            if (items != null)
            {
                Free(items);
                items = null;
                count = 0;
            }
        }
    }
}