namespace VoxelEngine.IO
{
    public struct Version : IEquatable<Version>
    {
        public byte Major;
        public byte Minor;
        public byte Patch;
        public byte Build;

        public Version(byte major, byte minor, byte patch, byte build)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Build = build;
        }

        public Version(uint version)
        {
            UIntToBytes(version, out Major, out Minor, out Patch, out Build);
        }

        public readonly uint ToUInt()
        {
            return BytesToUInt(Major, Minor, Patch, Build);
        }

        public static uint BytesToUInt(byte b1, byte b2, byte b3, byte b4)
        {
            return (uint)(b1 << 24 | b2 << 16 | b3 << 8 | b4);
        }

        public static void UIntToBytes(uint value, out byte b1, out byte b2, out byte b3, out byte b4)
        {
            b1 = (byte)((value >> 24) & 0xff);
            b2 = (byte)((value >> 16) & 0xff);
            b3 = (byte)((value >> 8) & 0xff);
            b4 = (byte)(value & 0xff);
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is Version version && Equals(version);
        }

        public readonly bool Equals(Version other)
        {
            return Major == other.Major &&
                   Minor == other.Minor &&
                   Patch == other.Patch &&
                   Build == other.Build;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Patch, Build);
        }

        public static implicit operator uint(Version v)
        {
            return v.ToUInt();
        }

        public static implicit operator Version(uint v)
        {
            return new(v);
        }

        public static bool operator ==(Version left, Version right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Version left, Version right)
        {
            return !(left == right);
        }

        public override readonly string ToString()
        {
            return $"{Major}.{Minor}.{Patch}.{Build}";
        }
    }
}