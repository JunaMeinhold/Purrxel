namespace VoxelEngine.Scenes
{
    using VoxelEngine.Collections;

    public interface ISceneSystem : IHasFlags<SystemFlags>
    {
        public string Name { get; }

        public void Awake(Scene scene)
        {
        }

        public void Destroy()
        {
        }

        public void Update(float delta)
        {
        }

        public void FixedUpdate()
        {
        }
    }
}