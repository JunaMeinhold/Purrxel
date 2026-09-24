#include "purrxel/core/voxel/serialization/voxel_region_file.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    VoxelRegionFile::VoxelRegionFile() = default;

    void VoxelRegionFile::Dispose(bool full)
    {
        region = VoxelRegion();
        stream = nullptr;
        if (full)
        {
            lz4Stream = nullptr;
        }
    }

    void VoxelRegionFile::Reset(Point2 regionId, ObjPtr<Stream> newStream, bool newFile, StreamMode mode)
    {
        id = regionId;
        stream = newStream;

        if (!lz4Stream)
        {
            lz4Stream = Lz4Stream::Create(stream.Get(), 8192, mode, 10);
        }
        lz4Stream->Reset(stream.Get(), mode);

        lastAccess = std::chrono::steady_clock::now();

        region = VoxelRegion();
        stream->Position(0);
        if (newFile)
        {
            region.Serialize(stream.Get());
        }
        else
        {
            region.Deserialize(stream.Get());
        }
    }

    void VoxelRegionFile::Lock(StreamMode mode)
    {
        regionSemaphore.acquire();
        if (lz4Stream)
        {
            lz4Stream->Reset(stream.Get(), mode);
        }
    }

    void VoxelRegionFile::Close(bool write)
    {
        if (write)
        {
            region.Flush(stream.Get());
        }

        regionSemaphore.release();
    }

    void VoxelRegionFile::WriteSegment(ChunkSegmentData* segment)
    {
        Point2 pointInRegion(segment->Position.X & 31, segment->Position.Y & 31);
        region.WriteSegment(stream.Get(), lz4Stream.Get(), segment, pointInRegion);
    }

    bool VoxelRegionFile::ReadSegment(ChunkSegmentData* segment)
    {
        Point2 pointInRegion(segment->Position.X & 31, segment->Position.Y & 31);
        return region.ReadSegment(stream.Get(), lz4Stream.Get(), segment, pointInRegion);
    }

    bool VoxelRegionFile::Exists(Point2 position) const
    {
        Point2 pointInRegion(position.X & 31, position.Y & 31);
        return region.Exists(pointInRegion);
    }
}
