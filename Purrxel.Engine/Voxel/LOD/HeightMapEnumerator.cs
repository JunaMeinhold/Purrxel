namespace VoxelEngine.Voxel.LOD
{
    using System.Runtime.CompilerServices;

    public unsafe struct HeightMapEnumerator
    {
        public readonly Chunk* Chunk => current.Chunk;

        public ushort Index;
        private HeightMapEntry current;

        public readonly HeightMapEntry Current => current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HeightMapEnumerator(Chunk* chunk)
        {
            current.Chunk = chunk;
            Index = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (Index < Voxel.Chunk.CHUNK_SIZE * Voxel.Chunk.CHUNK_SIZE)
            {
                byte index = (byte)Index++;
                byte minY = Chunk->MinY[index];
                byte maxY = Chunk->MaxY[index];

                if (minY > maxY)
                    continue;

                current.Index = index;
                current.X = (byte)(index & 0xF);
                current.Z = (byte)(index >> 4);
                current.MinY = minY;
                current.MaxY = maxY;

                return true;
            }

            return false;
        }
    }
}