namespace Purrxel.Engine.Core.Input.Events
{
    using Purrxel.Engine.Core.Input;

    public class JoystickButtonEventArgs : EventArgs
    {
        public JoystickButtonEventArgs()
        {
        }

        public JoystickButtonEventArgs(int button, JoystickButtonState state)
        {
            Button = button;
            State = state;
        }

        public int Button { get; internal set; }

        public JoystickButtonState State { get; internal set; }
    }
}