#pragma once

#include "purrxel/core/voxel/chunk.hpp"
#include "purrxel/core/voxel/serialization/block_run.hpp"
#include "purrxel/core/voxel/serialization/chunk_header.hpp"
#include "purrxel/core/voxel/serialization/height_map_run.hpp"
#include "purrxel/utils/vector.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    struct ChunkPreSerialized
    {
        Chunk* Chunk = nullptr;
        ChunkHeader Header;
        ChunkCompression CompressionMinY = ChunkCompression::RLE;
        vector<HeightMapRun> MinYRuns;
        ChunkCompression CompressionMaxY = ChunkCompression::RLE;
        vector<HeightMapRun> MaxYRuns;
        ChunkCompression Compression = ChunkCompression::RLE;
        vector<BlockRun> Runs;
        int64_t Length = 0;
    };
}
