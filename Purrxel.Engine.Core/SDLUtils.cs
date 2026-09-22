namespace VoxelEngine.Core
{
    using Hexa.NET.SDL2;
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    public static unsafe class SDLUtils
    {
        public static void Assert(bool condition)
        {
#if DEBUG
            Trace.Assert(condition);
#endif
        }

        public static void Assert(bool condition, string message)
        {
#if DEBUG
            Trace.Assert(condition, message);
#endif
        }

        public static void ThrowIf(bool condition, string message)
        {
            if (condition)
            {
                throw new(message);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIf(this Exception? exception)
        {
#if DEBUG
            if (exception != null)
            {
                throw exception;
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SdlThrowIf(this int result)
        {
#if DEBUG
            if (result == 0)
            {
                SDL.GetErrorAsException().ThrowIf();
            }
            return result;
#else
            return result;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SdlThrowIfNeg(this int result)
        {
#if DEBUG
            if (result < 0)
            {
                SDL.GetErrorAsException().ThrowIf();
            }
            return result;
#else
            return result;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SdlThrowIf(this uint result)
        {
#if DEBUG
            if (result == 0)
            {
                SDL.GetErrorAsException().ThrowIf();
            }
            return result;
#else
            return result;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SdlCheckError()
        {
#if DEBUG
            SDL.GetErrorAsException().ThrowIf();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* SdlCheckError(void* ptr)
        {
#if DEBUG
            if (ptr == null)
            {
                SDL.GetErrorAsException().ThrowIf();
            }
            return ptr;
#else
            return ptr;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* SdlCheckError<T>(T* ptr) where T : unmanaged
        {
#if DEBUG
            if (ptr == null)
            {
                SDL.GetErrorAsException().ThrowIf();
            }
            return ptr;
#else
            return ptr;
#endif
        }
    }
}