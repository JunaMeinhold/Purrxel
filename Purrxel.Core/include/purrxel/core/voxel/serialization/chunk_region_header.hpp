#pragma once

#include "purrxel/core/io/stream.hpp"
#include "purrxel/core/io/version.hpp"
#include "purrxel/pch/std.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    struct ChunkRegionHeader
    {
        static constexpr uint8_t MagicNumber[17] = { 0x54, 0x72, 0x61, 0x6e, 0x73, 0x56, 0x6f, 0x78, 0x65, 0x6c, 0x52, 0x65, 0x67, 0x69, 0x6f, 0x6e, 0x0};

        static constexpr IO::Version CurrentVersion{1, 0, 0, 0};
        static constexpr IO::Version MinVersion{1, 0, 0, 0};

        static constexpr int32_t Size = 4 + sizeof(MagicNumber);

        static void Write(Stream* stream)
        {
            stream->Write(MagicNumber, sizeof(MagicNumber));
            stream->WriteLittleEndian(CurrentVersion.ToUInt32());
        }

        static void Read(Stream* stream)
        {
            uint8_t buffer[sizeof(MagicNumber)];
            stream->Read(buffer, sizeof(buffer));
            if (std::memcmp(buffer, MagicNumber, sizeof(MagicNumber)) != 0)
            {
                throw std::runtime_error("Invalid magic number");
            }

            IO::Version version{stream->ReadLittleEndian<uint32_t>()};
            if (version > CurrentVersion || version < MinVersion)
            {
                throw std::runtime_error("ChunkRegionHeader: unsupported version.");
            }
        }
    };
}
