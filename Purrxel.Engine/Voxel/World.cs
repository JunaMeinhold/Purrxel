namespace VoxelEngine.Voxel
{
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities;
    using System.Collections.Generic;
    using System.IO;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using VoxelEngine.Threading;
    using VoxelEngine.Voxel.WorldGen;

    public unsafe partial class World
    {
        public World(string path, int dimId = 0)
        {
            Name = nameof(World);
            DirectoryInfo dir = Directory.CreateDirectory(path);
            if (!dir.Exists)
            {
                dir.Create();
            }

            Chunks = new();
            Path = dir.FullName;
            DimId = dimId;
            DimensionManager.AddWorld(this);
        }

        public IChunkGenerator Generator { get; set; }

        public Player Player { get; set; }

        public VoxelHelper VoxelHelper { get; } = new(Matrix4x4.Identity);

        public IReadOnlyDictionary<Point2, ChunkSegment> LoadedChunkSegments => WorldLoader.LoadedChunkSegments;

        public BlockingList<RenderRegion> LoadedRenderRegions => WorldLoader.LoadedRenderRegions;

        public int DimId { get; }

        public WorldLoader WorldLoader;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Mod(int x)
        {
            return x & 15;
        }

        public void SetBlock(Point3 point, Block block) => SetBlock(point.X, point.Y, point.Z, block);

        public void SetBlock(int x, int y, int z, Block block)
        {
            int xglobal = x >> 4;
            int yglobal = y >> 4;
            int zglobal = z >> 4;

            int xlocal = x & 15;
            int ylocal = y & 15;
            int zlocal = z & 15;

            if (xglobal < CHUNK_AMOUNT_X_MIN || xglobal >= CHUNK_AMOUNT_X || yglobal < CHUNK_AMOUNT_Y_MIN || yglobal >= CHUNK_AMOUNT_Y || zglobal < CHUNK_AMOUNT_Z_MIN || zglobal >= CHUNK_AMOUNT_Z)
            {
                return;
            }

            Chunk* c = Chunks[xglobal, yglobal, zglobal];

            if (c == null)
            {
                return;
            }

            c->SetBlockInternal(block, xlocal, ylocal, zlocal);

            UpdateChunk(xglobal, yglobal, zglobal, true);

            if (xlocal == 15) UpdateChunk(xglobal + 1, yglobal, zglobal, false);
            if (ylocal == 15) UpdateChunk(xglobal, yglobal + 1, zglobal, false);
            if (zlocal == 15) UpdateChunk(xglobal, yglobal, zglobal + 1, false);
            if (xlocal == 0) UpdateChunk(xglobal - 1, yglobal, zglobal, false);
            if (ylocal == 0) UpdateChunk(xglobal, yglobal - 1, zglobal, false);
            if (zlocal == 0) UpdateChunk(xglobal, yglobal, zglobal - 1, false);
        }

        public Block GetBlock(Point3 point) => GetBlock(point.X, point.Y, point.Z);

        public Block GetBlock(int x, int y, int z)
        {
            int xglobal = x >> 4;
            int yglobal = y >> 4;
            int zglobal = z >> 4;

            int xlocal = x & 15;
            int ylocal = y & 15;
            int zlocal = z & 15;

            // If it is at the edge of the map, return true
            if (xglobal < CHUNK_AMOUNT_X_MIN || xglobal >= CHUNK_AMOUNT_X ||
                yglobal < CHUNK_AMOUNT_Y_MIN || yglobal >= CHUNK_AMOUNT_Y ||
                zglobal < CHUNK_AMOUNT_Z_MIN || zglobal >= CHUNK_AMOUNT_Z)
            {
                return default;
            }

            // Chunk accessed quickly using bitwise shifts
            Chunk* c = Chunks[xglobal, yglobal, zglobal];

            if (c == null) return Block.Air;

            return c->GetBlockInternal(xlocal, ylocal, zlocal);
        }

        public static bool InWorldLimits(int x, int y, int z)
        {
            if (x < CHUNK_AMOUNT_X_MIN || x >= CHUNK_AMOUNT_X ||
                y < CHUNK_AMOUNT_Y_MIN || y >= CHUNK_AMOUNT_Y ||
                z < CHUNK_AMOUNT_Z_MIN || z >= CHUNK_AMOUNT_Z)
            {
                return false;
            }

            return true;
        }

        public void UpdateChunk(int x, int y, int z, bool save)
        {
            if (x < CHUNK_AMOUNT_X_MIN || x >= CHUNK_AMOUNT_X ||
                y < CHUNK_AMOUNT_Y_MIN || y >= CHUNK_AMOUNT_Y ||
                z < CHUNK_AMOUNT_Z_MIN || z >= CHUNK_AMOUNT_Z)
            {
                return;
            }

            WorldLoader.Dispatch(Chunks[x, y, z], save);
        }

        public void LoadFromDisk(Vector3 pos)
        {
            for (int i = 0; i < CHUNK_AMOUNT_Y; i++)
            {
                Chunk* chunk = ChunkAllocator.New(this, (int)pos.X, i, (int)pos.Z);
                Set(chunk, (int)pos.X, i, (int)pos.Z);
            }
        }

        public override void Awake()
        {
            Player = Scene.Find<Player>()!;
            base.Awake();
            WorldLoader = new(this);
            Generator.Dispose();
        }

        public override void Destroy()
        {
            base.Destroy();
            WorldLoader.Dispose();
            Chunks.Clear();
            Chunks = null;
            DimensionManager.RemoveWorld(this);
        }
    }
}