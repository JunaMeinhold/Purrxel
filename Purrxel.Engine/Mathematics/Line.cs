namespace VoxelEngine.Mathematics
{
    using System.Numerics;

    public struct Line
    {
        public Vector3 Origin;
        public Vector3 Destination;

        public Line(Vector3 origin, Vector3 destination)
        {
            Origin = origin;
            Destination = destination;
        }

        public static implicit operator LineVertex[](Line line)
        {
            return new LineVertex[] { new LineVertex(new(line.Origin, 1)), new LineVertex(new(line.Destination, 1)) };
        }
    }
}