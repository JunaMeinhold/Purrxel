namespace VoxelEngine.Voxel.LOD
{
    using System.Runtime.CompilerServices;

    public unsafe struct BlockEnumerator
    {
        public Block* Pointer;
        public Block* End;
        public ushort Index;
        public byte Y;
        public bool Started;

        public Block* Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Pointer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlockEnumerator(Block* begin, Block* end, ushort index, byte y)
        {
            Pointer = begin;
            End = end;
            Index = index;
            Y = y;
            Started = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!Started)
            {
                Started = true;
                return Pointer < End;
            }

            Pointer++;

            if (Pointer >= End)
                return false;

            Index++;
            Y++;
            return true;
        }
    }
}