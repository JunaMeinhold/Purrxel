namespace VoxelEngine.Scripting
{
    using HexaEngine.Queries;
    using HexaEngine.Queries.Generic;
    using VoxelEngine.Scenes;

    public class ScriptSystem : ISceneSystem
    {
        private readonly ComponentTypeQuery<ScriptComponent> components = new();

        public SystemFlags Flags { get; } = SystemFlags.Update | SystemFlags.FixedUpdate | SystemFlags.Awake | SystemFlags.Destroy;
        public string Name { get; } = "Script System";

        public void Awake(Scene scene)
        {
            components.OnAdded += OnAdded;
            components.OnRemoved += OnRemoved;
            scene.QueryManager.AddQuery(components);
        }

        private void OnRemoved(GameObject gameObject, ScriptComponent component)
        {
            component.Destroy();
        }

        private void OnAdded(GameObject gameObject, ScriptComponent component)
        {
            component.Awake();
        }

        public void Update(float delta)
        {
            foreach (var component in components)
            {
                component.Update();
            }
        }

        public void FixedUpdate()
        {
            foreach (var component in components)
            {
                component.FixedUpdate();
            }
        }

        public void Destroy()
        {
            foreach (var component in components)
            {
                component.Destroy();
            }
        }
    }
}