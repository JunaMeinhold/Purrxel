namespace VoxelEngine.Core.Input.Events
{
    using VoxelEngine.Core.Input;

    public class MouseButtonEventArgs : EventArgs
    {
        public MouseButton Button { get; internal set; }

        public MouseButtonState State { get; internal set; }

        public int Clicks { get; internal set; }
    }
}