namespace VoxelEngine.Graphics.Buffers
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Core;
    using VoxelEngine.Graphics.D3D11;

    /// <summary>
    /// Represents a structured buffer in graphics memory containing elements of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the buffer. Must be an unmanaged type.</typeparam>
    public unsafe class StructuredBuffer<T> : IStructuredBuffer where T : unmanaged
    {
        private const int DefaultCapacity = 128;

        private readonly string dbgName;
        private ComPtr<ID3D11Buffer> buffer;
        private ComPtr<ID3D11ShaderResourceView> srv;
        private BufferDesc description;

        private T* items;
        private uint count;
        private volatile bool isDirty;
        private uint capacity;

        private readonly EventHandlers<CapacityChangedEventArgs> handlers = new();

        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="StructuredBuffer{T}"/> class with default capacity.
        /// </summary>
        /// <param name="cpuAccessFlags">The CPU access flags indicating how the CPU can access the buffer.</param>
        /// <param name="filename">The name of the file calling this constructor (automatically set by the compiler).</param>
        /// <param name="lineNumber">The line number in the file where this constructor is called (automatically set by the compiler).</param>
        public StructuredBuffer(CpuAccessFlags cpuAccessFlags, [CallerFilePath] string filename = "", [CallerLineNumber] int lineNumber = 0)
        {
            dbgName = $"StructuredBuffer: {Path.GetFileNameWithoutExtension(filename)}, Line:{lineNumber}";
            description = new((uint)(sizeof(T) * DefaultCapacity), Usage.Default, (uint)BindFlag.ShaderResource, (uint)cpuAccessFlags, (uint)ResourceMiscFlag.BufferStructured, (uint)sizeof(T));
            if (cpuAccessFlags.HasFlag(CpuAccessFlags.Write))
            {
                description.Usage = Usage.Dynamic;
            }
            if (cpuAccessFlags.HasFlag(CpuAccessFlags.Read))
            {
                description.Usage = Usage.Staging;
            }
            var device = D3D11DeviceManager.Device;
            capacity = DefaultCapacity;
            items = AllocT<T>(DefaultCapacity);
            ZeroMemory(items, DefaultCapacity * sizeof(T));
            SubresourceData subresourceData = new(items);
            device.CreateBuffer(ref description, &subresourceData, out buffer);
            Utils.SetDebugName(buffer, dbgName);
            device.CreateShaderResourceView(buffer.As<ID3D11Resource>(), null, out srv);
            Utils.SetDebugName(buffer, dbgName + ".SRV");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StructuredBuffer{T}"/> class with a specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the buffer.</param>
        /// <param name="cpuAccessFlags">The CPU access flags indicating how the CPU can access the buffer.</param>
        /// <param name="filename">The name of the file calling this constructor (automatically set by the compiler).</param>
        /// <param name="lineNumber">The line number in the file where this constructor is called (automatically set by the compiler).</param>
        public StructuredBuffer(uint initialCapacity, CpuAccessFlags cpuAccessFlags, [CallerFilePath] string filename = "", [CallerLineNumber] int lineNumber = 0)
        {
            dbgName = $"StructuredBuffer: {Path.GetFileNameWithoutExtension(filename)}, Line:{lineNumber}";
            description = new((uint)(sizeof(T) * (int)initialCapacity), Usage.Default, (uint)BindFlag.ShaderResource, (uint)cpuAccessFlags, (uint)ResourceMiscFlag.BufferStructured, (uint)sizeof(T));
            if (cpuAccessFlags.HasFlag(CpuAccessFlags.Write))
            {
                description.Usage = Usage.Dynamic;
            }
            if (cpuAccessFlags.HasFlag(CpuAccessFlags.Read))
            {
                description.Usage = Usage.Staging;
            }
            var device = D3D11DeviceManager.Device;
            capacity = initialCapacity;
            items = AllocT<T>(initialCapacity);
            ZeroMemory(items, (int)initialCapacity * sizeof(T));
            SubresourceData subresourceData = new(items);
            device.CreateBuffer(ref description, &subresourceData, out buffer);
            Utils.SetDebugName(buffer, dbgName);
            device.CreateShaderResourceView(buffer.As<ID3D11Resource>(), null, out srv);
            Utils.SetDebugName(buffer, dbgName + ".SRV");
        }

        /// <summary>
        /// Gets the shader resource view (SRV) associated with the buffer.
        /// </summary>
        public ShaderResourceView SRV => srv;

        /// <summary>
        /// Gets the number of elements currently in the buffer.
        /// </summary>
        public uint Count => count;

        /// <summary>
        /// Gets or sets the capacity (maximum number of elements) of the buffer.
        /// </summary>
        public uint Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => capacity;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (capacity == value)
                {
                    return;
                }

                var oldCapacity = capacity;
                var tmp = AllocT<T>((int)value);
                var oldsize = count * sizeof(T);
                var newsize = value * sizeof(T);
                Buffer.MemoryCopy(items, tmp, newsize, oldsize > newsize ? newsize : oldsize);
                Free(items);
                items = tmp;
                capacity = value;
                count = capacity < count ? capacity : count;
                srv.Dispose();

                buffer.Dispose();
                var device = D3D11DeviceManager.Device;
                SubresourceData subresourceData = new(items);
                device.CreateBuffer(ref description, &subresourceData, out buffer);
                Utils.SetDebugName(buffer, dbgName);

                device.CreateShaderResourceView(buffer.As<ID3D11Resource>(), null, out srv);
                Utils.SetDebugName(buffer, dbgName + ".SRV");

                handlers.Invoke(this, new((int)oldCapacity, (int)value));
            }
        }

        public event EventHandler<CapacityChangedEventArgs> Resize
        {
            add => handlers.AddHandler(value);
            remove => handlers.RemoveHandler(value);
        }

        /// <summary>
        /// Gets the description of the buffer, specifying its size, usage, and other properties.
        /// </summary>
        public BufferDesc Description => description;

        /// <summary>
        /// Gets the length, in bytes, of the buffer.
        /// </summary>
        public int Length => (int)description.ByteWidth;

        /// <summary>
        /// Gets the dimension (type) of the resource.
        /// </summary>
        public ResourceDimension Dimension => ResourceDimension.Buffer;

        /// <summary>
        /// Gets the native pointer associated with the buffer.
        /// </summary>
        public nint NativePointer => (nint)buffer.Handle;

        /// <summary>
        /// Gets or sets the debug name of the buffer.
        /// </summary>
        public string? DebugName { get => dbgName; }

        /// <summary>
        /// Gets a value indicating whether the buffer is disposed.
        /// </summary>
        public bool IsDisposed => disposedValue;

        /// <summary>
        ///
        /// </summary>
        public T* Items => items;

        /// <summary>
        /// Gets or sets the element at the specified index in the buffer.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => items[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                items[index] = value;
                isDirty = true;
            }
        }

        /// <summary>
        /// Gets or sets the element at the specified index in the buffer.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        public T this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => items[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                items[index] = value;
                isDirty = true;
            }
        }

        /// <summary>
        /// Resets the item counter to zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetCounter()
        {
            count = 0;
        }

        /// <summary>
        /// Clears the buffer by resetting the item counter to zero and marking the buffer as dirty.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            count = 0;
            isDirty = true;
        }

        /// <summary>
        /// Erases the contents of the buffer by zeroing out the memory and resetting the item counter to zero and marking the buffer as dirty.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Erase()
        {
            ZeroMemoryT(items, capacity);
            count = 0;
            isDirty = true;
        }

        /// <summary>
        /// Ensures that the buffer has the specified capacity. If the current capacity is less than the specified capacity,
        /// the buffer is resized to accommodate the new capacity.
        /// </summary>
        /// <param name="capacity">The desired capacity.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(uint capacity)
        {
            if (this.capacity < capacity)
            {
                Grow(capacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow(uint capacity)
        {
            uint newcapacity = count == 0 ? DefaultCapacity : 2 * count;

            if (newcapacity < capacity)
            {
                newcapacity = capacity;
            }

            Capacity = newcapacity;
        }

        /// <summary>
        /// Adds an item to the buffer, updating the count and marking the buffer as dirty.
        /// </summary>
        /// <param name="item">The item to add to the buffer.</param>
        public void Add(T item)
        {
            uint index = count;
            count++;
            EnsureCapacity(count);
            items[index] = item;
            isDirty = true;
        }

        /// <summary>
        /// Removes the specified item from the buffer.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns><c>true</c> if the item is successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(T item)
        {
            int idx = IndexOf(item);
            if (idx == -1)
            {
                return false;
            }
            RemoveAt(idx);
            isDirty = true;
            return true;
        }

        /// <summary>
        /// Determines whether the buffer contains the specified item.
        /// </summary>
        /// <param name="item">The item to locate in the buffer.</param>
        /// <returns><c>true</c> if the item is found in the buffer; otherwise, <c>false</c>.</returns>
        public bool Contains(T item)
        {
            for (int i = 0; i < count; i++)
            {
                var it = items[i];
                if (item.Equals(it))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the index of the specified item in the buffer.
        /// </summary>
        /// <param name="item">The item to locate in the buffer.</param>
        /// <returns>The index of the item if found; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            for (int i = 0; i < count; i++)
            {
                var it = items[i];
                if (item.Equals(it))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Removes the item at the specified index from the buffer.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            var size = (count - index) * sizeof(T);
            Buffer.MemoryCopy(&items[index + 1], &items[index], size, size);
            isDirty = true;
        }

        /// <summary>
        /// Updates the buffer data on the graphics context if the buffer is marked as dirty.
        /// </summary>
        /// <param name="context">The graphics context.</param>
        /// <returns><c>true</c> if the buffer data was updated; otherwise, <c>false</c>.</returns>
        public bool Update(GraphicsContext context)
        {
            if (isDirty)
            {
                context.Write(this, items, (int)count);
                isDirty = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Releases the resources held by the structured buffer.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                handlers.Clear();

                srv.Dispose();
                buffer.Dispose();
                if (items != null)
                {
                    Free(items);
                    items = null;
                }

                count = 0;
                capacity = 0;
                disposedValue = true;
            }
        }

        /// <summary>
        /// Releases the resources held by the structured buffer.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}