#pragma once

#include "purrxel/core/io/stream.hpp"
#include "purrxel/core/point2.hpp"
#include "purrxel/core/point3.hpp"
#include "purrxel/core/voxel/chunk.hpp"
#include "purrxel/core/voxel/serialization/chunk_pre_serialized.hpp"

namespace Purrxel::Core::Voxel
{
    struct ChunkSegment
    {
        static constexpr int32_t CHUNK_SEGMENT_SIZE = 16;

        Point2 Position;
        Chunk* Chunks[CHUNK_SEGMENT_SIZE] = {};

        ChunkSegment() = default;
        explicit ChunkSegment(Point2 position) : Position(position) {}

        bool IsEmpty() const noexcept { return Chunks[0] == nullptr; }
        bool InMemory() const noexcept { return Chunks[0] != nullptr && Chunks[0]->InMemory(); }
        bool MissingNeighbours() const noexcept;

        void SetBlock(Point3 pos, Block block);

        void FreeSimulationMemory()
        {
            for (int32_t i = 0; i < CHUNK_SEGMENT_SIZE; i++)
            {
                Chunks[i]->FreeSimulationMemory();
            }
        }

        int64_t PreSerialize(Serialization::ChunkPreSerialized (&preSerializeds)[CHUNK_SEGMENT_SIZE]);
        void Serialize(Stream* stream, Serialization::ChunkPreSerialized (&preSerializeds)[CHUNK_SEGMENT_SIZE]);
        void LoadFromStream(Stream* stream);

        friend bool operator==(const ChunkSegment& left, const ChunkSegment& right) noexcept { return left.Position == right.Position; }
        friend bool operator!=(const ChunkSegment& left, const ChunkSegment& right) noexcept { return !(left == right); }
    };
}

template <>
struct std::hash<Purrxel::Core::Voxel::ChunkSegment>
{
    size_t operator()(const Purrxel::Core::Voxel::ChunkSegment& segment) const noexcept
    {
        return std::hash<Purrxel::Core::Point2>{}(segment.Position);
    }
};
