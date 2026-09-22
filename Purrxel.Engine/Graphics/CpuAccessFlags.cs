namespace VoxelEngine.Graphics
{
    [Flags]
    public enum CpuAccessFlags
    {
        None,
        Write = 0x10000,
        Read = 0x20000,
    }
}