namespace Purrxel.Engine.Voxel
{
    using Hexa.NET.Mathematics;
    using Purrxel.Engine.Voxel.Meshing;
    using Purrxel.Engine.Voxel.Metadata;
    using Purrxel.Engine.Voxel.Serialization;
    using System.Diagnostics;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public unsafe struct Chunk
    {
        public const int EMPTY = 0;
        public const int CHUNK_SIZE = 16;
        public const int CHUNK_SIZE_SQUARED = CHUNK_SIZE * CHUNK_SIZE;
        public const int CHUNK_SIZE_CUBED = CHUNK_SIZE_SQUARED * CHUNK_SIZE;
        public const int CHUNK_SIZE_MINUS_ONE = 15;
        public const int CHUNK_SIZE_SHIFTED = CHUNK_SIZE << 6;
        public const int CHUNK_SHIFT_Y = 4;
        public const int CHUNK_SHIFT_Z = 8;
        public const int CHUNK_MASK = CHUNK_SIZE - 1;

        public int DimId;
        public Point3 Position;
        public BoundingBox BoundingBox;

        public Block* Data;
        public byte* MinY;
        public byte* MaxY;
        public ushort BlockCount;

        public BlockMetadataCollection BlockMetadata;

        public ChunkVertexBuffer OpaqueVertexBuffer = new();
        public ChunkVertexBuffer TransparentVertexBuffer = new();

        public SemaphoreLight _lock = new(1, 1);

        private InternalChunkFlags flags;

        private int refCount = 1;

        public Chunk(World map, int x, int y, int z, bool generated = false)
        {
            DimId = map.DimId;
            Position = new(x, y, z);
            Vector3 realPos = new Vector3(x, y, z) * CHUNK_SIZE;
            BoundingBox = new BoundingBox(realPos, realPos + new Vector3(CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE));
            DiskDirty = generated;
        }

        public void AddRef()
        {
            Interlocked.Increment(ref refCount);
        }

        public void Dispose(Chunk* self)
        {
            int count = Interlocked.Decrement(ref refCount);
            if (count != 0) return;

            ChunkAllocator.Free(self);
        }

        public void Allocate(bool zero)
        {
            if (InMemory) return;
            Data = AllocT<Block>(CHUNK_SIZE_CUBED);
            MinY = AllocT<byte>(CHUNK_SIZE_SQUARED);
            MaxY = AllocT<byte>(CHUNK_SIZE_SQUARED);
            BlockMetadata = new();
            if (zero)
            {
                ZeroMemoryT(Data, CHUNK_SIZE_CUBED);
                ZeroMemoryT(MaxY, CHUNK_SIZE_SQUARED);
                Memset(MinY, CHUNK_SIZE, CHUNK_SIZE_SQUARED);
            }
        }

        private enum InternalChunkFlags : byte
        {
            None = 0,
            InBuffer = 1,
            InMemory = 2,
            InSimulation = 4,
            Dirty = 8,
            DiskDirty = 16,
            MissingNeighbours = 32,
        }

        public readonly World Map => DimensionManager.GetWorld(DimId);

        public bool InBuffer
        {
            readonly get => (flags & InternalChunkFlags.InBuffer) != 0;
            private set
            {
                if (value)
                {
                    flags |= InternalChunkFlags.InBuffer;
                }
                else
                {
                    flags &= ~InternalChunkFlags.InBuffer;
                }
            }
        }

        public readonly bool InMemory => Data != null;

        public bool InSimulation
        {
            readonly get => (flags & InternalChunkFlags.InSimulation) != 0;
            private set
            {
                if (value)
                {
                    flags |= InternalChunkFlags.InSimulation;
                }
                else
                {
                    flags &= ~InternalChunkFlags.InSimulation;
                }
            }
        }

        public bool Dirty
        {
            readonly get => (flags & InternalChunkFlags.Dirty) != 0;
            private set
            {
                if (value)
                {
                    flags |= InternalChunkFlags.Dirty;
                }
                else
                {
                    flags &= ~InternalChunkFlags.Dirty;
                }
            }
        }

        public bool DiskDirty
        {
            readonly get => (flags & InternalChunkFlags.DiskDirty) != 0;
            private set
            {
                if (value)
                {
                    flags |= InternalChunkFlags.DiskDirty;
                }
                else
                {
                    flags &= ~InternalChunkFlags.DiskDirty;
                }
            }
        }

        public bool MissingNeighbours
        {
            readonly get => (flags & InternalChunkFlags.MissingNeighbours) != 0;
            internal set
            {
                if (value)
                {
                    flags |= InternalChunkFlags.MissingNeighbours;
                }
                else
                {
                    flags &= ~InternalChunkFlags.MissingNeighbours;
                }
            }
        }

        public void FreeMemory()
        {
            InBuffer = false;
            OpaqueVertexBuffer.Dispose();
            TransparentVertexBuffer.Dispose();
            if (Data != null)
            {
                Free(Data);
                Data = null;
            }
            if (MinY != null)
            {
                Free(MinY);
                MinY = null;
            }
            if (MaxY != null)
            {
                Free(MaxY);
                MaxY = null;
            }
            BlockMetadata.Release();
        }

        public void FreeSimulationMemory()
        {
            if (Data != null)
            {
                Free(Data);
                Data = null;
            }
            if (MinY != null)
            {
                Free(MinY);
                MinY = null;
            }
            if (MaxY != null)
            {
                Free(MaxY);
                MaxY = null;
            }
            BlockMetadata.Release();
        }

        public void Unload(Chunk* self)
        {
            _lock.Wait();
            try
            {
                InBuffer = false;
                OpaqueVertexBuffer.Dispose();
                TransparentVertexBuffer.Dispose();

                DimensionManager.GetWorld(DimId).Chunks.Remove(self);

                if (Data != null)
                {
                    Free(Data);
                    Data = null;
                }
                if (MinY != null)
                {
                    Free(MinY);
                    MinY = null;
                }

                if (MaxY != null)
                {
                    Free(MaxY);
                    MaxY = null;
                }

                BlockMetadata.Release();
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Generates the chunk mesh
        /// </summary>
        public void Update(Chunk* self)
        {
            _lock.Wait();
            try
            {
                if (!InMemory)
                {
                    return;
                }

                VoxelMeshFactory.GenerateMesh(self);
                Dirty = false;
            }
            finally
            {
                _lock.Release();
            }
        }

        public void SetBlockInternal(Block block, int x, int y, int z)
        {
            _lock.Wait();

            try
            {
                if (block.Type == 0 && !InMemory)
                {
                    return;
                }
                if (block.Type != 0 && !InMemory)
                {
                    Allocate(true);
                }

                DiskDirty = true;
                Dirty = true;

                int index = Extensions.MapToIndex(x, y, z);
                Data[index] = block;

                int heightAccess = new Point2(x, z).MapToIndex();

                if (block.Type == 0)
                {
                    BlockCount--;
                    if (MaxY[heightAccess] == y + 1)
                    {
                        byte newMaxY = 0;
                        for (int yl = y - 1; yl >= 0; yl--)
                        {
                            if (Data[new Point3(x, yl, z).MapToIndex()].Type != 0)
                            {
                                newMaxY = (byte)(yl + 1);
                                break;
                            }
                        }
                        MaxY[heightAccess] = newMaxY;
                    }

                    if (MinY[heightAccess] == y)
                    {
                        byte newMinY = 15;
                        for (int yl = y; yl < 16; yl++)
                        {
                            if (Data[new Point3(x, yl, z).MapToIndex()].Type != 0)
                            {
                                newMinY = (byte)yl;
                                break;
                            }
                        }
                        MinY[heightAccess] = newMinY;
                    }

                    if (BlockCount == 0)
                    {
                        FreeMemory();
                    }
                }
                else
                {
                    BlockCount++;
                    MinY[heightAccess] = Math.Min(MinY[heightAccess], (byte)y);
                    MaxY[heightAccess] = Math.Max(MaxY[heightAccess], (byte)(y + 1));
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Block GetBlockUnsafe(uint index)
        {
            return Data[index];
        }

        public Block GetBlockInternal(int x, int y, int z)
        {
            if (!InMemory) return default;
            int index = Extensions.MapToIndex(x, y, z);
            if (index < CHUNK_SIZE_CUBED)
            {
                return Data[index];
            }
            else
            {
                return default;
            }
        }

        public Block GetBlock(Point3 pos)
        {
            Block block = GetBlockInternal(pos.X, pos.Y, pos.Z);
            return block;
        }

        public void SetBlock(Block block, Point3 pos)
        {
            SetBlockInternal(block, pos.X, pos.Y, pos.Z);
        }

        public unsafe ChunkPreSerialized PreSerialize(Chunk* self)
        {
            _lock.Wait();
            try
            {
                DiskDirty = false;
                return ChunkSerializer.PreSerialize(self);
            }
            finally
            {
                _lock.Release();
            }
        }

        public unsafe void Serialize(Chunk* self, Stream stream, ChunkPreSerialized preSerialized)
        {
            _lock.Wait();
            try
            {
                DiskDirty = false;
                ChunkSerializer.Serialize(self, stream, preSerialized);
            }
            finally
            {
                _lock.Release();
            }
        }

        public unsafe void Serialize(Chunk* self, Stream stream)
        {
            _lock.Wait();
            try
            {
                DiskDirty = false;
                ChunkSerializer.Serialize(self, stream);
            }
            finally
            {
                _lock.Release();
            }
        }

        public unsafe void Deserialize(Chunk* self, Stream stream)
        {
            _lock.Wait();
            try
            {
                ChunkSerializer.Deserialize(self, stream);
            }
            finally
            {
                _lock.Release();
            }
        }

        private string GetDebuggerDisplay()
        {
            var name = $"<{Position.X},{Position.Y},{Position.Z}>";
            return $"{name}, Flags: {flags}";
        }
    }
}