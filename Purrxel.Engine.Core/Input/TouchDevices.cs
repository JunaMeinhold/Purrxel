namespace VoxelEngine.Core.Input
{
    using Hexa.NET.SDL2;

    public static class TouchDevices
    {
        internal static void Init()
        {
            var touchdevCount = SDL.GetNumTouchDevices();
            for (int i = 0; i < touchdevCount; i++)
            {
                var id = SDL.GetTouchDevice(i);
            }
        }
    }
}