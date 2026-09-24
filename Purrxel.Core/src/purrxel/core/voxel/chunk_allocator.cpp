#include "purrxel/core/voxel/chunk_allocator.hpp"

#include "purrxel/core/allocator.h"

#include <new>

namespace Purrxel::Core::Voxel
{
    int32_t ChunkAllocator::FreeThreshold = 64;
    std::vector<Chunk*> ChunkAllocator::pool;
    semaphore ChunkAllocator::poolSemaphore{1};
    int32_t ChunkAllocator::allocatedAmount = 0;

    Chunk* ChunkAllocator::New()
    {
        poolSemaphore.acquire();

        allocatedAmount++;
        Chunk* result;
        if (!pool.empty())
        {
            result = pool.back();
            pool.pop_back();
            result->~Chunk();
            new (result) Chunk();
            poolSemaphore.release();
            return result;
        }

        result = Purrxel_Alloc<Chunk>();
        new (result) Chunk();

        poolSemaphore.release();
        return result;
    }

    void ChunkAllocator::Free(Chunk* chunk)
    {
        poolSemaphore.acquire();

        allocatedAmount--;
        chunk->FreeMemory();
        if (static_cast<int32_t>(pool.size()) < FreeThreshold)
        {
            pool.push_back(chunk);
        }
        else
        {
            chunk->~Chunk();
            Purrxel_Free(chunk);
        }

        poolSemaphore.release();
    }

    void ChunkAllocator::Dispose()
    {
        while (!pool.empty())
        {
            Chunk* chunk = pool.back();
            pool.pop_back();
            chunk->FreeMemory();
            chunk->~Chunk();
            Purrxel_Free(chunk);
        }
    }
}
