namespace VoxelEngine.Scenes
{
    using System;

    public readonly struct PassIdentifer : IEquatable<PassIdentifer>
    {
        public readonly string Name;
        public readonly int HashCode;

        public PassIdentifer(string name)
        {
            Name = name;
            HashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is PassIdentifer identifer && Equals(identifer);
        }

        public readonly bool Equals(PassIdentifer other)
        {
            return HashCode == other.HashCode && StringComparer.OrdinalIgnoreCase.Equals(Name, other.Name);
        }

        public override readonly int GetHashCode()
        {
            return HashCode;
        }

        public static bool operator ==(PassIdentifer left, PassIdentifer right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PassIdentifer left, PassIdentifer right)
        {
            return !(left == right);
        }

        public static readonly PassIdentifer DirectionalLightShadowPass = new("directionalpass");
        public static readonly PassIdentifer DeferredPass = new("deferred");
        public static readonly PassIdentifer ForwardPass = new("forward");
    }
}