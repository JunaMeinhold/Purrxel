namespace VoxelEngine.Voxel.Serialization
{
    using Hexa.NET.Utilities;
    using System.IO;
    using VoxelEngine.IO;

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

    public readonly unsafe ref struct ChunkVoxelEnumerable
    {
        private readonly Chunk* chunk;

        public ChunkVoxelEnumerable(Chunk* chunk)
        {
            this.chunk = chunk;
        }

        public Enumerator GetEnumerator() => new(chunk);

        public ref struct Enumerator
        {
            private readonly Chunk* chunk;
            private byte x;
            private byte y;
            private byte yMax;
            private byte z;

            private int access;
            private Block* voxel;

            public Enumerator(Chunk* chunk)
            {
                this.chunk = chunk;

                x = 0;
                y = 0;
                yMax = 0;
                z = 0;

                access = 0;
                voxel = null;

                Current = default;
            }

            public VoxelRef Current { get; private set; }

            public bool MoveNext()
            {
                // Continue current column first.
                if (voxel != null)
                {
                    y++;
                    access++;
                    voxel++;
                }

                while (true)
                {
                    // Scan current column.
                    while (y < yMax)
                    {
                        if (voxel->Type != Chunk.EMPTY)
                        {
                            Current = new(voxel, (ushort)access, x, y, z);

                            return true;
                        }

                        y++;
                        access++;
                        voxel++;
                    }

                    // Move to next column.
                    x++;

                    if (x >= Chunk.CHUNK_SIZE)
                    {
                        x = 0;
                        z++;

                        if (z >= Chunk.CHUNK_SIZE)
                            return false;
                    }

                    int heightMapAccess = (z << 4) + x;

                    y = chunk->MinY[heightMapAccess];
                    yMax = chunk->MaxY[heightMapAccess];

                    if (y >= yMax)
                    {
                        voxel = null;
                        continue;
                    }

                    access = (z << 8) + (x << 4) + y;

                    voxel = chunk->Data + access;
                }
            }
        }
    }

    public unsafe struct VoxelRef
    {
        public Block* Block;
        public ushort Index;
        public byte X;
        public byte Y;
        public byte Z;

        public VoxelRef(Block* ptr, ushort index, byte x, byte y, byte z)
        {
            Block = ptr;
            Index = index;

            X = x;
            Y = y;
            Z = z;
        }

        public ref Block Value => ref *Block;

        public static implicit operator Block*(in VoxelRef voxelRef) => voxelRef.Block;
    }

    public static class ChunkSerializer
    {
        public static unsafe ChunkPreSerialized PreSerialize(Chunk* chunk)
        {
            long size = ChunkHeader.Size;

            ChunkPreSerialized result = default;
            var runs = result.Runs = [];

            int runsWritten = 0;
            if (chunk->InMemory)
            {
                size += EncodeHeightMap(chunk->MinY, Chunk.CHUNK_SIZE_SQUARED, &result.MinYRuns, &result.CompressionMinY, Chunk.CHUNK_SIZE);
                size += EncodeHeightMap(chunk->MaxY, Chunk.CHUNK_SIZE_SQUARED, &result.MaxYRuns, &result.CompressionMaxY, 0);

                size += chunk->BlockMetadata.SizeOf();

                BlockRun run = default;
                bool newRun = true;

                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    int zShifted = z << 8;
                    int heightMapAccess = z << 4;
                    for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                    {
                        int y = chunk->MinY[heightMapAccess];
                        int yMax = chunk->MaxY[heightMapAccess];

                        heightMapAccess++;

                        int access = zShifted + (x << 4) + y;
                        Block* voxels = chunk->Data + access;

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
                                runs.Add(run);
                                if (runsWritten > ChunkHeader.RLEBreakevenPoint)
                                {
                                    goto end;
                                }
                            }

                            newRun = true; // prevent writing a run when hitting break;
                            for (; y < yMax && voxels->Type == Chunk.EMPTY; y++, access++, voxels++) ;
                            if (y == yMax) break;

                            b = *voxels;
                            run.Type = b.Type;
                            run.Count = 0;
                            run.Index = (ushort)access;
                            newRun = false;
                        }
                    }
                }

                if (!newRun)
                {
                    runsWritten++;
                    runs.Add(run);
                    if (runsWritten > ChunkHeader.RLEBreakevenPoint)
                    {
                        goto end;
                    }
                }
            }

        end:

            ChunkHeader header = default;
            if (runs.Count > ChunkHeader.RLEBreakevenPoint)
            {
                result.Compression = ChunkCompression.Raw;
                size += Chunk.CHUNK_SIZE_CUBED * sizeof(ushort);
                runs.Release();
            }
            else
            {
                result.Compression = ChunkCompression.RLE;
                size += runs.Count * sizeof(BlockRun);
            }

            header.BlockCount = chunk->BlockCount;
            header.Length = size - ChunkHeader.Size;

            result.Chunk = chunk;
            result.Header = header;
            result.Runs = runs;
            result.Length = size;

            return result;
        }

        private static unsafe int EncodeHeightMap(byte* values, int length, UnsafeList<HeightMapRun>* output, ChunkCompression* compression, int zeroValue)
        {
            HeightMapRun run = default;
            bool newRun = true;
            int runsWritten = 0;
            for (int i = 0; i < length; i++, values++)
            {
                var value = *values;
                if (value == run.Value && !newRun)
                {
                    run.Count++;
                    continue;
                }

                if (!newRun)
                {
                    runsWritten++;
                    output->Add(run);
                    if (runsWritten > ChunkHeader.RLEHeightMapBreakevenPoint)
                    {
                        goto end;
                    }
                }

                newRun = true; // prevent writing a run when hitting break;
                for (; i < length && *values == zeroValue; i++, values++) ;
                if (i == length) break;

                value = *values;
                run.Value = value;
                run.Count = 0;
                run.Index = (byte)i;
                newRun = false;
            }

            if (!newRun)
            {
                runsWritten++;
                output->Add(run);
            }

        end:

            if (runsWritten > ChunkHeader.RLEHeightMapBreakevenPoint)
            {
                *compression = ChunkCompression.Raw;
                output->Release();
                return Chunk.CHUNK_SIZE_SQUARED;
            }
            else
            {
                *compression = ChunkCompression.RLE;
                return runsWritten * sizeof(HeightMapRun);
            }
        }

        public static unsafe void Serialize(Chunk* chunk, Stream stream)
        {
            Serialize(chunk, stream, PreSerialize(chunk));
        }

        public static unsafe void Serialize(Chunk* chunk, Stream stream, ChunkPreSerialized serialized)
        {
            var header = serialized.Header;

            header.Write(stream);
            if (chunk->InMemory)
            {
                WriteHeightMap(stream, serialized.CompressionMinY, chunk->MinY, serialized.MinYRuns);
                WriteHeightMap(stream, serialized.CompressionMaxY, chunk->MaxY, serialized.MaxYRuns);

                chunk->BlockMetadata.Serialize(stream);

                stream.WriteUInt16((ushort)serialized.Compression);
                switch (serialized.Compression)
                {
                    case ChunkCompression.Raw:
                        Block* data = chunk->Data;
                        Write(stream, data, Chunk.CHUNK_SIZE_CUBED);
                        break;

                    case ChunkCompression.RLE:
                        stream.WriteUInt16((ushort)serialized.Runs.Count);
                        Write(stream, serialized.Runs.Data, serialized.Runs.Count);
                        break;
                }
            }
            ((ChunkPreSerialized)serialized).Release();
        }

        private static unsafe void WriteHeightMap(Stream stream, ChunkCompression compression, byte* raw, UnsafeList<HeightMapRun> runs)
        {
            stream.WriteUInt16((ushort)compression);
            switch (compression)
            {
                case ChunkCompression.Raw:
                    stream.Write(new ReadOnlySpan<byte>(raw, Chunk.CHUNK_SIZE_SQUARED));
                    break;

                case ChunkCompression.RLE:
                    stream.WriteByte((byte)runs.Count);
                    Write(stream, runs.Data, runs.Count);
                    break;
            }
        }

        private static unsafe void Write<T>(Stream stream, T* data, int length) where T : unmanaged, IBinarySerializable
        {
            if (BitConverter.IsLittleEndian)
            {
                stream.Write(new Span<byte>(data, sizeof(T) * length));
            }
            else
            {
                const int bufferSize = 8192;
                int maxItems = bufferSize / sizeof(T);
                int maxBytes = bufferSize - bufferSize % sizeof(T);

                Span<byte> buffer = stackalloc byte[maxBytes];
                int offset = 0;
                int itemCount = 0;
                T* end = data + length;
                while (data != end)
                {
                    offset += data->Write(buffer[offset..]);
                    itemCount++;
                    if (itemCount >= maxItems)
                    {
                        stream.Write(buffer);
                        offset = 0; itemCount = 0;
                    }
                    data++;
                }
                if (offset > 0)
                {
                    stream.Write(buffer[..offset]);
                }
            }
        }

        private static unsafe void Read<T>(Stream stream, T* data, int length) where T : unmanaged, IBinarySerializable
        {
            if (BitConverter.IsLittleEndian)
            {
                stream.ReadExactly(new Span<byte>(data, sizeof(T) * length));
            }
            else
            {
                const int bufferSize = 8192;
                int remainingBytes = sizeof(T) * length;
                int maxBytes = bufferSize - bufferSize % sizeof(T);

                Span<byte> buffer = stackalloc byte[maxBytes];

                while (remainingBytes > 0)
                {
                    int toRead = Math.Min(maxBytes, remainingBytes);
                    stream.ReadExactly(buffer[..toRead]);
                    remainingBytes -= toRead;
                    int offset = 0;
                    while (offset < toRead)
                    {
                        offset += data->Read(buffer[offset..]);
                        data++;
                    }
                }
            }
        }

        public static unsafe void Deserialize(Chunk* chunk, Stream stream)
        {
            ChunkHeader header = ChunkHeader.ReadFrom(stream);

            if (header.BlockCount == 0)
            {
                return; // skip reading/allocating.
            }

            if (!chunk->InMemory)
            {
                chunk->Allocate(false);
            }

            ZeroMemoryT(chunk->Data, Chunk.CHUNK_SIZE_CUBED);
            ZeroMemoryT(chunk->MaxY, Chunk.CHUNK_SIZE_SQUARED);
            Memset(chunk->MinY, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE_SQUARED);

            ReadHeightMap(stream, chunk->MinY);
            ReadHeightMap(stream, chunk->MaxY);

            chunk->BlockMetadata.Deserialize(stream);
            chunk->BlockCount = header.BlockCount;

            ChunkCompression compression = (ChunkCompression)stream.ReadUInt16();
            switch (compression)
            {
                case ChunkCompression.Raw:
                    Read(stream, chunk->Data, Chunk.CHUNK_SIZE_CUBED);
                    break;

                case ChunkCompression.RLE:
                    ushort runCount = stream.ReadUInt16();
                    if (runCount > ChunkHeader.RLEBreakevenPoint) throw new FormatException("Corrupt chunk data, RLE data too long.");
                    BlockRun run = default;
                    for (int i = 0; i < runCount; i++)
                    {
                        run.Read(stream);

                        if (run.Count == Chunk.CHUNK_SIZE_CUBED)
                        {
                            MemsetT(chunk->Data, new Block(run.Type), Chunk.CHUNK_SIZE_CUBED);
                            break;
                        }

                        int access = run.Index;
                        int remaining = run.Count + 1;
                        int z = (access >> 8) & 0xF;
                        int x = (access >> 4) & 0xF;
                        int y = access & 0xF;

                        if (access + remaining > Chunk.CHUNK_SIZE_CUBED) throw new IndexOutOfRangeException("Corrupt chunk data, RLE Decoding exceeded chunk boundaries.");

                        int heightMapAccess = (z << 4) + x;

                        Block block = new(run.Type);

                        int minY = chunk->MinY[heightMapAccess];
                        int maxY = chunk->MaxY[heightMapAccess];

                        while (remaining > 0)
                        {
                            int toSet = Math.Min(remaining, maxY - y);
                            MemsetT(chunk->Data + access, block, toSet);
                            remaining -= toSet;

                            if (remaining <= 0)
                            {
                                break;
                            }

                            do
                            {
                                x++;
                                if (x >= Chunk.CHUNK_SIZE)
                                {
                                    x = 0;
                                    z++;
                                    heightMapAccess = z << 4;
                                    if (z == Chunk.CHUNK_SIZE) throw new IndexOutOfRangeException("Corrupt chunk data, RLE Decoding exceeded chunk boundaries.");
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

                default:
                    throw new FormatException("Corrupt chunk data, unknown compression mode.");
            }
        }

        private static unsafe void ReadHeightMap(Stream stream, byte* output)
        {
            ChunkCompression compression = (ChunkCompression)stream.ReadUInt16();
            switch (compression)
            {
                case ChunkCompression.Raw:
                    stream.ReadExactly(new Span<byte>(output, Chunk.CHUNK_SIZE_SQUARED));
                    break;

                case ChunkCompression.RLE:
                    int runCount = stream.ReadByte();
                    if (runCount > ChunkHeader.RLEHeightMapBreakevenPoint) throw new FormatException("Corrupt height-map data, RLE data too long.");
                    HeightMapRun run = default;
                    for (int i = 0; i < runCount; i++)
                    {
                        run.Read(stream);

                        if (run.Index + run.Count + 1 > Chunk.CHUNK_SIZE_CUBED) throw new IndexOutOfRangeException("Corrupt height-map data, RLE Decoding exceeded chunk boundaries.");

                        MemsetT(output + run.Index, run.Value, run.Count + 1); // shift count by one to remap from 0..255 to 1..256
                    }
                    break;

                default:
                    throw new FormatException("Corrupt height-map data, unknown compression mode.");
            }
        }
    }
}