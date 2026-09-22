namespace VoxelEngine.Voxel.WorldGen
{
    using Hexa.NET.Mathematics;
    using System;
    using System.Numerics;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Mathematics.Noise;
    using VoxelEngine.Voxel;
    using VoxelEngine.Voxel.WorldGen.Biomes;
    using static VoxelEngine.Voxel.Blocks.BlockRegistry;

    public struct PrefabRecord
    {
        public Point3 Pos;
        public Block Block;

        public PrefabRecord(Point3 pos, Block block)
        {
            Pos = pos;
            Block = block;
        }
    }

    public struct BlockBoundingBox
    {
        public Point3 Min;
        public Point3 Max;

        public BlockBoundingBox(Point3 min, Point3 max)
        {
            Min = min;
            Max = max;
        }

        public readonly BlockBoundingBox Offset(Point3 translation)
        {
            return new(Min + translation, Max + translation);
        }

        public readonly BlockBoundingBox Merge(BlockBoundingBox other)
        {
            return new(MathHelper.Min(Min, other.Min), MathHelper.Max(Max, other.Max));
        }

        public readonly ContainmentType Contains(in Vector3 point)
        {
            if (Min.X <= point.X && Max.X >= point.X && Min.Y <= point.Y && Max.Y >= point.Y && Min.Z <= point.Z && Max.Z >= point.Z)
            {
                return ContainmentType.Contains;
            }

            return ContainmentType.Disjoint;
        }

        public readonly ContainmentType Contains(BlockBoundingBox box)
        {
            if (Max.X < box.Min.X || Min.X > box.Max.X)
            {
                return ContainmentType.Disjoint;
            }

            if (Max.Y < box.Min.Y || Min.Y > box.Max.Y)
            {
                return ContainmentType.Disjoint;
            }

            if (Max.Z < box.Min.Z || Min.Z > box.Max.Z)
            {
                return ContainmentType.Disjoint;
            }

            if (Min.X <= box.Min.X && box.Max.X <= Max.X && Min.Y <= box.Min.Y && box.Max.Y <= Max.Y && Min.Z <= box.Min.Z && box.Max.Z <= Max.Z)
            {
                return ContainmentType.Contains;
            }

            return ContainmentType.Intersects;
        }
    }

    public class BlockPrefab
    {
        public List<PrefabRecord> Blocks = [];
        public BlockBoundingBox BoundingBox;

        public static BlockPrefab Default = GetTreePrefab();

        private static BlockPrefab GetTreePrefab()
        {
            BlockPrefab tree = new();

            int trunkHeight = 5;
            for (int i = 0; i < trunkHeight; i++)
                tree.Blocks.Add(new(new(0, i, 0), GetBlockByName("Oak Log")));

            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    if (Math.Abs(dx) == 2 && Math.Abs(dz) == 2) continue;

                    tree.Blocks.Add(new(new(dx, trunkHeight - 2, dz), GetBlockByName("Oak Leaves")));
                    tree.Blocks.Add(new(new(dx, trunkHeight - 1, dz), GetBlockByName("Oak Leaves")));
                }
            }

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    tree.Blocks.Add(new(new(dx, trunkHeight, dz), GetBlockByName("Oak Leaves")));
                }
            }

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (Math.Abs(dx) == 1 && Math.Abs(dz) == 1) continue;
                    tree.Blocks.Add(new(new(dx, trunkHeight + 1, dz), GetBlockByName("Oak Leaves")));
                }
            }

            tree.ComputeBoundingBox();
            return tree;
        }

        public void ComputeBoundingBox()
        {
            Point3 min = new(int.MaxValue);
            Point3 max = new(int.MinValue);

            foreach (PrefabRecord record in Blocks)
            {
                min = MathHelper.Min(min, record.Pos);
                max = MathHelper.Max(max, record.Pos);
            }

            BoundingBox = new(min, max);
        }
    }

    public unsafe class DefaultChunkGenerator : DisposableBase, IChunkGenerator
    {
        private readonly GenericNoise genericNoise;
        private readonly PerlinNoise perlinNoise;
        private readonly SimplexNoise simplexNoise;
        private readonly int seed;

        public DefaultChunkGenerator(int seed)
        {
            genericNoise = new(seed);
            perlinNoise = new();
            simplexNoise = new(seed);
            this.seed = seed;

            Biome plains = new()
            {
                Name = "Plains",
                Octaves = 3,
                Persistence = 0.5f,
                Amplitude = 1,
                MaxHeight = 120,
                MinHeight = 100,
                Redistribution = 5,
                WaterHeight = 100,
            };

            Biome hills = new()
            {
                Name = "Hills",
                Octaves = 5,
                Persistence = 0.6f,
                Amplitude = 4,
                MaxHeight = 240,
                MinHeight = 120,
                Redistribution = 12,
                WaterHeight = 60,
            };

            Biome mountains = new()
            {
                Name = "Mountains",
                Octaves = 3,
                Persistence = 0.5f,
                Amplitude = 2,
                MaxHeight = 240,
                MinHeight = 160,
                Redistribution = 10,
                WaterHeight = 100,
            };

            Biome ocean = new()
            {
                Name = "Ocean",
                Octaves = 3,
                Persistence = 0.4f,
                Amplitude = 2,
                MaxHeight = 80,
                MinHeight = 40,
                Redistribution = 10,
                WaterHeight = 100,
            };

            //Biomes.Add(plains);
            Biomes.Add(hills);
            Biomes.Add(ocean);
            //Biomes.Add(mountains);
        }

        public List<Biome> Biomes { get; } = new();

        public void GenerateBatch(ref ChunkSegment.ChunkArray chunks, World world, Point3 position)
        {
            for (int i = 0; i < ChunkSegment.CHUNK_SEGMENT_SIZE; i++)
            {
                Chunk* chunk = ChunkAllocator.New(world, (int)position.X, i, (int)position.Z, true);
                world.Set(chunk, new(position.X, i, position.Z));
                chunks[i] = chunk;
            }

            Point3 chunkPos = position * Chunk.CHUNK_SIZE;

            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    int globalX = chunkPos.X + x;
                    int globalZ = chunkPos.Z + z;

                    BiomeData data = BiomeData.GetBlendedBiomeData(perlinNoise, Biomes, globalX, globalZ);

                    int height = (int)data.ComputeHeight(perlinNoise, globalX, globalZ);

                    if (height <= 0)
                    {
                        continue;
                    }

                    CaveCoefficients coefficients = CaveCoefficients.Generate(perlinNoise, globalX, globalZ, 2, 100);

                    for (int h = 0; h < (int)height && h < Chunk.CHUNK_SIZE_SQUARED; h++)
                    {
                        float caveNoiseValue = coefficients.ComputeValue(h);

                        if (caveNoiseValue > 0)
                        {
                            continue;
                        }

                        chunks.SetBlock(new Point3(x, h, z), GetBlock(h, height));
                    }

                    if (height < 100)
                    {
                        for (int i = height; i < 100; i++)
                        {
                            chunks.SetBlock(new Point3(x, i, z), GetBlockByName("Water"));
                        }
                    }
                    else
                    {
                        if (ShouldPlaceTree(globalX, globalZ))
                        {
                            PlaceTree(ref chunks, new(x, (int)height, z), world);
                        }
                    }
                }
            }
        }

        private void PlaceTree(ref ChunkSegment.ChunkArray chunks, Point3 pos, World world)
        {
            BlockBoundingBox chunkBox = new(default, new(Chunk.CHUNK_SIZE - 1, Chunk.CHUNK_SIZE * World.CHUNK_AMOUNT_Y - 1, Chunk.CHUNK_SIZE - 1));
            var prefab = BlockPrefab.Default;

            BlockBoundingBox prefabBox = prefab.BoundingBox.Offset(pos);

            if (chunkBox.Contains(prefabBox) != ContainmentType.Contains) return;

            foreach (var item in prefab.Blocks)
            {
                var localPos = pos + item.Pos;

                if (chunkBox.Contains(localPos) == ContainmentType.Disjoint) continue;

                chunks.SetBlock(localPos, item.Block);
            }
        }

        public bool ShouldPlaceTree(int x, int z)
        {
            double noiseValue = simplexNoise.Noise(x, z);
            return noiseValue > 0.85;
        }

        public static Block GetBlock(int height, int maxheight)
        {
            if (height == maxheight - 1)
            {
                return GetBlockByName("Grass");
            }
            else if (height > maxheight - 5)
            {
                return GetBlockByName("Dirt");
            }
            else
            {
                return GetBlockByName("Stone");
            }
        }

        protected override void DisposeCore()
        {
        }
    }
}