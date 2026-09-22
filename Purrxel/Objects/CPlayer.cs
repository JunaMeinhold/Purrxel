namespace App.Objects
{
    using System.Numerics;
    using App.Scripts;
    using VoxelEngine.Voxel;

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