#include "purrxel/core/voxel/chunk_segment.hpp"

#include "purrxel/core/voxel/extensions.hpp"
#include "purrxel/core/voxel/serialization/chunk_serializer.hpp"

namespace Purrxel::Core::Voxel
{
    bool ChunkSegmentData::MissingNeighbours() const noexcept
    {
        for (int32_t i = 0; i < CHUNK_SEGMENT_SIZE; i++)
        {
            if (Chunks[i]->MissingNeighbours()) return true;
        }
        return false;
    }

    void ChunkSegmentData::SetBlock(Point3 pos, Block block)
    {
        if (pos.Y < 0 || pos.Y > 255) return;
        int32_t index = pos.Y >> 4;
        int32_t height = pos.Y & 15;

        Chunk* chunk = Chunks[index];

        if (block.Type == 0 && !chunk->InMemory()) return;
        if (block.Type != 0 && !chunk->InMemory()) chunk->Allocate(true);

        int32_t heightAccess = MapToIndex(Point2(pos.X, pos.Z));
        if (block.Type == 0)
        {
            chunk->BlockCount--;
            if (chunk->MaxY[heightAccess] == height + 1)
            {
                uint8_t newMaxY = 0;
                for (int32_t y = height; y >= 0; y--)
                {
                    if (chunk->Data[MapToIndex(Point3(pos.X, y, pos.Z))].Type != 0)
                    {
                        newMaxY = static_cast<uint8_t>(y + 1);
                        break;
                    }
                }
                chunk->MaxY[heightAccess] = newMaxY;
            }

            if (chunk->MinY[heightAccess] == height)
            {
                uint8_t newMinY = 15;
                for (int32_t y = height; y <= 16; y++)
                {
                    if (chunk->Data[MapToIndex(Point3(pos.X, y, pos.Z))].Type != 0)
                    {
                        newMinY = static_cast<uint8_t>(y);
                        break;
                    }
                }
                chunk->MinY[heightAccess] = newMinY;
            }

            if (chunk->BlockCount == 0) { chunk->FreeMemory(); }
        }
        else
        {
            chunk->BlockCount++;
            chunk->MinY[heightAccess] = std::min(chunk->MinY[heightAccess], static_cast<uint8_t>(height));
            chunk->MaxY[heightAccess] = std::max(chunk->MaxY[heightAccess], static_cast<uint8_t>(height + 1));
        }

        chunk->Data[MapToIndex(Point3(pos.X, height, pos.Z))] = block;
    }

    int64_t ChunkSegmentData::PreSerialize(Serialization::ChunkPreSerialized (&preSerializeds)[CHUNK_SEGMENT_SIZE])
    {
        int64_t size = 0;
        for (int32_t i = 0; i < CHUNK_SEGMENT_SIZE; i++)
        {
            Serialization::ChunkPreSerialized result = Serialization::ChunkSerializer::PreSerialize(Chunks[i]);
            size += result.Length;
            preSerializeds[i] = std::move(result);
        }
        return size;
    }

    void ChunkSegmentData::Serialize(Stream* stream, Serialization::ChunkPreSerialized (&preSerializeds)[CHUNK_SEGMENT_SIZE])
    {
        for (int32_t i = 0; i < CHUNK_SEGMENT_SIZE; i++)
        {
            Serialization::ChunkSerializer::Serialize(Chunks[i], stream, preSerializeds[i]);
        }
    }

    void ChunkSegmentData::LoadFromStream(Stream* stream)
    {
        for (int32_t i = 0; i < CHUNK_SEGMENT_SIZE; i++)
        {
            Serialization::ChunkSerializer::Deserialize(Chunks[i], stream);
        }
    }
}
