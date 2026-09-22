namespace VoxelEngine.Core.Input
{
    using Hexa.NET.Mathematics;
    using System;

    public static class Helper
    {
        public static Hexa.NET.SDL2.SDLKeyCode ConvertBack(Key code)
        {
            return (Hexa.NET.SDL2.SDLKeyCode)code;
        }

        public static Hexa.NET.SDL2.SDLGameControllerAxis ConvertBack(GamepadAxis gamepadAxis)
        {
            return (Hexa.NET.SDL2.SDLGameControllerAxis)gamepadAxis;
        }

        public static Hexa.NET.SDL2.SDLGameControllerButton ConvertBack(GamepadButton gamepadButton)
        {
            return (Hexa.NET.SDL2.SDLGameControllerButton)gamepadButton;
        }

        public static Key Convert(Hexa.NET.SDL2.SDLKeyCode code)
        {
            return (Key)code;
        }

        public static GamepadAxis Convert(Hexa.NET.SDL2.SDLGameControllerAxis axis)
        {
            return (GamepadAxis)axis;
        }

        public static GamepadButton Convert(Hexa.NET.SDL2.SDLGameControllerButton button)
        {
            return (GamepadButton)button;
        }

        public static GamepadType Convert(Hexa.NET.SDL2.SDLGameControllerType gameControllerType)
        {
            return (GamepadType)gameControllerType;
        }

        internal static GamepadSensorType Convert(Hexa.NET.SDL2.SDLSensorType sensorType)
        {
            return (GamepadSensorType)sensorType;
        }

        internal static Hexa.NET.SDL2.SDLSensorType ConvertBack(GamepadSensorType gamepadSensorType)
        {
            return (Hexa.NET.SDL2.SDLSensorType)gamepadSensorType;
        }

        internal static JoystickType Convert(Hexa.NET.SDL2.SDLJoystickType joystickType)
        {
            return (JoystickType)joystickType;
        }

        internal static JoystickPowerLevel Convert(Hexa.NET.SDL2.SDLJoystickPowerLevel joystickPowerLevel)
        {
            return (JoystickPowerLevel)joystickPowerLevel;
        }
    }
}