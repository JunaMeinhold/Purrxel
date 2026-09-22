namespace VoxelEngine.Core.Input
{
    using System.Runtime.CompilerServices;
    using Hexa.NET.SDL2;

    public static class Joysticks
    {
        private static readonly List<Joystick> joysticks = new();
        private static readonly Dictionary<int, Joystick> idToJoystick = new();

        public static IReadOnlyList<Joystick> Sticks => joysticks;

        public static IReadOnlyDictionary<int, Joystick> IdToJoystick => idToJoystick;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddJoystick(SDLJoyDeviceEvent even)
        {
            Joystick joystick = new(even.Which);
            joysticks.Add(joystick);
            idToJoystick.Add(joystick.Id, joystick);
        }

        internal static void OnAxisMotion(SDLJoyAxisEvent even)
        {
            idToJoystick[even.Which].OnAxisMotion(even);
        }

        internal static void OnBallMotion(SDLJoyBallEvent even)
        {
            idToJoystick[even.Which].OnBallMotion(even);
        }

        internal static void OnButtonDown(SDLJoyButtonEvent even)
        {
            idToJoystick[even.Which].OnButtonDown(even);
        }

        internal static void OnButtonUp(SDLJoyButtonEvent even)
        {
            idToJoystick[even.Which].OnButtonUp(even);
        }

        internal static void OnHatMotion(SDLJoyHatEvent even)
        {
            idToJoystick[even.Which].OnHatMotion(even);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RemoveJoystick(SDLJoyDeviceEvent even)
        {
            Joystick joystick = idToJoystick[even.Which];
            joysticks.Remove(joystick);
            idToJoystick.Remove(even.Which);
            joystick.Dispose();
        }
    }
}