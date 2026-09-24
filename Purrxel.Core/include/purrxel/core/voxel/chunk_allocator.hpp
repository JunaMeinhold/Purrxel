#pragma once

#include "purrxel/core/voxel/chunk.hpp"
#include "purrxel/pch/std.hpp"
#include "purrxel/utils/semaphore.hpp"

namespace Purrxel::Core::Voxel
{
    class ChunkAllocator
    {
    public:
        static int32_t FreeThreshold;

        static int32_t AllocatedAmount() noexcept { return allocatedAmount; }

        static Chunk* New();
        static void Free(Chunk* chunk);
        static void Dispose();

    private:
        static std::vector<Chunk*> pool;
        static semaphore poolSemaphore;
        static int32_t allocatedAmount;
    };
}
