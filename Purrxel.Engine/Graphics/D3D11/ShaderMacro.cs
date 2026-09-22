namespace VoxelEngine.Graphics.D3D11
{
    public struct ShaderMacro : IEquatable<ShaderMacro>
    {
        public string Name;
        public string Definition;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderMacro"/> struct.
        /// </summary>
        /// <param name="name">The macro name.</param>
        /// <param name="definition">The macro definition.</param>
        public ShaderMacro(string name, object? definition)
        {
            Name = name;
            Definition = definition?.ToString() ?? throw new ArgumentNullException(nameof(definition));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderMacro"/> struct.
        /// </summary>
        /// <param name="name">The macro name.</param>
        /// <param name="definition">The macro definition.</param>
        public ShaderMacro(string name, string definition = "")
        {
            Name = name;
            Definition = definition;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is ShaderMacro macro && Equals(macro);
        }

        public readonly bool Equals(ShaderMacro other)
        {
            return Name == other.Name &&
                   Definition == other.Definition;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Name, Definition);
        }

        public static bool operator ==(ShaderMacro left, ShaderMacro right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ShaderMacro left, ShaderMacro right)
        {
            return !(left == right);
        }
    }
}