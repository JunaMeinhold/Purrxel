#pragma once

#include "purrxel/core/io/stream.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    struct BlockRun
    {
        uint16_t Index = 0;
        uint16_t Count = 0;
        uint16_t Type = 0;

        void Read(Stream* stream)
        {
            Index = stream->ReadLittleEndian<uint16_t>();
            Count = stream->ReadLittleEndian<uint16_t>();
            Type = stream->ReadLittleEndian<uint16_t>();
        }

        void Write(Stream* stream) const
        {
            stream->WriteLittleEndian(Index);
            stream->WriteLittleEndian(Count);
            stream->WriteLittleEndian(Type);
        }
    };
}
