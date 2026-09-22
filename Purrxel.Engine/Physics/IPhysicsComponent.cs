namespace VoxelEngine.Physics
{
    using VoxelEngine.Scenes;

    public interface IPhysicsComponent : IComponent
    {
        public void PreTick(PhysicsSystem system);

        public void PostTick(PhysicsSystem system);
    }
}