#include "purrxel/core/allocator.h"

#include <cstdlib>

namespace
{
    void* DefaultAlloc(size_t size) { return std::malloc(size); }
    void* DefaultReAlloc(void* oldPtr, size_t newSize) { return std::realloc(oldPtr, newSize); }
    void DefaultFree(void* ptr) { std::free(ptr); }

    AllocCallback g_allocCallback = DefaultAlloc;
    ReAllocCallback g_reallocCallback = DefaultReAlloc;
    FreeCallback g_freeCallback = DefaultFree;
}

void* Purrxel_Alloc(size_t size)
{
    return g_allocCallback(size);
}

void* Purrxel_ReAlloc(void* oldPtr, size_t newSize)
{
    return g_reallocCallback(oldPtr, newSize);
}

void Purrxel_Free(void* ptr)
{
    g_freeCallback(ptr);
}

void Purrxel_SetAllocatorCallbacks(AllocCallback allocCallback, ReAllocCallback reallocCallback, FreeCallback freeCallback)
{
    g_allocCallback = allocCallback ? allocCallback : DefaultAlloc;
    g_reallocCallback = reallocCallback ? reallocCallback : DefaultReAlloc;
    g_freeCallback = freeCallback ? freeCallback : DefaultFree;
}

void Purrxel_GetAllocatorCallbacks(AllocCallback* allocCallback, ReAllocCallback* reallocCallback, FreeCallback* freeCallback)
{
    if (allocCallback) *allocCallback = g_allocCallback;
    if (reallocCallback) *reallocCallback = g_reallocCallback;
    if (freeCallback) *freeCallback = g_freeCallback;
}
