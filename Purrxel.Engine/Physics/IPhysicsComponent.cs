namespace Purrxel.Engine.Physics
{
    using Purrxel.Engine.Scenes;

    public interface IPhysicsComponent : IComponent
    {
        public void PreTick(PhysicsSystem system);

        public void PostTick(PhysicsSystem system);
    }
}