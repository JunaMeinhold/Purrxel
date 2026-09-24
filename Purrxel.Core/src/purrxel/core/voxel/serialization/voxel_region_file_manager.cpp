#include "purrxel/core/voxel/serialization/voxel_region_file_manager.hpp"

#include <filesystem>

namespace Purrxel::Core::Voxel::Serialization
{
    VoxelRegionFile* VoxelRegionFileManager::AcquireRegionStream(Point2 regionPos, const std::string& worldPath, bool write)
    {
        StreamMode mode = write ? StreamMode::Write : StreamMode::Read;

        {
            std::shared_lock<shared_fmutex> lock(mapMutex);
            auto it = idToRegions.find(regionPos);
            if (it != idToRegions.end())
            {
                VoxelRegionFile* region = it->second;
                region->Lock(mode);
                region->SetLastAccess(std::chrono::steady_clock::now());
                return region;
            }
        }

        std::unique_lock<shared_fmutex> lock(mapMutex);
        auto it = idToRegions.find(regionPos);
        if (it == idToRegions.end())
        {
            VoxelRegionFile* evicted = nullptr;
            TryEvict(evicted);

            std::string filename = worldPath + "/r." + std::to_string(regionPos.X) + "." + std::to_string(regionPos.Y) + ".vxr";

            bool exists = std::filesystem::exists(filename);
            if (!exists && !write)
            {
                return nullptr;
            }

            auto stream = FileStream::Open(filename.c_str(), exists ? "r+b" : "w+b");
            if (!stream)
            {
                return nullptr;
            }

            VoxelRegionFile* region = evicted;
            if (region == nullptr)
            {
                regions.push_back(std::make_unique<VoxelRegionFile>());
                region = regions.back().get();
            }

            region->Lock(mode);
            region->Reset(regionPos, stream, !exists, mode);

            idToRegions[regionPos] = region;
            return region;
        }
        else
        {
            VoxelRegionFile* region = it->second;
            region->Lock(mode);
            return region;
        }
    }

    void VoxelRegionFileManager::TryEvict(VoxelRegionFile*& old)
    {
        if (static_cast<int32_t>(idToRegions.size()) < MaxOpenFiles)
        {
            old = nullptr;
            return;
        }

        VoxelRegionFile* leastUsed = nullptr;
        for (auto& region : regions)
        {
            if (region->CurrentLockCount() == 1)
            {
                if (leastUsed == nullptr || region->LastAccess() < leastUsed->LastAccess())
                {
                    leastUsed = region.get();
                }
            }
        }

        if (leastUsed != nullptr)
        {
            leastUsed->Dispose(false);
            idToRegions.erase(leastUsed->Id());
        }

        old = leastUsed;
    }
}
