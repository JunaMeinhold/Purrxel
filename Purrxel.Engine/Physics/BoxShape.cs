namespace Purrxel.Engine.Physics
{
    using System.Numerics;

    public struct BoxShape : IShape
    {
        private readonly ShapeType type = ShapeType.Box;
        public Pose Pose;
        public Vector3 Size;

        public BoxShape(Vector3 size)
        {
            Size = size;
        }

        public readonly ShapeType Type => type;
    }
}