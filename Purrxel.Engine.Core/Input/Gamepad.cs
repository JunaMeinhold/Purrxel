namespace VoxelEngine.Core.Input
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Hexa.NET.SDL2;
    using VoxelEngine.Core.Input.Events;

    public unsafe class Gamepad : IDisposable
    {
        private static readonly GamepadAxis[] axes = Enum.GetValues<GamepadAxis>();
        private static readonly string[] axisNames = new string[axes.Length];
        private static readonly GamepadButton[] buttons = Enum.GetValues<GamepadButton>();
        private static readonly string[] buttonNames = new string[buttons.Length];

        private static readonly GamepadSensorType[] sensorTypes = Enum.GetValues<GamepadSensorType>();

        private readonly int id;
        private readonly SDLGameController* controller;
        internal readonly SDLJoystick* joystick;
        private readonly Dictionary<GamepadAxis, short> axisStates = new();
        private readonly Dictionary<GamepadButton, GamepadButtonState> buttonStates = new();
        private readonly Dictionary<GamepadSensorType, GamepadSensor> sensors = new();
        private readonly List<GamepadTouchpad> touchpads = new();
        private readonly Haptic? haptic;
        private readonly List<string> mappings = new();

        private readonly GamepadRemappedEventArgs remappedEventArgs = new();
        private readonly GamepadAxisMotionEventArgs axisMotionEventArgs = new();
        private readonly GamepadButtonEventArgs buttonEventArgs = new();

        private readonly string guid;
        private short deadzone = 8000;

        static Gamepad()
        {
            for (int i = 0; i < axes.Length; i++)
            {
                axisNames[i] = SDL.GameControllerGetStringForAxisS(Helper.ConvertBack(axes[i]));
            }
            for (int i = 0; i < buttons.Length; i++)
            {
                buttonNames[i] = SDL.GameControllerGetStringForButtonS(Helper.ConvertBack(buttons[i]));
            }
        }

        public Gamepad(int id)
        {
            controller = SDL.GameControllerOpen(id);
            if (controller == null)
                SdlCheckError();
            joystick = SDL.GameControllerGetJoystick(controller);
            if (controller == null)
                SdlCheckError();
            this.id = SDL.JoystickInstanceID(joystick).SdlThrowIfNeg();
            var axes = Enum.GetValues<GamepadAxis>();
            for (int i = 0; i < axes.Length; i++)
            {
                if (SDL.GameControllerHasAxis(controller, Helper.ConvertBack(axes[i])) == SDLBool.True)
                {
                    axisStates.Add(axes[i], 0);
                }
            }
            var buttons = Enum.GetValues<GamepadButton>();
            for (int i = 0; i < buttons.Length; i++)
            {
                if (SDL.GameControllerHasButton(controller, Helper.ConvertBack(buttons[i])) == SDLBool.True)
                {
                    buttonStates.Add(buttons[i], GamepadButtonState.Up);
                }
            }

            var touchpadCount = SDL.GameControllerGetNumTouchpads(controller);
            for (int i = 0; i < touchpadCount; i++)
            {
                touchpads.Add(new(i, controller));
            }

            var sensorTypes = Enum.GetValues<GamepadSensorType>();
            for (int i = 0; i < sensorTypes.Length; i++)
            {
                if (SDL.GameControllerHasSensor(controller, Helper.ConvertBack(sensorTypes[i])) == SDLBool.True)
                {
                    sensors.Add(sensorTypes[i], new(controller, sensorTypes[i]));
                }
            }

            var mappingCount = SDL.GameControllerNumMappings();
            for (int i = 0; i < mappingCount; i++)
            {
                var mapping = SDL.GameControllerMappingForIndexS(i);
                if (mapping == null)
                    SdlCheckError();
                mappings.Add(mapping);
            }

            if (SDL.JoystickIsHaptic(joystick) == 1)
            {
                haptic = Haptic.OpenFromGamepad(this);
            }

            var guid = SDL.JoystickGetGUID(joystick);
            SdlCheckError();
            var buffer = AllocT<byte>(33);
            SDL.JoystickGetGUIDString(guid, buffer, 33);
            var size = StrLen(buffer);
            var value = Encoding.ASCII.GetString(buffer, size - 1);
            Free(buffer);
            this.guid = value;
        }

        public static IReadOnlyList<GamepadAxis> Axes => axes;

        public static IReadOnlyList<string> AxisNames => axisNames;

        public static IReadOnlyList<GamepadButton> Buttons => buttons;

        public static IReadOnlyList<string> ButtonNames => buttonNames;

        public static IReadOnlyList<GamepadSensorType> SensorTypes => sensorTypes;

        public int Id => id;

        public string Name
        {
            get
            {
                var name = SDL.GameControllerNameS(controller);
                if (name == null)
                    SdlCheckError();
                return name;
            }
        }

        public ushort Vendor => SDL.GameControllerGetVendor(controller);

        public ushort Product => SDL.GameControllerGetProduct(controller);

        public ushort ProductVersion => SDL.GameControllerGetProductVersion(controller);

        public string Serial => SDL.GameControllerGetSerialS(controller);

        public string GUID => guid;

        public bool IsAttached => SDL.GameControllerGetAttached(controller) == SDLBool.True;

        public bool IsHaptic => SDL.JoystickIsHaptic(joystick) == 1;

        public bool HasLED => SDL.GameControllerHasLED(controller) == SDLBool.True;

        public GamepadType Type => Helper.Convert(SDL.GameControllerGetType(controller));

        public short Deadzone { get => deadzone; set => deadzone = value; }

        public int PlayerIndex { get => SDL.GameControllerGetPlayerIndex(controller); set => SDL.GameControllerSetPlayerIndex(controller, value); }

        public string Mapping
        {
            get
            {
                var mapping = SDL.GameControllerMappingS(controller); ;
                if (mapping == null)
                    SdlCheckError();
                return mapping;
            }
        }

        public IReadOnlyDictionary<GamepadAxis, short> AxisStates => axisStates;

        public IReadOnlyDictionary<GamepadButton, GamepadButtonState> ButtonStates => buttonStates;

        public IReadOnlyDictionary<GamepadSensorType, GamepadSensor> Sensors => sensors;

        public IReadOnlyList<GamepadTouchpad> Touchpads => touchpads;

        public IReadOnlyList<string> Mappings => mappings;

        public Haptic? Haptic => haptic;

        public event EventHandler<GamepadRemappedEventArgs>? Remapped;

        public event EventHandler<GamepadAxisMotionEventArgs>? AxisMotion;

        public event EventHandler<GamepadButtonEventArgs>? ButtonDown;

        public event EventHandler<GamepadButtonEventArgs>? ButtonUp;

        public bool HasButton(GamepadButton button)
        {
            return buttonStates.ContainsKey(button);
        }

        public bool HasAxis(GamepadAxis axis)
        {
            return axisStates.ContainsKey(axis);
        }

        public bool HasSensor(GamepadSensorType sensor)
        {
            return sensors.ContainsKey(sensor);
        }

        public bool IsDown(GamepadButton button)
        {
            return buttonStates[button] == GamepadButtonState.Down;
        }

        public bool IsUp(GamepadButton button)
        {
            return buttonStates[button] == GamepadButtonState.Up;
        }

        public void Rumble(ushort lowFreq, ushort highFreq, uint durationMs)
        {
            SDL.GameControllerRumble(controller, lowFreq, highFreq, durationMs);
        }

        public void RumbleTriggers(ushort rightRumble, ushort leftRumble, uint durationMs)
        {
            SDL.GameControllerRumbleTriggers(controller, rightRumble, leftRumble, durationMs);
        }

        public void SetLED(Vector4 color)
        {
            SDL.GameControllerSetLED(controller, (byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255));
        }

        public void SetLED(byte red, byte green, byte blue)
        {
            SDL.GameControllerSetLED(controller, red, green, blue);
        }

        public void AddMapping(string mapping)
        {
            SDL.GameControllerAddMapping(mapping).SdlThrowIfNeg();
            mappings.Add(mapping);
        }

        internal void OnRemapped()
        {
            remappedEventArgs.Mapping = Mapping;
            Remapped?.Invoke(this, remappedEventArgs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnAxisMotion(SDLControllerAxisEvent even)
        {
            var axis = Helper.Convert((SDLGameControllerAxis)even.Axis);
            if (Math.Abs((int)even.Value) < deadzone)
            {
                even.Value = 0;
            }

            if (even.Value == axisStates[axis])
            {
                return;
            }

            axisStates[axis] = even.Value;
            axisMotionEventArgs.Axis = axis;
            axisMotionEventArgs.Value = even.Value;
            AxisMotion?.Invoke(this, axisMotionEventArgs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnButtonDown(SDLControllerButtonEvent even)
        {
            var button = Helper.Convert((SDLGameControllerButton)even.Button);
            buttonStates[button] = GamepadButtonState.Down;
            buttonEventArgs.Button = button;
            buttonEventArgs.State = GamepadButtonState.Down;
            ButtonDown?.Invoke(this, buttonEventArgs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnButtonUp(SDLControllerButtonEvent even)
        {
            var button = Helper.Convert((SDLGameControllerButton)even.Button);
            buttonStates[button] = GamepadButtonState.Up;
            buttonEventArgs.Button = button;
            buttonEventArgs.State = GamepadButtonState.Up;
            ButtonUp?.Invoke(this, buttonEventArgs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnTouchPadDown(SDLControllerTouchpadEvent even)
        {
            touchpads[even.Touchpad].OnTouchPadDown(even);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnTouchPadMotion(SDLControllerTouchpadEvent even)
        {
            touchpads[even.Touchpad].OnTouchPadMotion(even);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnTouchPadUp(SDLControllerTouchpadEvent even)
        {
            touchpads[even.Touchpad].OnTouchPadUp(even);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnSensorUpdate(SDLControllerSensorEvent even)
        {
            sensors[Helper.Convert((SDLSensorType)even.Sensor)].OnSensorUpdate(even);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            foreach (var sensor in sensors)
            {
                sensor.Value?.Dispose();
            }
            SDL.GameControllerClose(controller);
            SdlCheckError();
        }
    }
}