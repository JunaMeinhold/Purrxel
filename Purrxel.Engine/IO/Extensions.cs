namespace VoxelEngine.IO
{
    using System;
    using System.Buffers.Binary;
    using System.IO;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Text;

    public static class Extensions
    {
        public const int MaxStackallocSize = 2048;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadString(this Stream stream)
        {
            int length = stream.ReadInt32();
            Span<byte> buffer = length <= MaxStackallocSize ? stackalloc byte[length] : new byte[length];
            _ = stream.Read(buffer);
            return Encoding.UTF8.GetString(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[2];
            _ = stream.Read(buffer);
            short val = BinaryPrimitives.ReadInt16LittleEndian(buffer);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[2];
            _ = stream.Read(buffer);
            ushort val = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[4];
            _ = stream.Read(buffer);
            int val = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[4];
            _ = stream.Read(buffer);
            uint val = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[8];
            _ = stream.Read(buffer);
            long val = BinaryPrimitives.ReadInt64LittleEndian(buffer);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[8];
            _ = stream.Read(buffer);
            ulong val = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadSingle(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[4];
            _ = stream.Read(buffer);
            float val = BinaryPrimitives.ReadSingleLittleEndian(buffer);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDouble(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[8];
            _ = stream.Read(buffer);
            double val = BinaryPrimitives.ReadDoubleLittleEndian(buffer);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ReadVector2(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[8];
            _ = stream.Read(buffer);
            float x = BinaryPrimitives.ReadSingleLittleEndian(buffer);
            float y = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]);
            return new(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ReadVector3(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[12];
            _ = stream.Read(buffer);
            float x = BinaryPrimitives.ReadSingleLittleEndian(buffer);
            float y = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]);
            float z = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]);
            return new(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ReadVector4(this Stream stream)
        {
            Span<byte> buffer = stackalloc byte[16];
            _ = stream.Read(buffer);
            float x = BinaryPrimitives.ReadSingleLittleEndian(buffer);
            float y = BinaryPrimitives.ReadSingleLittleEndian(buffer[4..]);
            float z = BinaryPrimitives.ReadSingleLittleEndian(buffer[8..]);
            float w = BinaryPrimitives.ReadSingleLittleEndian(buffer[12..]);
            return new(x, y, z, w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadString(this Stream stream, out string result)
        {
            result = stream.ReadString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadInt16(this Stream stream, out short result)
        {
            result = stream.ReadInt16();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadUInt16(this Stream stream, out ushort result)
        {
            result = stream.ReadUInt16();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadInt32(this Stream stream, out int result)
        {
            result = stream.ReadInt32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadUInt32(this Stream stream, out uint result)
        {
            result = stream.ReadUInt32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadInt64(this Stream stream, out long result)
        {
            result = stream.ReadInt64();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadUInt64(this Stream stream, out ulong result)
        {
            result = stream.ReadUInt64();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadSingle(this Stream stream, out float result)
        {
            result = stream.ReadSingle();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadDouble(this Stream stream, out double result)
        {
            result = stream.ReadDouble();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadVector2(this Stream stream, out Vector2 result)
        {
            result = stream.ReadVector2();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadVector3(this Stream stream, out Vector3 result)
        {
            result = stream.ReadVector3();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadVector4(this Stream stream, out Vector4 result)
        {
            result = stream.ReadVector4();
        }

        public static byte[] Read(this Stream stream, long length)
        {
            byte[] buffer = new byte[length];
            _ = stream.Read(buffer, 0, (int)length);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteString(this Stream stream, string value)
        {
            int length = Encoding.UTF8.GetByteCount(value);
            Span<byte> buffer = length <= MaxStackallocSize ? stackalloc byte[length] : new byte[length];
            stream.WriteInt32(length);
            Encoding.UTF8.GetBytes(value, buffer);
            stream.Write(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16(this Stream stream, short value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16(this Stream stream, ushort value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32(this Stream stream, int value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32(this Stream stream, uint value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64(this Stream stream, long value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64(this Stream stream, ulong value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingle(this Stream stream, float value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
            stream.Write(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(this Stream stream, double value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
            stream.Write(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVector2(this Stream stream, Vector2 value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteSingleLittleEndian(buffer, value.X);
            BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], value.Y);
            stream.Write(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVector3(this Stream stream, Vector3 value)
        {
            Span<byte> buffer = stackalloc byte[12];
            BinaryPrimitives.WriteSingleLittleEndian(buffer, value.X);
            BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], value.Y);
            BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], value.Z);
            stream.Write(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVector4(this Stream stream, Vector4 value)
        {
            Span<byte> buffer = stackalloc byte[16];
            BinaryPrimitives.WriteSingleLittleEndian(buffer, value.X);
            BinaryPrimitives.WriteSingleLittleEndian(buffer[4..], value.Y);
            BinaryPrimitives.WriteSingleLittleEndian(buffer[8..], value.Z);
            BinaryPrimitives.WriteSingleLittleEndian(buffer[12..], value.W);
            stream.Write(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadCompare(this Stream stream, byte[] bytes)
        {
            Span<byte> buffer = stackalloc byte[bytes.Length];
            stream.ReadExactly(buffer);
            return buffer.SequenceEqual(bytes);
        }
    }
}