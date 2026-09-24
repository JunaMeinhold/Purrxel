#pragma once

#include "purrxel/pch/std.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    struct FreeListEntry
    {
        int64_t Start = 0;
        int64_t End = 0;

        constexpr FreeListEntry() = default;
        constexpr FreeListEntry(int64_t start, int64_t end) : Start(start), End(end) {}

        constexpr int64_t Length() const noexcept { return End - Start; }
    };
}
