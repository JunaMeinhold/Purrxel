#pragma once

#include "purrxel/core/raw_list.hpp"
#include "purrxel/core/voxel/metadata/block_metadata.hpp"
#include "purrxel/core/voxel/metadata/block_metadata_collection_header.hpp"

namespace Purrxel::Core::Voxel::Metadata
{
    struct BlockMetadataCollection
    {
        RawList<BlockMetadata> Items;

        void Release()
        {
            for (auto& item : Items)
            {
                item.Release();
            }
            Items.Release();
        }

        void Serialize(Stream* stream) const
        {
            BlockMetadataCollectionHeader::Write(stream, Items.Count);
            for (const auto& item : Items)
            {
                item.Write(stream);
            }
        }

        int32_t SizeOf() const
        {
            int32_t size = BlockMetadataCollectionHeader::Size;
            for (const auto& item : Items)
            {
                size += item.Length + BlockMetadata::StaticSize;
            }
            return size;
        }

        void Deserialize(Stream* stream)
        {
            int32_t metadataCount = 0;
            BlockMetadataCollectionHeader::Read(stream, metadataCount);
            Items.SetCapacity(metadataCount);
            for (int32_t i = 0; i < metadataCount; i++)
            {
                Items.Add(BlockMetadata::ReadFrom(stream));
            }
        }
    };
}
