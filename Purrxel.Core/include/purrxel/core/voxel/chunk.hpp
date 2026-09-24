#pragma once

#include "purrxel/pch/std.hpp"

#include "purrxel/core/allocator.h"
#include "purrxel/utils/fmutex.hpp"
#include "purrxel/core/io/stream.hpp"
#include "purrxel/core/point3.hpp"
#include "purrxel/core/voxel/block.hpp"
#include "purrxel/core/voxel/metadata/block_metadata_collection.hpp"
#include "purrxel/utils/macros.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    struct ChunkPreSerialized;
}

namespace Purrxel::Core::Voxel
{
    enum class InternalChunkFlags : uint8_t
    {
        None = 0,
        InBuffer = 1,
        InMemory = 2,
        InSimulation = 4,
        Dirty = 8,
        DiskDirty = 16,
        MissingNeighbours = 32,
    };

    DEFINE_FLAGS_OPERATORS(InternalChunkFlags, uint8_t)

    struct Chunk
    {
        static constexpr int32_t EMPTY = 0;
        static constexpr int32_t CHUNK_SIZE = 16;
        static constexpr int32_t CHUNK_SIZE_SQUARED = CHUNK_SIZE * CHUNK_SIZE;
        static constexpr int32_t CHUNK_SIZE_CUBED = CHUNK_SIZE_SQUARED * CHUNK_SIZE;
        static constexpr int32_t CHUNK_SIZE_MINUS_ONE = 15;
        static constexpr int32_t CHUNK_SIZE_SHIFTED = CHUNK_SIZE << 6;
        static constexpr int32_t CHUNK_SHIFT_Y = 4;
        static constexpr int32_t CHUNK_SHIFT_Z = 8;
        static constexpr int32_t CHUNK_MASK = CHUNK_SIZE - 1;

        int32_t DimId = 0;
        Point3 Position;

        Block* Data = nullptr;
        uint8_t* MinY = nullptr;
        uint8_t* MaxY = nullptr;
        uint16_t BlockCount = 0;

        Metadata::BlockMetadataCollection BlockMetadata;

        fmutex _lock;

        void AddRef() noexcept { refCount.fetch_add(1, std::memory_order_relaxed); }

        void Dispose(Chunk* self);

        bool InBuffer() const noexcept { return (flags & InternalChunkFlags::InBuffer) != InternalChunkFlags::None; }

        void InBuffer(bool value)
        {
            if (value)
            {
                flags |= InternalChunkFlags::InBuffer;
            }
            else
            {
                flags &= ~InternalChunkFlags::InBuffer;
            }
        }

        bool InMemory() const noexcept { return Data != nullptr; }

        bool InSimulation() const noexcept { return (flags & InternalChunkFlags::InSimulation) != InternalChunkFlags::None; }

        void InSimulation(bool value)
        {
            if (value)
            {
                flags |= InternalChunkFlags::InSimulation;
            }
            else
            {
                flags &= ~InternalChunkFlags::InSimulation;
            }
        }

        bool Dirty() const noexcept { return (flags & InternalChunkFlags::Dirty) != InternalChunkFlags::None; }

        void Dirty(bool value)
        {
            if (value)
            {
                flags |= InternalChunkFlags::Dirty;
            }
            else
            {
                flags &= ~InternalChunkFlags::Dirty;
            }
        }

        bool DiskDirty() const noexcept { return (flags & InternalChunkFlags::DiskDirty) != InternalChunkFlags::None; }

        void DiskDirty(bool value)
        {
            if (value)
            {
                flags |= InternalChunkFlags::DiskDirty;
            }
            else
            {
                flags &= ~InternalChunkFlags::DiskDirty;
            }
        }

        bool MissingNeighbours() const noexcept { return (flags & InternalChunkFlags::MissingNeighbours) != InternalChunkFlags::None; }

        void MissingNeighbours(bool value)
        {
            if (value)
            {
                flags |= InternalChunkFlags::MissingNeighbours;
            }
            else
            {
                flags &= ~InternalChunkFlags::MissingNeighbours;
            }
        }

        void Allocate(bool zero)
        {
            if (InMemory()) return;
            Data = Purrxel_Alloc<Block>(CHUNK_SIZE_CUBED);
            MinY = Purrxel_Alloc<uint8_t>(CHUNK_SIZE_SQUARED);
            MaxY = Purrxel_Alloc<uint8_t>(CHUNK_SIZE_SQUARED);
            if (zero)
            {
                std::memset(Data, 0, sizeof(Block) * CHUNK_SIZE_CUBED);
                std::memset(MaxY, 0, CHUNK_SIZE_SQUARED);
                std::memset(MinY, CHUNK_SIZE, CHUNK_SIZE_SQUARED);
            }
        }

        void FreeMemory()
        {
            if (Data != nullptr)
            {
                Purrxel_Free(Data);
                Data = nullptr;
            }
            if (MinY != nullptr)
            {
                Purrxel_Free(MinY);
                MinY = nullptr;
            }
            if (MaxY != nullptr)
            {
                Purrxel_Free(MaxY);
                MaxY = nullptr;
            }
            BlockMetadata.Release();
        }

        void FreeSimulationMemory()
        {
            if (Data != nullptr)
            {
                Purrxel_Free(Data);
                Data = nullptr;
            }
            if (MinY != nullptr)
            {
                Purrxel_Free(MinY);
                MinY = nullptr;
            }
            if (MaxY != nullptr)
            {
                Purrxel_Free(MaxY);
                MaxY = nullptr;
            }
            BlockMetadata.Release();
        }

        void SetBlockInternal(Block block, int32_t x, int32_t y, int32_t z);

        Block GetBlockUnsafe(uint32_t index) const noexcept { return Data[index]; }

        Block GetBlockInternal(int32_t x, int32_t y, int32_t z) const;

        Block GetBlock(Point3 pos) const { return GetBlockInternal(pos.X, pos.Y, pos.Z); }
        void SetBlock(Block block, Point3 pos) { SetBlockInternal(block, pos.X, pos.Y, pos.Z); }

        Serialization::ChunkPreSerialized PreSerialize(Chunk* self);
        void Serialize(Chunk* self, Stream* stream, const Serialization::ChunkPreSerialized& preSerialized);
        void Serialize(Chunk* self, Stream* stream);
        void Deserialize(Chunk* self, Stream* stream);

    private:
        InternalChunkFlags flags = InternalChunkFlags::None;
        std::atomic<int32_t> refCount{1};
    };
}
