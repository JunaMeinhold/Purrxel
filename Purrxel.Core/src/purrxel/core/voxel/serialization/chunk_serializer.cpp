#include "purrxel/core/voxel/serialization/chunk_serializer.hpp"

#include "purrxel/pch/std.hpp"

namespace Purrxel::Core::Voxel::Serialization
{
    ChunkPreSerialized ChunkSerializer::PreSerialize(Chunk* chunk)
    {
        int64_t size = ChunkHeader::Size;

        ChunkPreSerialized result;

        int32_t runsWritten = 0;
        if (chunk->InMemory())
        {
            size += EncodeHeightMap(chunk->MinY, Chunk::CHUNK_SIZE_SQUARED, result.MinYRuns, result.CompressionMinY, Chunk::CHUNK_SIZE);
            size += EncodeHeightMap(chunk->MaxY, Chunk::CHUNK_SIZE_SQUARED, result.MaxYRuns, result.CompressionMaxY, 0);

            size += chunk->BlockMetadata.SizeOf();

            BlockRun run{};
            bool newRun = true;

            for (int32_t z = 0; z < Chunk::CHUNK_SIZE; z++)
            {
                int32_t zShifted = z << 8;
                int32_t heightMapAccess = z << 4;
                for (int32_t x = 0; x < Chunk::CHUNK_SIZE; x++)
                {
                    int32_t y = chunk->MinY[heightMapAccess];
                    int32_t yMax = chunk->MaxY[heightMapAccess];

                    heightMapAccess++;

                    int32_t access = zShifted + (x << 4) + y;
                    const Block* voxels = chunk->Data + access;

                    for (; y < yMax; y++, access++, voxels++)
                    {
                        Block b = *voxels;

                        if (b.Type == run.Type && !newRun)
                        {
                            run.Count++;
                            continue;
                        }

                        if (!newRun)
                        {
                            runsWritten++;
                            result.Runs.push_back(run);
                            if (runsWritten > ChunkHeader::RLEBreakevenPoint)
                            {
                                goto end;
                            }
                        }

                        newRun = true; // prevent writing a run when hitting break;
                        for (; y < yMax && voxels->Type == Chunk::EMPTY; y++, access++, voxels++);
                        if (y == yMax) break;

                        b = *voxels;
                        run.Type = b.Type;
                        run.Count = 0;
                        run.Index = static_cast<uint16_t>(access);
                        newRun = false;
                    }
                }
            }

            if (!newRun)
            {
                runsWritten++;
                result.Runs.push_back(run);
                if (runsWritten > ChunkHeader::RLEBreakevenPoint)
                {
                    goto end;
                }
            }
        }

    end:

        ChunkHeader header;
        if (static_cast<int32_t>(result.Runs.size()) > ChunkHeader::RLEBreakevenPoint)
        {
            result.Compression = ChunkCompression::Raw;
            size += static_cast<int64_t>(Chunk::CHUNK_SIZE_CUBED) * sizeof(uint16_t);
            result.Runs.clear();
        }
        else
        {
            result.Compression = ChunkCompression::RLE;
            size += static_cast<int64_t>(result.Runs.size()) * sizeof(BlockRun);
        }

        header.BlockCount = chunk->BlockCount;
        header.Length = size - ChunkHeader::Size;

        result.Chunk = chunk;
        result.Header = header;
        result.Length = size;

        return result;
    }

    int32_t ChunkSerializer::EncodeHeightMap(const uint8_t* values, int32_t length, vector<HeightMapRun>& output, ChunkCompression& compression, int32_t zeroValue)
    {
        HeightMapRun run{};
        bool newRun = true;
        int32_t runsWritten = 0;
        int32_t i = 0;
        for (; i < length; i++, values++)
        {
            uint8_t value = *values;
            if (value == run.Value && !newRun)
            {
                run.Count++;
                continue;
            }

            if (!newRun)
            {
                runsWritten++;
                output.push_back(run);
                if (runsWritten > ChunkHeader::RLEHeightMapBreakevenPoint)
                {
                    goto end;
                }
            }

            newRun = true; // prevent writing a run when hitting break;
            for (; i < length && *values == zeroValue; i++, values++);
            if (i == length) break;

            value = *values;
            run.Value = value;
            run.Count = 0;
            run.Index = static_cast<uint8_t>(i);
            newRun = false;
        }

        if (!newRun)
        {
            runsWritten++;
            output.push_back(run);
        }

    end:

        if (runsWritten > ChunkHeader::RLEHeightMapBreakevenPoint)
        {
            compression = ChunkCompression::Raw;
            output.clear();
            return Chunk::CHUNK_SIZE_SQUARED;
        }
        else
        {
            compression = ChunkCompression::RLE;
            return runsWritten * static_cast<int32_t>(sizeof(HeightMapRun));
        }
    }

    void ChunkSerializer::Serialize(Chunk* chunk, Stream* stream)
    {
        Serialize(chunk, stream, PreSerialize(chunk));
    }

    void ChunkSerializer::Serialize(Chunk* chunk, Stream* stream, const ChunkPreSerialized& serialized)
    {
        serialized.Header.Write(stream);
        if (chunk->InMemory())
        {
            WriteHeightMap(stream, serialized.CompressionMinY, chunk->MinY, serialized.MinYRuns);
            WriteHeightMap(stream, serialized.CompressionMaxY, chunk->MaxY, serialized.MaxYRuns);

            chunk->BlockMetadata.Serialize(stream);

            stream->WriteLittleEndian(static_cast<uint16_t>(serialized.Compression));
            switch (serialized.Compression)
            {
                case ChunkCompression::Raw:
                    stream->Write(chunk->Data, sizeof(Block) * Chunk::CHUNK_SIZE_CUBED);
                    break;

                case ChunkCompression::RLE:
                    stream->WriteLittleEndian(static_cast<uint16_t>(serialized.Runs.size()));
                    for (const auto& run : serialized.Runs)
                    {
                        run.Write(stream);
                    }
                    break;
            }
        }
    }

