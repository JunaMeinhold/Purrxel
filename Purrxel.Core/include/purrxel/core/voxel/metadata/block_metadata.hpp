#pragma once

#include "purrxel/core/allocator.h"
#include "purrxel/core/io/stream.hpp"
#include "purrxel/core/voxel/metadata/block_metadata_type.hpp"
#include "purrxel/pch/std.hpp"

namespace Purrxel::Core::Voxel::Metadata
{
    struct BlockMetadata
    {
        static constexpr int32_t StaticSize = 8;

        BlockMetadataType Type = BlockMetadataType::None;
        uint8_t* Data = nullptr;
        int32_t Length = 0;
        int32_t Capacity = 0;

        void SetCapacity(int32_t capacity)
        {
            if (Capacity == capacity) return;
            Data = static_cast<uint8_t*>(Data == nullptr ? Purrxel_Alloc(static_cast<size_t>(capacity)) : Purrxel_ReAlloc(Data, static_cast<size_t>(capacity)));
            Capacity = capacity;
        }

        void EnsureCapacity(int32_t size)
        {
            if (Data == nullptr || Capacity < size)
            {
                SetCapacity(std::max(Capacity * 2, size));
            }
        }

        void Resize(int32_t size)
        {
            EnsureCapacity(size);
            Length = size;
        }

        void Release()
        {
            if (Data != nullptr)
            {
                Purrxel_Free(Data);
                Data = nullptr;
                Length = 0;
                Capacity = 0;
            }
        }

        void Write(Stream* stream) const
        {
            stream->WriteLittleEndian(Type);
            stream->WriteLittleEndian(static_cast<int32_t>(Length));
            if (Length > 0)
            {
                stream->Write(Data, static_cast<size_t>(Length));
            }
        }

        void Read(Stream* stream)
        {
            Type = stream->ReadLittleEndian<BlockMetadataType>();
            int32_t length = stream->ReadLittleEndian<int32_t>();
            Capacity = Length = length;
            if (Length > 0)
            {
                EnsureCapacity(Length);
                stream->Read(Data, static_cast<size_t>(Length));
            }
        }

        static BlockMetadata ReadFrom(Stream* stream)
        {
            BlockMetadata metadata;
            metadata.Read(stream);
            return metadata;
        }
    };
}
