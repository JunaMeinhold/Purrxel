#pragma once

#include "purrxel/core/point2.hpp"
#include "purrxel/core/point3.hpp"
#include "purrxel/core/voxel/chunk.hpp"

namespace Purrxel::Core::Voxel
{
    inline int32_t MapToIndex(const Point2& vector)
    {
        return vector.X + (vector.Y << 4);
    }

    inline int32_t MapToIndex(const Point3& vector)
    {
        return (vector.Z << Chunk::CHUNK_SHIFT_Z) + (vector.X << Chunk::CHUNK_SHIFT_Y) + vector.Y;
    }

    inline int32_t MapToIndex(int32_t x, int32_t y, int32_t z)
    {
        return (z << Chunk::CHUNK_SHIFT_Z) + (x << Chunk::CHUNK_SHIFT_Y) + y;
    }
}
