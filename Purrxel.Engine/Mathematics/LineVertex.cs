namespace VoxelEngine.Mathematics
{
    using System.Numerics;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct LineVertex
    {
        public LineVertex(Vector4 position)
        {
            Position = position;
        }

        public Vector4 Position;
    }
}