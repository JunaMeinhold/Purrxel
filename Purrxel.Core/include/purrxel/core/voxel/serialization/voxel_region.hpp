#pragma once

#include "purrxel/core/io/stream.hpp"
#include "purrxel/core/point2.hpp"
#include "purrxel/core/voxel/chunk_segment.hpp"
#include "purrxel/core/voxel/serialization/free_list_entry.hpp"
#include "purrxel/core/voxel/serialization/voxel_region_seek_table_entry.hpp"
#include "purrxel/pch/std.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    struct VoxelRegion
    {
        static constexpr int32_t CHUNK_REGION_SIZE = 32;
        static constexpr int32_t CHUNK_REGION_SIZE_SQUARED = CHUNK_REGION_SIZE * CHUNK_REGION_SIZE;

        VoxelRegion();

        void Serialize(Stream* stream);
        void Deserialize(Stream* stream);

        bool ReadSegment(Stream* baseStream, Stream* compressedStream, ChunkSegmentData* segment, Point2 point);
        void WriteSegment(Stream* baseStream, Stream* compressedStream, ChunkSegmentData* segment, Point2 point);

        void Flush(Stream* stream);

        bool Exists(Point2 point) const;

    private:
        void FreeEntry(Stream* stream, VoxelRegionSeekTableEntry entry);
        int32_t FindInsertionIndex(int64_t start) const;
        void Defragment(Stream* stream);
        void MoveBlock(Stream* stream, int64_t fromPos, int64_t toPos, int64_t length);

        VoxelRegionSeekTableEntry seekTable[CHUNK_REGION_SIZE_SQUARED];
        std::vector<FreeListEntry> freeList;
        int64_t fragmentedBytes = 0;

        int64_t segmentsStart = 0;
        int64_t segmentsLength = 0;
    };
}
