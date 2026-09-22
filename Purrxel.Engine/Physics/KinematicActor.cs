namespace Purrxel.Engine.Physics
{
    using Hexa.NET.Utilities;
    using System.Numerics;

    public unsafe struct KinematicActor
    {
        internal Pose pose;
        internal Pose lastPose;

        internal UnsafeList<Pointer<Shape>> shapes;

        public bool Grounded;

        public void SetPosition(Vector3 position)
        {
            if (pose.Position == position) return;
            Grounded = false;
            pose.Position = position;
        }

        public Vector3 GetPosition() => pose.Position;

        public void AddShape<T>(T shape) where T : unmanaged, IShape
        {
            var s = AllocT(shape);
            shapes.Add((Shape*)s);
        }
    }
}