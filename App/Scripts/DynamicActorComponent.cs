namespace App.Scripts
{
    using System.Numerics;
    using VoxelEngine.Physics;
    using VoxelEngine.Scenes;

    public unsafe class DynamicActorComponent : IPhysicsComponent
    {
        private DynamicActor* actor;

        public GameObject GameObject { get; set; } = null!;

        public bool IsGrounded => actor != null && actor->Grounded;

        public void SetPosition(Vector3 position)
        {
            if (actor == null) return;
            actor->SetPosition(position);
        }

        public void Move(Vector3 position)
        {
            if (actor == null) return;
            GameObject.Scene.Physics.MoveWithCollision(actor, position);
        }

        public void Awake()
        {
            actor = GameObject.Scene.Physics.CreateActor();
            actor->AddShape(new BoxShape(new(0.5f, 2, 0.5f)));
            actor->SetPosition(GameObject.Transform.GlobalPosition);
        }

        public void Destroy()
        {
            if (actor == null) return;
            GameObject.Scene.Physics.DestroyActor(actor);
            actor = null;
        }

        public void PreTick(PhysicsSystem system)
        {
        }

        public void PostTick(PhysicsSystem system)
        {
            if (actor == null) return;
            GameObject.Transform.GlobalPosition = actor->GetPosition();
        }
    }
}