#pragma once

#include "purrxel/core/allocator.h"
#include "purrxel/pch/std.hpp"

namespace Purrxel::Core
{
    template <typename T>
    struct RawList
    {
        T* Data = nullptr;
        int32_t Count = 0;
        int32_t Capacity = 0;

        void SetCapacity(int32_t capacity)
        {
            if (Capacity == capacity) return;
            Data = static_cast<T*>(Data == nullptr ? Purrxel_Alloc(sizeof(T) * static_cast<size_t>(capacity)) : Purrxel_ReAlloc(Data, sizeof(T) * static_cast<size_t>(capacity)));
            Capacity = capacity;
        }

        void EnsureCapacity(int32_t size)
        {
            if (Data == nullptr || Capacity < size)
            {
                SetCapacity(std::max(Capacity * 2, size));
            }
        }

        void Add(const T& value)
        {
            EnsureCapacity(Count + 1);
            Data[Count++] = value;
        }

        void Clear() noexcept { Count = 0; }

        void Release()
        {
            if (Data != nullptr)
            {
                Purrxel_Free(Data);
                Data = nullptr;
                Count = 0;
                Capacity = 0;
            }
        }

        T& operator[](int32_t index) noexcept { return Data[index]; }
        const T& operator[](int32_t index) const noexcept { return Data[index]; }

        T* begin() noexcept { return Data; }
        T* end() noexcept { return Data + Count; }
        const T* begin() const noexcept { return Data; }
        const T* end() const noexcept { return Data + Count; }
    };
}
