namespace VoxelEngine.Mathematics
{
    using System.Numerics;

    public struct InstanceData
    {
        public Matrix4x4 Transform;
    }

    public class Instance
    {
        public Matrix4x4 Transform;

        public static implicit operator InstanceData(Instance type)
        {
            return new() { Transform = type.Transform };
        }
    }
}