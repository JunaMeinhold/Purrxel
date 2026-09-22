namespace VoxelEngine.Voxel
{
    public unsafe struct ChunkHelper
    {
        public bool* visitXN = AllocT<bool>(Chunk.CHUNK_SIZE_CUBED);
        public bool* visitXP = AllocT<bool>(Chunk.CHUNK_SIZE_CUBED);
        public bool* visitZN = AllocT<bool>(Chunk.CHUNK_SIZE_CUBED);
        public bool* visitZP = AllocT<bool>(Chunk.CHUNK_SIZE_CUBED);
        public bool* visitYN = AllocT<bool>(Chunk.CHUNK_SIZE_CUBED);
        public bool* visitYP = AllocT<bool>(Chunk.CHUNK_SIZE_CUBED);

        public ChunkHelper()
        {
            ZeroMemoryT(visitXN, Chunk.CHUNK_SIZE_CUBED);
            ZeroMemoryT(visitXP, Chunk.CHUNK_SIZE_CUBED);
            ZeroMemoryT(visitZN, Chunk.CHUNK_SIZE_CUBED);
            ZeroMemoryT(visitZP, Chunk.CHUNK_SIZE_CUBED);
            ZeroMemoryT(visitYN, Chunk.CHUNK_SIZE_CUBED);
            ZeroMemoryT(visitYP, Chunk.CHUNK_SIZE_CUBED);
        }

        public void Release()
        {
            Free(visitXN);
            Free(visitXP);
            Free(visitZN);
            Free(visitZP);
            Free(visitYN);
            Free(visitYP);
            this = default;
        }
    }
}