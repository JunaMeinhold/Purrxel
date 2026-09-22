namespace VoxelEngine.Core.Input.Events
{
    using VoxelEngine.Core.Input;

    public class GamepadAxisMotionEventArgs
    {
        public GamepadAxisMotionEventArgs()
        {
        }

        public GamepadAxisMotionEventArgs(GamepadAxis axis, short value)
        {
            Axis = axis;
            Value = value;
        }

        public GamepadAxis Axis { get; internal set; }

        public short Value { get; internal set; }
    }
}