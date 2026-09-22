namespace VoxelEngine.Scripting
{
    using VoxelEngine.Scenes;

    /// <summary>
    /// For scripts that need both frame wise updates and not frame wise updates.
    /// </summary>
    /// <seealso cref="IComponent" />
    public abstract class ScriptComponent : IComponent
    {
        public GameObject GameObject { get; set; }

        public Scene Scene => GameObject.Scene;

        public virtual void Awake()
        {
        }

        public virtual void Destroy()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void FixedUpdate()
        {
        }
    }
}