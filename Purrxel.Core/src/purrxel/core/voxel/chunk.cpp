#include "purrxel/core/voxel/chunk.hpp"

#include "purrxel/core/voxel/chunk_allocator.hpp"
#include "purrxel/core/voxel/extensions.hpp"
#include "purrxel/core/voxel/serialization/chunk_serializer.hpp"

namespace Purrxel::Core::Voxel
{
    void Chunk::Dispose(Chunk* self)
    {
        int32_t count = refCount.fetch_sub(1, std::memory_order_acq_rel) - 1;
        if (count != 0) return;

        ChunkAllocator::Free(self);
    }

    void Chunk::SetBlockInternal(Block block, int32_t x, int32_t y, int32_t z)
    {
        std::lock_guard<fmutex> lock(_lock);

        if (block.Type == 0 && !InMemory())
        {
            return;
        }
        if (block.Type != 0 && !InMemory())
        {
            Allocate(true);
        }

        DiskDirty(true);
        Dirty(true);

        int32_t index = MapToIndex(x, y, z);
        Data[index] = block;

        int32_t heightAccess = MapToIndex(Point2(x, z));

        if (block.Type == 0)
        {
            BlockCount--;
            if (MaxY[heightAccess] == y + 1)
            {
                uint8_t newMaxY = 0;
                for (int32_t yl = y - 1; yl >= 0; yl--)
                {
                    if (Data[MapToIndex(Point3(x, yl, z))].Type != 0)
                    {
                        newMaxY = static_cast<uint8_t>(yl + 1);
                        break;
                    }
                }
                MaxY[heightAccess] = newMaxY;
            }

            if (MinY[heightAccess] == y)
            {
                uint8_t newMinY = 15;
                for (int32_t yl = y; yl < 16; yl++)
                {
                    if (Data[MapToIndex(Point3(x, yl, z))].Type != 0)
                    {
                        newMinY = static_cast<uint8_t>(yl);
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
            MinY[heightAccess] = std::min(MinY[heightAccess], static_cast<uint8_t>(y));
            MaxY[heightAccess] = std::max(MaxY[heightAccess], static_cast<uint8_t>(y + 1));
        }
    }

    Block Chunk::GetBlockInternal(int32_t x, int32_t y, int32_t z) const
    {
        if (!InMemory()) return Block{};
        int32_t index = MapToIndex(x, y, z);
        if (index < CHUNK_SIZE_CUBED)
        {
            return Data[index];
        }
        else
        {
            return Block{};
        }
    }

    Serialization::ChunkPreSerialized Chunk::PreSerialize(Chunk* self)
    {
        std::lock_guard<fmutex> lock(_lock);
        DiskDirty(false);
        return Serialization::ChunkSerializer::PreSerialize(self);
    }

    void Chunk::Serialize(Chunk* self, Stream* stream, const Serialization::ChunkPreSerialized& preSerialized)
    {
        std::lock_guard<fmutex> lock(_lock);
        DiskDirty(false);
        Serialization::ChunkSerializer::Serialize(self, stream, preSerialized);
    }

    void Chunk::Serialize(Chunk* self, Stream* stream)
    {
        std::lock_guard<fmutex> lock(_lock);
        DiskDirty(false);
        Serialization::ChunkSerializer::Serialize(self, stream);
    }

    void Chunk::Deserialize(Chunk* self, Stream* stream)
    {
        std::lock_guard<fmutex> lock(_lock);
        Serialization::ChunkSerializer::Deserialize(self, stream);
    }
}
