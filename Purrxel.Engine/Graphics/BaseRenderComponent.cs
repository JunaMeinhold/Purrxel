namespace VoxelEngine.Graphics
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using VoxelEngine.Scenes;

    public abstract class BaseRenderComponent : IRenderComponent
    {
        private bool initialized;

        public GameObject GameObject { get; set; }

        public abstract int QueueIndex { get; }

        void IComponent.Awake()
        {
            if (initialized) return;
            Awake();
            initialized = true;
        }

        void IComponent.Destroy()
        {
            if (!initialized) return;
            Destroy();
            initialized = false;
        }

        public abstract void Awake();

        public abstract void Destroy();

        public abstract void Draw(GraphicsContext context, PassIdentifer pass, Camera camera, object? parameter);
    }
}