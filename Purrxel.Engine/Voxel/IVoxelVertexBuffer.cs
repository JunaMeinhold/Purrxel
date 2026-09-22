namespace VoxelEngine.Voxel
{
    public interface IVoxelVertexBuffer
    {
        VoxelVertex this[int index] { get; set; }

        int Capacity { get; set; }

        int Count { get; }

        void Dispose();

        void EnsureCapacity(int capacity);

        unsafe VoxelVertex* Increase(int count);

        void Reset(int length = 4096);

        void Lock();

        void ReleaseLock();
    }
}