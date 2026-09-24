#pragma once

#include "purrxel/core/io/stream.hpp"
#include "purrxel/core/voxel/chunk.hpp"
#include "purrxel/core/voxel/serialization/chunk_pre_serialized.hpp"

/*
Pattern for ImHex:

struct BlockMetadata
{
    u32 type;
    u32 length;
    u8 data[length];
};

struct BlockMetadataCollection
{
    u32 version;
    u32 count;
    BlockMetadata metadata[count];
};

struct BlockRun
{
    u16 type;
    u16 index;
    u16 count;
};

struct HeightMapRun
{
    u8 value;
    u8 count;
    u8 index;
};

struct HeightMap
{
    u16 compression;
    if (compression == 0)
    {
        u8 data[256];
    }
    else
    {
        u8 runCount;
        HeightMapRun runs[runCount];
    }
};

struct Chunk
{
    u16 blockCount;
    u64 length;

    if (blockCount > 0)
    {
        HeightMap minY;
        HeightMap maxY;

        BlockMetadataCollection collection;

        u16 compression;
        if (compression == 0)
        {
            u16 blocks[blockCount];
        }
        else
        {
            u16 runCount;
            BlockRun runs[runCount];
        }
    }
};

struct ChunkSegment
{
    u32 chunkCount;
    Chunk chunks[chunkCount];
};

ChunkSegment segment @ 0x0;

 */

namespace Purrxel::Core::Voxel::Serialization
{
    struct ChunkSerializer
    {
        static ChunkPreSerialized PreSerialize(Chunk* chunk);

        static void Serialize(Chunk* chunk, Stream* stream);
        static void Serialize(Chunk* chunk, Stream* stream, const ChunkPreSerialized& serialized);

        static void Deserialize(Chunk* chunk, Stream* stream);

    private:
        static int32_t EncodeHeightMap(const uint8_t* values, int32_t length, vector<HeightMapRun>& output, ChunkCompression& compression, int32_t zeroValue);

        static void WriteHeightMap(Stream* stream, ChunkCompression compression, const uint8_t* raw, const vector<HeightMapRun>& runs);

        static void ReadHeightMap(Stream* stream, uint8_t* output);
    };
}
