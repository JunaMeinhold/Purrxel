namespace Purrxel.Engine.Physics
{
    using System.Numerics;

    public struct RaycastHit
    {
        public Vector3 Position;
        public Vector3 Normal;
        public bool Hit;
        public int BlockX, BlockY, BlockZ;
    }
}