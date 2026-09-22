namespace VoxelEngine.Voxel
{
    using Hexa.NET.Mathematics;
    using VoxelEngine.Scenes;

    public unsafe partial class World : GameObject
    {
        public ChunkArray Chunks;
        public const byte SHIFT = 5;
        public const int MASK = 0x1f;
        public const int CHUNK_AMOUNT_X = int.MaxValue;
        public const int CHUNK_AMOUNT_Y = 16;
        public const int CHUNK_AMOUNT_Z = int.MaxValue;
        public const int CHUNK_AMOUNT_X_MIN = int.MinValue;
        public const int CHUNK_AMOUNT_Y_MIN = 0;
        public const int CHUNK_AMOUNT_Z_MIN = int.MinValue;

        public string Path { get; private set; }

        public unsafe bool IsNoBlock(int x, int y, int z)
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
                return true;
            }

            if (xlocal < 0 || xlocal >= Chunk.CHUNK_SIZE ||
                ylocal < 0 || ylocal >= Chunk.CHUNK_SIZE ||
                zlocal < 0 || zlocal >= Chunk.CHUNK_SIZE)
            {
                return true;
            }

            Chunk* c = Chunks[xglobal, yglobal, zglobal];

            // To lower memory usage, a chunk is null if it has no blocks
            if (c == null || c->Data == null)
            {
                return true;
            }

            // Chunk data accessed quickly using bit masks
            return c->Data[Extensions.MapToIndex(xlocal, ylocal, zlocal)].Type == Chunk.EMPTY;
        }

        public void Set(Chunk* chunk, Point3 pos)
        {
            if (pos.X < CHUNK_AMOUNT_X_MIN || pos.X >= CHUNK_AMOUNT_X ||
                pos.Y < CHUNK_AMOUNT_Y_MIN || pos.Y >= CHUNK_AMOUNT_Y ||
                pos.Z < CHUNK_AMOUNT_Z_MIN || pos.Z >= CHUNK_AMOUNT_Z)
            {
                return;
            }

            Chunks[pos.X, pos.Y, pos.Z] = chunk;
        }

        public void Set(Chunk* chunk, int x, int y, int z)
        {
            if (x < CHUNK_AMOUNT_X_MIN || x >= CHUNK_AMOUNT_X ||
                y < CHUNK_AMOUNT_Y_MIN || y >= CHUNK_AMOUNT_Y ||
                z < CHUNK_AMOUNT_Z_MIN || z >= CHUNK_AMOUNT_Z)
            {
                return;
            }

            Chunks[x, y, z] = chunk;
        }

        public Chunk* Get(Point3 pos)
        {
            if (pos.X < CHUNK_AMOUNT_X_MIN || pos.X >= CHUNK_AMOUNT_X ||
                pos.Y < CHUNK_AMOUNT_Y_MIN || pos.Y >= CHUNK_AMOUNT_Y ||
                pos.Z < CHUNK_AMOUNT_Z_MIN || pos.Z >= CHUNK_AMOUNT_Z)
            {
                return null;
            }

            return Chunks[pos.X, pos.Y, pos.Z];
        }

        public Chunk* Get(int x, int y, int z)
        {
            if (x < CHUNK_AMOUNT_X_MIN || x >= CHUNK_AMOUNT_X ||
                y < CHUNK_AMOUNT_Y_MIN || y >= CHUNK_AMOUNT_Y ||
                z < CHUNK_AMOUNT_Z_MIN || z >= CHUNK_AMOUNT_Z)
            {
                return null;
            }

            return Chunks[x, y, z];
        }

        public void Set(ChunkSegment region)
        {
            if (region.Position.X < CHUNK_AMOUNT_X_MIN || region.Position.X >= CHUNK_AMOUNT_X ||
                region.Position.Y < CHUNK_AMOUNT_Y_MIN || region.Position.Y >= CHUNK_AMOUNT_Y)
            {
                return;
            }

            for (int i = 0; i < ChunkSegment.CHUNK_SEGMENT_SIZE; i++)
            {
                Set(region.Chunks[i], region.Position.X, i, region.Position.Y);
            }
        }

        public ChunkSegment GetSegment(Point3 pos)
        {
            if (pos.X < CHUNK_AMOUNT_X_MIN || pos.X >= CHUNK_AMOUNT_X ||
                pos.Y < CHUNK_AMOUNT_Y_MIN || pos.Y >= CHUNK_AMOUNT_Y ||
                pos.Z < CHUNK_AMOUNT_Z_MIN || pos.Z >= CHUNK_AMOUNT_Z)
            {
                return default;
            }

            return ChunkSegment.CreateFrom(this, pos);
        }

        public ChunkSegment GetSegment(int x, int z)
        {
            if (x < CHUNK_AMOUNT_X_MIN || x >= CHUNK_AMOUNT_X ||
                z < CHUNK_AMOUNT_Y_MIN || z >= CHUNK_AMOUNT_Z)
            {
                return default;
            }

            return ChunkSegment.CreateFrom(this, x, z);
        }
    }
}