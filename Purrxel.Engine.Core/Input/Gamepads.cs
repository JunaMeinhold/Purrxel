namespace VoxelEngine.Core.Input
{
    using System.Runtime.CompilerServices;
    using Hexa.NET.SDL2;

    public static unsafe class Gamepads
    {
        private static readonly List<Gamepad> gamepads = new();
        private static readonly Dictionary<int, Gamepad> idToGamepads = new();

        public static IReadOnlyList<Gamepad> Controllers => gamepads;

        public static IReadOnlyDictionary<int, Gamepad> IdToGamepad => idToGamepads;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Init()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddController(SDLControllerDeviceEvent even)
        {
            Gamepad gamepad = new(even.Which);
            gamepads.Add(gamepad);
            idToGamepads.Add(gamepad.Id, gamepad);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RemoveController(SDLControllerDeviceEvent even)
        {
            Gamepad gamepad = idToGamepads[even.Which];
            gamepads.Remove(gamepad);
            idToGamepads.Remove(even.Which);
            gamepad.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Remapped(SDLControllerDeviceEvent even)
        {
            idToGamepads[even.Which].OnRemapped();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AxisMotion(SDLControllerAxisEvent even)
        {
            idToGamepads[even.Which].OnAxisMotion(even);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ButtonDown(SDLControllerButtonEvent even)
        {
            idToGamepads[even.Which].OnButtonDown(even);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ButtonUp(SDLControllerButtonEvent even)
        {
            idToGamepads[even.Which].OnButtonUp(even);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void TouchPadDown(SDLControllerTouchpadEvent even)
        {
            idToGamepads[even.Which].OnTouchPadDown(even);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void TouchPadMotion(SDLControllerTouchpadEvent even)
        {
            idToGamepads[even.Which].OnTouchPadDown(even);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void TouchPadUp(SDLControllerTouchpadEvent even)
        {
            idToGamepads[even.Which].OnTouchPadUp(even);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SensorUpdate(SDLControllerSensorEvent even)
        {
            idToGamepads[even.Which].OnSensorUpdate(even);
        }
    }
}