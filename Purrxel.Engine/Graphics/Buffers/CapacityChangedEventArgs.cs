namespace VoxelEngine.Graphics.Buffers
{
    public struct CapacityChangedEventArgs
    {
        public int OldCapacity;
        public int Capacity;

        public CapacityChangedEventArgs(int oldCapacity, int capacity)
        {
            OldCapacity = oldCapacity;
            Capacity = capacity;
        }
    }
}