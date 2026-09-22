namespace VoxelEngine.Scenes
{
    /// <summary>
    /// Interface for SceneElement Components
    /// </summary>
    public interface IComponent
    {
        GameObject GameObject { get; set; }

        /// <summary>
        /// Initializes the component.<br/>
        /// Called by <see cref="GameObject.Awake"/> ... <see cref="Scene.Initialize"/> ... <see cref="SceneManager.Load(Scene)"/><br/>
        /// Called by <see cref="GameObject.Awake"/> ... <see cref="Scene.Add(GameObject)"/> <see cref="Scene.initialized"/> == <see langword="true" /><br/>
        /// </summary>
        /// <param name="element">The element.</param>
        void Awake();

        /// <summary>
        /// Uninitializes the component.<br/>
        /// Called by <see cref="GameObject.Destroy"/> ... <see cref="Scene.Dispose"/> ... <see cref="SceneManager.Unload"/><br/>
        /// </summary>
        void Destroy();
    }
}