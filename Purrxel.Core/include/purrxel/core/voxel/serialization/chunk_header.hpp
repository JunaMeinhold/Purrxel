#pragma once

#include "purrxel/core/io/stream.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    enum class ChunkCompression : uint16_t
    {
        Raw = 0,
        RLE = 1,
    };

    struct ChunkHeader
    {
        uint16_t BlockCount = 0;
        int64_t Length = 0;

        static constexpr int32_t Size = 10;

        static constexpr int32_t RLEBreakevenPoint = 1365;
        static constexpr int32_t RLEHeightMapBreakevenPoint = 85;

        void Write(Stream* stream) const
        {
            stream->WriteLittleEndian(BlockCount);
            stream->WriteLittleEndian(Length);
        }

        void Read(Stream* stream)
        {
            BlockCount = stream->ReadLittleEndian<uint16_t>();
            Length = stream->ReadLittleEndian<int64_t>();
        }

        static ChunkHeader ReadFrom(Stream* stream)
        {
            ChunkHeader header;
            header.Read(stream);
            return header;
        }
    };
}
