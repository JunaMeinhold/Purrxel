namespace VoxelEngine.Core.Input
{
    using Hexa.NET.SDL2;

    public unsafe class Haptic
    {
        private readonly int id;
        private readonly SDLHaptic* haptic;

        private Haptic(SDLHaptic* haptic)
        {
            this.haptic = haptic;
            id = SDL.HapticIndex(haptic).SdlThrowIfNeg();
        }

        public int Id => id;

        public string Name => SDL.HapticNameS(id);

        public int AxesCount => SDL.HapticNumAxes(haptic);

        public int EffectsCount => SDL.HapticNumEffects(haptic);

        public int EffectsPlayingCount => SDL.HapticNumEffectsPlaying(haptic);

        public bool RumbleSupported => SDL.HapticRumbleSupported(haptic) == 1;

        public HapticEffectFlags EffectsSupported => (HapticEffectFlags)SDL.HapticQuery(haptic);

        public static Haptic OpenFromGamepad(Gamepad gamepad)
        {
            return new(SDL.HapticOpenFromJoystick(gamepad.joystick));
        }

        public static Haptic OpenFromJoystick(Joystick joystick)
        {
            return new(SDL.HapticOpenFromJoystick(joystick.joystick));
        }

        public static Haptic OpenFromMouse()
        {
            return new(SDL.HapticOpenFromMouse());
        }

        public static Haptic OpenFromIndex(int index)
        {
            return new(SDL.HapticOpen(index));
        }
    }
}