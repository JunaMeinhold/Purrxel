namespace VoxelEngine.Graphics
{
    using Hexa.NET.D3D11;
    using HexaEngine.Queries.Generic;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Scenes;

    public class RenderSystem : ISceneSystem
    {
        private readonly ComponentTypeQuery<IRenderComponent> components = new();
        private readonly RenderQueues queues = new();
        private bool initialized;

        public SystemFlags Flags { get; } = SystemFlags.Awake | SystemFlags.Destroy;

        public string Name { get; } = "Render System";

        public void Awake(Scene scene)
        {
            components.OnAdded += OnAdded;
            components.OnRemoved += OnRemoved;
            scene.QueryManager.AddQuery(components);
            foreach (var component in components)
            {
                component.Awake();
            }
            initialized = true;
        }

        private void OnAdded(GameObject gameObject, IRenderComponent component)
        {
            if (initialized)
            {
                component.Awake();
            }
            queues.Add(component);
        }

        private void OnRemoved(GameObject gameObject, IRenderComponent component)
        {
            queues.Remove(component);
            if (initialized)
            {
                component.Destroy();
            }
        }

        public void Destroy()
        {
            components.OnAdded -= OnAdded;
            components.OnRemoved -= OnRemoved;
            components.Dispose();
            initialized = false;
        }

        public void Draw(GraphicsContext context, RenderQueueIndex index, PassIdentifer pass, Camera camera, object? parameter = null)
        {
            foreach (var component in queues[index])
            {
                component.Draw(context, pass, camera, parameter);
            }
        }
    }
}