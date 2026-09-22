namespace Purrxel.Engine.Voxel
{
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities;
    using Purrxel.Engine.Voxel.Serialization;
    using System;
    using System.Collections;
    using System.IO;
    using VoxelEngine.Voxel;

    public unsafe struct ChunkSegment
    {
        public Point2 Position;
        public ChunkArray Chunks;
        public const int CHUNK_SEGMENT_SIZE = World.CHUNK_AMOUNT_Y;

        public struct ChunkArray : IEnumerable<Pointer<Chunk>>
        {
            private Chunk* chunk0;
            private Chunk* chunk1;
            private Chunk* chunk2;
            private Chunk* chunk3;
            private Chunk* chunk4;
            private Chunk* chunk5;
            private Chunk* chunk6;
            private Chunk* chunk7;
            private Chunk* chunk8;
            private Chunk* chunk9;
            private Chunk* chunk10;
            private Chunk* chunk11;
            private Chunk* chunk12;
            private Chunk* chunk13;
            private Chunk* chunk14;
            private Chunk* chunk15;

            public void SetBlock(Point3 pos, Block block)
            {
                if (pos.Y < 0 || pos.Y > 255) return;
                int index = pos.Y >> 4;
                int height = pos.Y & 15;

                var chunk = this[index];

                if (block.Type == 0 && !chunk->InMemory)
                {
                    return;
                }
                if (block.Type != 0 && !chunk->InMemory)
                {
                    chunk->Allocate(true);
                }

                int heightAccess = new Point2(pos.X, pos.Z).MapToIndex();
                if (block.Type == 0)
                {
                    chunk->BlockCount--;
                    if (chunk->MaxY[heightAccess] == height + 1)
                    {
                        byte newMaxY = 0;
                        for (int y = height; y >= 0; y--)
                        {
                            if (chunk->Data[new Point3(pos.X, y, pos.Z).MapToIndex()].Type != 0)
                            {
                                newMaxY = (byte)(y + 1);
                                break;
                            }
                        }
                        chunk->MaxY[heightAccess] = newMaxY;
                    }

                    if (chunk->MinY[heightAccess] == height)
                    {
                        byte newMinY = 15;
                        for (int y = height; y <= 16; y++)
                        {
                            if (chunk->Data[new Point3(pos.X, y, pos.Z).MapToIndex()].Type != 0)
                            {
                                newMinY = (byte)y;
                                break;
                            }
                        }
                        chunk->MinY[heightAccess] = newMinY;
                    }

                    if (chunk->BlockCount == 0)
                    {
                        chunk->FreeMemory();
                    }
                }
                else
                {
                    chunk->BlockCount++;
                    chunk->MinY[heightAccess] = Math.Min(chunk->MinY[heightAccess], (byte)height);
                    chunk->MaxY[heightAccess] = Math.Max(chunk->MaxY[heightAccess], (byte)(height + 1));
                }

                chunk->Data[new Point3(pos.X, height, pos.Z).MapToIndex()] = block;
            }

            public Chunk* this[int index]
            {
                get => index switch
                {
                    0 => chunk0,
                    1 => chunk1,
                    2 => chunk2,
                    3 => chunk3,
                    4 => chunk4,
                    5 => chunk5,
                    6 => chunk6,
                    7 => chunk7,
                    8 => chunk8,
                    9 => chunk9,
                    10 => chunk10,
                    11 => chunk11,
                    12 => chunk12,
                    13 => chunk13,
                    14 => chunk14,
                    15 => chunk15,
                    _ => throw new IndexOutOfRangeException(),
                };
                set
                {
                    switch (index)
                    {
                        case 0: chunk0 = value; break;
                        case 1: chunk1 = value; break;
                        case 2: chunk2 = value; break;
                        case 3: chunk3 = value; break;
                        case 4: chunk4 = value; break;
                        case 5: chunk5 = value; break;
                        case 6: chunk6 = value; break;
                        case 7: chunk7 = value; break;
                        case 8: chunk8 = value; break;
                        case 9: chunk9 = value; break;
                        case 10: chunk10 = value; break;
                        case 11: chunk11 = value; break;
                        case 12: chunk12 = value; break;
                        case 13: chunk13 = value; break;
                        case 14: chunk14 = value; break;
                        case 15: chunk15 = value; break;
                        default: throw new IndexOutOfRangeException();
                    }
                }
            }

            private struct Enumerator : IEnumerator<Pointer<Chunk>>
            {
                private int _index;
                private ChunkArray array;

                public Enumerator(ChunkArray array)
                {
                    this.array = array;
                }

                public Pointer<Chunk> Current => array[_index];

                object IEnumerator.Current => Current;

                public readonly void Dispose()
                {
                }

                public bool MoveNext()
                {
                    _index++;

                    return _index < CHUNK_SEGMENT_SIZE;
                }

                public void Reset()
                {
                    _index = -1;
                }
            }

            public readonly IEnumerator<Pointer<Chunk>> GetEnumerator()
            {
                return new Enumerator(this);
            }

            readonly IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(this);
            }
        }

        public ChunkSegment(Point2 position)
        {
            Position = position;
        }

        public readonly bool IsEmpty => Chunks[0] is null;

        public readonly bool InMemory => Chunks[0] is not null && Chunks[0]->InMemory;

        public readonly bool MissingNeighbours => Chunks.Any(x => x.Data->MissingNeighbours);

        public void Generate(World world)
        {
            world.Generator.GenerateBatch(ref Chunks, world, new(Position.X, 0, Position.Y));
        }

        public readonly void Update(bool loadToSimulation)
        {
            if (loadToSimulation)
            {
                for (int i = 0; i < CHUNK_SEGMENT_SIZE; i++)
                {
                    Chunk* chunk = Chunks[i];
                    chunk->Update(chunk);
                }
            }
            else
            {
                for (int i = 0; i < CHUNK_SEGMENT_SIZE; i++)
                {
                    Chunk* chunk = Chunks[i];
                    chunk->Update(chunk);
                }
            }
        }

        public readonly void Unload()
        {
            for (int i = 0; i < CHUNK_SEGMENT_SIZE; i++)
            {
                Chunk* chunk = Chunks[i];
                chunk->Unload(chunk);
                chunk->Dispose(chunk);
            }
        }

        public readonly void FreeSimulationMemory()
        {
            for (int i = 0; i < CHUNK_SEGMENT_SIZE; i++)
            {
                Chunk* chunk = Chunks[i];
                chunk->FreeSimulationMemory();
            }
        }

        public long PreSerialize(Span<ChunkPreSerialized> preSerializeds)
        {
            long size = 0;
            for (int i = 0; i < CHUNK_SEGMENT_SIZE; i++)
            {
                Chunk* chunk = Chunks[i];
                var result = chunk->PreSerialize(chunk);
                size += result.Length;
                preSerializeds[i] = result;
            }
            return size;
        }

        public void Serialize(Stream stream, Span<ChunkPreSerialized> preSerializeds)
        {
            for (int i = 0; i < CHUNK_SEGMENT_SIZE; i++)
            {
                Chunk* chunk = Chunks[i];
                chunk->Serialize(chunk, stream, preSerializeds[i]);
            }
        }

        public unsafe void LoadFromStream(Stream stream, World world)
        {
            for (int i = 0; i < CHUNK_SEGMENT_SIZE; i++)
            {
                Chunk* chunk = ChunkAllocator.New(world, Position.X, i, Position.Y);
                chunk->Deserialize(chunk, stream);
                Chunks[i] = chunk;
                world.Chunks[Chunks[i]->Position] = chunk;
            }

            world.Set(this);
        }

        public static ChunkSegment CreateFrom(World world, Point3 pos)
        {
            ChunkSegment segment = new(new Point2(pos.X, pos.Z));
            for (int y = 0; y < World.CHUNK_AMOUNT_Y; y++)
            {
                segment.Chunks[y] = world.Get(new Point3(pos.X, y, pos.Z));
            }

            return segment;
        }

        public static ChunkSegment CreateFrom(World world, int x, int z)
        {
            ChunkSegment segment = new(new Point2(x, z));
            for (int i = 0; i < World.CHUNK_AMOUNT_Y; i++)
            {
                segment.Chunks[i] = world.Get(new Point3(x, i, z));
            }

            return segment;
        }

        public static bool operator ==(ChunkSegment left, ChunkSegment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChunkSegment left, ChunkSegment right)
        {
            return !(left == right);
        }

        public override readonly bool Equals(object? obj)
        {
            if (obj is ChunkSegment region)
            {
                return region.Position == Position;
            }

            return false;
        }

        public override readonly int GetHashCode()
        {
            return Position.GetHashCode();
        }
    }
}