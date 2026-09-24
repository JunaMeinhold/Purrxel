#pragma once

#include "purrxel/core/point2.hpp"
#include "purrxel/core/voxel/serialization/voxel_region_file.hpp"
#include "purrxel/pch/std.hpp"
#include "purrxel/utils/dense_map.hpp"
#include "purrxel/utils/fmutex.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    class VoxelRegionFileManager
    {
    public:
        int32_t MaxOpenFiles = 32;

        VoxelRegionFile* AcquireRegionStream(Point2 regionPos, const std::string& worldPath, bool write);

    private:
        void TryEvict(VoxelRegionFile*& old);

        shared_fmutex mapMutex;
        Purrxel::dense_map<Point2, VoxelRegionFile*> idToRegions;
        std::vector<std::unique_ptr<VoxelRegionFile>> regions;
    };
}
