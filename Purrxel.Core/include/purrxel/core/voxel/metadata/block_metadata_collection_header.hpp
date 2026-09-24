#pragma once

#include "purrxel/pch/std.hpp"

#include "purrxel/core/io/stream.hpp"
#include "purrxel/core/io/version.hpp"

namespace Purrxel::Core::Voxel::Metadata
{
    struct BlockMetadataCollectionHeader
    {
        static constexpr IO::Version CurrentVersion{1, 0, 0, 0};
        static constexpr IO::Version MinVersion{1, 0, 0, 0};
        static constexpr int32_t Size = 8;

        static void Write(Stream* stream, int32_t metadataCount)
        {
            stream->WriteLittleEndian(CurrentVersion.ToUInt32());
            stream->WriteLittleEndian(static_cast<int32_t>(metadataCount));
        }

        static void Read(Stream* stream, int32_t& metadataCount)
        {
            IO::Version version{stream->ReadLittleEndian<uint32_t>()};
            if (version > CurrentVersion || version < MinVersion)
            {
                throw std::runtime_error("BlockMetadataCollectionHeader: unsupported version.");
            }
            metadataCount = stream->ReadLittleEndian<int32_t>();
        }
    };
}