    void ChunkSerializer::WriteHeightMap(Stream* stream, ChunkCompression compression, const uint8_t* raw, const vector<HeightMapRun>& runs)
    {
        stream->WriteLittleEndian(static_cast<uint16_t>(compression));
        switch (compression)
        {
            case ChunkCompression::Raw:
                stream->Write(raw, Chunk::CHUNK_SIZE_SQUARED);
                break;

            case ChunkCompression::RLE:
                stream->Write(static_cast<uint8_t>(runs.size()));
                for (const auto& run : runs)
                {
                    run.Write(stream);
                }
                break;
        }
    }

    void ChunkSerializer::Deserialize(Chunk* chunk, Stream* stream)
    {
        ChunkHeader header = ChunkHeader::ReadFrom(stream);

        if (header.BlockCount == 0)
        {
            return;
        }

        if (!chunk->InMemory())
        {
            chunk->Allocate(false);
        }

        std::memset(chunk->Data, 0, sizeof(Block) * Chunk::CHUNK_SIZE_CUBED);
        std::memset(chunk->MaxY, 0, Chunk::CHUNK_SIZE_SQUARED);
        std::memset(chunk->MinY, Chunk::CHUNK_SIZE, Chunk::CHUNK_SIZE_SQUARED);

        ReadHeightMap(stream, chunk->MinY);
        ReadHeightMap(stream, chunk->MaxY);

        chunk->BlockMetadata.Deserialize(stream);
        chunk->BlockCount = header.BlockCount;

        ChunkCompression compression = static_cast<ChunkCompression>(stream->ReadLittleEndian<uint16_t>());
        switch (compression)
        {
            case ChunkCompression::Raw:
                stream->Read(chunk->Data, sizeof(Block) * Chunk::CHUNK_SIZE_CUBED);
                break;

            case ChunkCompression::RLE:
            {
                uint16_t runCount = stream->ReadLittleEndian<uint16_t>();
                if (runCount > ChunkHeader::RLEBreakevenPoint)
                {
                    throw std::runtime_error("Corrupt chunk data, RLE data too long.");
                }

                BlockRun run{};
                for (int32_t i = 0; i < runCount; i++)
                {
                    run.Read(stream);

                    if (run.Count == Chunk::CHUNK_SIZE_CUBED)
                    {
                        std::fill_n(chunk->Data, Chunk::CHUNK_SIZE_CUBED, Block{run.Type});
                        break;
                    }

                    int32_t access = run.Index;
                    int32_t remaining = run.Count + 1;
                    int32_t z = (access >> 8) & 0xF;
                    int32_t x = (access >> 4) & 0xF;
                    int32_t y = access & 0xF;

                    if (access + remaining > Chunk::CHUNK_SIZE_CUBED)
                    {
                        throw std::out_of_range("Corrupt chunk data, RLE Decoding exceeded chunk boundaries.");
                    }

                    int32_t heightMapAccess = (z << 4) + x;

                    Block block{run.Type};

                    int32_t minY = chunk->MinY[heightMapAccess];
                    int32_t maxY = chunk->MaxY[heightMapAccess];

                    while (remaining > 0)
                    {
                        int32_t toSet = std::min(remaining, maxY - y);
                        std::fill_n(chunk->Data + access, toSet, block);
                        remaining -= toSet;

                        if (remaining <= 0)
                        {
                            break;
                        }

                        do
                        {
                            x++;
                            if (x >= Chunk::CHUNK_SIZE)
                            {
                                x = 0;
                                z++;
                                heightMapAccess = z << 4;
                                if (z == Chunk::CHUNK_SIZE)
                                {
                                    throw std::out_of_range("Corrupt chunk data, RLE Decoding exceeded chunk boundaries.");
                                }
                            }
                            else
                            {
                                heightMapAccess++;
                            }

                            y = chunk->MinY[heightMapAccess];
                            maxY = chunk->MaxY[heightMapAccess];
                            if (y > maxY) continue;
                            access = (z << 8) + (x << 4) + y;
                        } while (y > maxY);
                    }
                }
                break;
            }

            default:
                throw std::runtime_error("Corrupt chunk data, unknown compression mode.");
        }
    }

    void ChunkSerializer::ReadHeightMap(Stream* stream, uint8_t* output)
    {
        ChunkCompression compression = static_cast<ChunkCompression>(stream->ReadLittleEndian<uint16_t>());
        switch (compression)
        {
            case ChunkCompression::Raw:
                stream->Read(output, Chunk::CHUNK_SIZE_SQUARED);
                break;

            case ChunkCompression::RLE:
            {
                int32_t runCount = stream->ReadByte();
                if (runCount > ChunkHeader::RLEHeightMapBreakevenPoint)
                {
                    throw std::runtime_error("Corrupt height-map data, RLE data too long.");
                }

                HeightMapRun run{};
                for (int32_t i = 0; i < runCount; i++)
                {
                    run.Read(stream);

                    if (run.Index + run.Count + 1 > Chunk::CHUNK_SIZE_CUBED)
                    {
                        throw std::out_of_range("Corrupt height-map data, RLE Decoding exceeded chunk boundaries.");
                    }

                    std::fill_n(output + run.Index, run.Count + 1, run.Value); // shift count by one to remap from 0..255 to 1..256
                }
                break;
            }

            default:
                throw std::runtime_error("Corrupt height-map data, unknown compression mode.");
        }
    }
}
