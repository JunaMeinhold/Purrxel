namespace Purrxel.Objects
{
    using Purrxel.Engine.Voxel;
    using Purrxel.Scripts;
    using System.Numerics;

    public class CPlayer : Player
    {
        public CPlayer(Vector3 spawnpoint) : base(spawnpoint)
        {
            Transform.Position = spawnpoint;
            AddComponent(new PlayerController());
            AddComponent(new DynamicActorComponent());
        }
    }
}