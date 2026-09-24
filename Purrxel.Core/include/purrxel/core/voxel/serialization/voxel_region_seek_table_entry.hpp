#pragma once

#include "purrxel/core/io/stream.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    struct VoxelRegionSeekTableEntry
    {
        int64_t Position = 0;
        int64_t Length = 0;

        void Read(Stream* stream)
        {
            Position = stream->ReadLittleEndian<int64_t>();
            Length = stream->ReadLittleEndian<int64_t>();
        }

        void Write(Stream* stream) const
        {
            stream->WriteLittleEndian(Position);
            stream->WriteLittleEndian(Length);
        }
    };
}
