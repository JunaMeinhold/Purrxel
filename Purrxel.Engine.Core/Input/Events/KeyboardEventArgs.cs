namespace VoxelEngine.Core.Input.Events
{
    using Hexa.NET.SDL2;
    using VoxelEngine.Core.Input;

    public class KeyboardEventArgs : EventArgs
    {
        public KeyboardEventArgs()
        {
        }

        public KeyboardEventArgs(Key keyCode, KeyState keyState, SDLScancode scancode)
        {
            KeyCode = keyCode;
            KeyState = keyState;
            Scancode = scancode;
        }

        public Key KeyCode { get; internal set; }

        public KeyState KeyState { get; internal set; }

        public SDLScancode Scancode { get; internal set; }
    }
}