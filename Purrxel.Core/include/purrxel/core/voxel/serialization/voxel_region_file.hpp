#pragma once

#include "purrxel/core/io/stream.hpp"
#include "purrxel/utils/memory.hpp"
#include "purrxel/core/point2.hpp"
#include "purrxel/core/voxel/chunk_segment.hpp"
#include "purrxel/core/voxel/serialization/lz4_stream.hpp"
#include "purrxel/core/voxel/serialization/stream_mode.hpp"
#include "purrxel/core/voxel/serialization/voxel_region.hpp"
#include "purrxel/pch/std.hpp"
#include "purrxel/utils/semaphore.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    class VoxelRegionFile
    {
    public:
        VoxelRegionFile();

        Point2 Id() const noexcept { return id; }
        int32_t CurrentLockCount() const noexcept { return static_cast<int32_t>(regionSemaphore.current_count()); }
        std::chrono::steady_clock::time_point LastAccess() const noexcept { return lastAccess; }
        void SetLastAccess(std::chrono::steady_clock::time_point value) noexcept { lastAccess = value; }

        void Dispose(bool full);
        void Reset(Point2 regionId, ObjPtr<Stream> stream, bool newFile, StreamMode mode);
        void Lock(StreamMode mode);

        void Close(bool write);

        void WriteSegment(ChunkSegmentData* segment);
        bool ReadSegment(ChunkSegmentData* segment);
        bool Exists(Point2 position) const;

    private:
        semaphore regionSemaphore{1};
        Point2 id;
        std::chrono::steady_clock::time_point lastAccess{};
        VoxelRegion region;
        ObjPtr<Stream> stream;
        ObjPtr<Lz4Stream> lz4Stream;
    };
}
