namespace VoxelEngine.Network.Protocol
{
    public unsafe struct Record
    {
        public RecordType Type;
        public uint Length;
        public void* Data;
    }
}