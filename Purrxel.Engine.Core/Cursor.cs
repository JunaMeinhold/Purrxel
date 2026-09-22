namespace VoxelEngine.Core
{
    using Hexa.NET.SDL2;

    public static unsafe class CursorHelper
    {
        public static void SetCursor(SDLSystemCursor cursor)
        {
            SDL.SetCursor(SDL.CreateSystemCursor(cursor));
        }

        public static void SetCursor(IntPtr ptr)
        {
            SDL.SetCursor((SDLCursor*)ptr.ToPointer());
        }
    }
}