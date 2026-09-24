#pragma once

#include "purrxel/core/io/stream.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    struct HeightMapRun
    {
        uint8_t Index = 0;
        uint8_t Count = 0;
        uint8_t Value = 0;

        void Read(Stream* stream)
        {
            Index = stream->ReadByte();
            Count = stream->ReadByte();
            Value = stream->ReadByte();
        }

        void Write(Stream* stream) const
        {
            stream->Write(Index);
            stream->Write(Count);
            stream->Write(Value);
        }
    };
}
