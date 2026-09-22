namespace VoxelEngine.Core.Input.Events
{
    public unsafe class KeyboardCharEventArgs : EventArgs
    {
        public KeyboardCharEventArgs()
        {
        }

        public byte* Text { get; internal set; }
    }
}