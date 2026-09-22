namespace VoxelEngine.Network.Protocol
{
    public unsafe struct Message
    {
        public ProtocolVersion Version;
        public Record* Records;
        public uint Count;
    }
}