#ifndef PURRXEL_CORE_ALLOCATOR_H
#define PURRXEL_CORE_ALLOCATOR_H

#include "purrxel/core/config.h"

C_API_BEGIN

typedef void* (*AllocCallback)(size_t size);
typedef void* (*ReAllocCallback)(void* oldPtr, size_t newSize);
typedef void (*FreeCallback)(void* ptr);

PURRXEL_API void* Purrxel_Alloc(size_t size);

PURRXEL_API void* Purrxel_ReAlloc(void* oldPtr, size_t newSize);

PURRXEL_API void Purrxel_Free(void* ptr);

PURRXEL_API void Purrxel_SetAllocatorCallbacks(AllocCallback allocCallback, ReAllocCallback reallocCallback, FreeCallback freeCallback);

PURRXEL_API void Purrxel_GetAllocatorCallbacks(AllocCallback* allocCallback, ReAllocCallback* reallocCallback, FreeCallback* freeCallback);

C_API_END

#ifdef __cplusplus

template <typename T>
T* Purrxel_Alloc()
{
    return (T*)Purrxel_Alloc(sizeof(T));
}

template <typename T>
T* Purrxel_Alloc(size_t count)
{
    return (T*)Purrxel_Alloc(sizeof(T) * count);
}

#endif

#endif
