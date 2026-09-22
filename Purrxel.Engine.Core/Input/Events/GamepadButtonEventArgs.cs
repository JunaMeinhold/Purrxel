namespace VoxelEngine.Core.Input.Events
{
    using VoxelEngine.Core.Input;

    public class GamepadButtonEventArgs : EventArgs
    {
        public GamepadButtonEventArgs()
        {
        }

        public GamepadButtonEventArgs(GamepadButton button, GamepadButtonState state)
        {
            Button = button;
            State = state;
        }

        public GamepadButton Button { get; internal set; }

        public GamepadButtonState State { get; internal set; }
    }
}