namespace VoxelEngine.Core.Input
{
    using System.Numerics;
    using Hexa.NET.SDL2;
    using VoxelEngine.Core.Input.Events;

    public unsafe class GamepadSensor : IDisposable
    {
        private readonly SDLGameController* controller;
        private readonly GamepadSensorType type;
        private readonly float* buffer;
        private readonly int length = 3;

        private readonly GamepadSensorUpdateEventArgs sensorUpdateEventArgs = new();

        private bool disposedValue;

        public GamepadSensor(SDLGameController* controller, GamepadSensorType sensorType)
        {
            this.controller = controller;
            type = sensorType;
            buffer = AllocT<float>(3);
            SDL.GameControllerGetSensorData(controller, Helper.ConvertBack(sensorType), buffer, length).SdlThrowIfNeg();
        }

        public bool Enabled
        {
            get => SDL.GameControllerIsSensorEnabled(controller, Helper.ConvertBack(type)) == SDLBool.True;
            set => SDL.GameControllerSetSensorEnabled(controller, Helper.ConvertBack(type), value ? SDLBool.True : SDLBool.False).SdlThrowIfNeg();
        }

        public GamepadSensorType Type => type;

        public Span<float> Data => new(buffer, length);

        public Vector3 Vector => *(Vector3*)buffer;

        public event EventHandler<GamepadSensorUpdateEventArgs>? SensorUpdate;

        internal void OnSensorUpdate(SDLControllerSensorEvent even)
        {
            MemcpyT(&even.Data_0, buffer, length);
            sensorUpdateEventArgs.Data = buffer;
            sensorUpdateEventArgs.Length = length;
            sensorUpdateEventArgs.Type = type;
            SensorUpdate?.Invoke(this, sensorUpdateEventArgs);
        }

        public void Flush()
        {
            SDL.GameControllerGetSensorData(controller, Helper.ConvertBack(type), buffer, length).SdlThrowIfNeg();
            sensorUpdateEventArgs.Data = buffer;
            sensorUpdateEventArgs.Length = length;
            sensorUpdateEventArgs.Type = type;
            SensorUpdate?.Invoke(this, sensorUpdateEventArgs);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Free(buffer);
                disposedValue = true;
            }
        }

        ~GamepadSensor()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}