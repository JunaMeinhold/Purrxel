namespace Purrxel.Engine.Voxel
{
    public unsafe class BlockAllocator
    {
        public struct FreeNode
        {
            public FreeNode* Next;
        }

        private nint freeList;

        public void Free(Block* block)
        {
            var node = (FreeNode*)block;
            do
            {
                var next = freeList;
                node->Next = (FreeNode*)next;
            } while (Interlocked.CompareExchange(ref freeList, (nint)node, (nint)node->Next) != (nint)node->Next);
        }
    }
}