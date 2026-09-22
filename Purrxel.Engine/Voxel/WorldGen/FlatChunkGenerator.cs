namespace VoxelEngine.Voxel.WorldGen
{
    using Hexa.NET.Mathematics;
    using System.Numerics;
    using VoxelEngine.Voxel;
    using static VoxelEngine.Voxel.Blocks.BlockRegistry;

    public unsafe class FlatChunkGenerator : IChunkGenerator
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public int Height { get; set; } = 4;

        public void GenerateBatch(ref ChunkSegment.ChunkArray chunks, World world, Point3 position)
        {
            for (int i = 0; i < ChunkSegment.CHUNK_SEGMENT_SIZE; i++)
            {
                Chunk* chunk = ChunkAllocator.New(world, (int)position.X, i, (int)position.Z, true);
                world.Set(chunk, new(position.X, i, position.Z));
                chunks[i] = chunk;
            }

            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    float he = Height;
                    if (he <= 0)
                    {
                        continue;
                    }

                    for (int h = 0; h < (int)he & h < Chunk.CHUNK_SIZE_SQUARED; h++)
                    {
                        chunks.SetBlock(new Point3(x, h, z), GetBlock(h, (int)he));
                    }
                }
            }
        }

        public static Block GetBlock(int height, int maxheight)
        {
            if (height == maxheight - 1)
            {
                return GetBlockByName("Grass");
            }
            else
            {
                return GetBlockByName("Dirt");
            }
        }
    }
}