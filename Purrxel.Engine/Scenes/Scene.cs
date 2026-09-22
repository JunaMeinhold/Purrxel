namespace VoxelEngine.Scenes
{
    using Hexa.NET.D3D11;
    using HexaEngine.Queries;
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using VoxelEngine.Collections;
    using VoxelEngine.Core;
    using VoxelEngine.Graphics;
    using VoxelEngine.Graphics.D3D11;
    using VoxelEngine.Lights;
    using VoxelEngine.Physics;
    using VoxelEngine.Scripting;
    using VoxelEngine.Voxel;
    using VoxelEngine.Windows;

    public delegate void SceneEventHandler<T>(Scene scene, T args);

    public class SceneRootNode : GameObject
    {
        private Scene parent;

        public SceneRootNode(Scene parent)
        {
            this.parent = parent;
            Name = "Root";
        }

        public override Scene Scene { get => parent; internal set => parent = value; }
    }

    public class Scene : IDisposable
    {
        private bool disposedValue;
        private bool initialized;
        private SceneDispatcher dispatcher = new();
        private readonly SceneProfiler profiler = new();
        private readonly List<GameObject> gameObjects = new();
        private readonly FlaggedList<SystemFlags, ISceneSystem> systems = new();
        private readonly SceneRootNode root;

        private readonly SemaphoreSlim semaphore = new(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="Scene"/> class.
        /// </summary>
        public Scene()
        {
            root = new(this);
            systems.Add(QueryManager = new QuerySystem(this));
            systems.Add(TransformSystem = new TransformSystem());
            systems.Add(RenderSystem = new RenderSystem());
            systems.Add(new ScriptSystem());
            systems.Add(Physics = new PhysicsSystem());
            systems.Add(LightSystem = new LightSystem());
        }

        public IReadOnlyList<GameObject> GameObjects => gameObjects;

        /// <summary>
        /// Gets the dispatcher.
        /// </summary>
        /// <value>
        /// The dispatcher. Used to ensure thread-safety
        /// </value>
        public SceneDispatcher Dispatcher => dispatcher;

        /// <summary>
        /// Gets the window.
        /// </summary>
        /// <value>
        /// The window.
        /// </value>
        public GameWindow Window { get; private set; }

        /// <summary>
        /// Gets or sets the camera.
        /// </summary>
        /// <value>
        /// The camera.
        /// </value>
        public Camera Camera { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is simulating.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is simulating; otherwise, <c>false</c>.
        /// </value>
        public bool IsSimulating { get; set; }

        public event SceneEventHandler<GameObject>? GameObjectAdded;

        public event SceneEventHandler<GameObject>? GameObjectRemoved;

        public SceneProfiler Profiler => profiler;

        public QuerySystem QueryManager { get; }

        public RenderSystem RenderSystem { get; }

        public LightSystem LightSystem { get; }

        public MiniProfiler SceneProfiler { get; } = new();

        public TransformSystem TransformSystem { get; }
        public PhysicsSystem Physics { get; }

        public virtual void Initialize()
        {
            Window = (GameWindow)Application.MainWindow;

            root.Awake();

            foreach (var system in systems[SystemFlags.Awake])
            {
                system.Awake(this);
            }

            Time.FixedUpdate += FixedUpdate;
            initialized = true;
        }

        private void FixedUpdate(object? sender, EventArgs e)
        {
            if (IsSimulating)
            {
                foreach (var system in systems[SystemFlags.PhysicsUpdate])
                {
                    system.FixedUpdate();
                }
            }

            foreach (var system in systems[SystemFlags.FixedUpdate])
            {
                system.FixedUpdate();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick()
        {
            float delta = Time.Delta;
            profiler.Reset();

            foreach (var system in systems[SystemFlags.EarlyUpdate])
            {
                system.Update(delta);
            }

            foreach (var system in systems[SystemFlags.Update])
            {
                system.Update(delta);
            }

            foreach (var system in systems[SystemFlags.LateUpdate])
            {
                system.Update(delta);
            }

            Dispatcher.ExecuteInvokes();
            profiler.ProfileDispatch();
        }

        public T GetSystem<T>() where T : ISceneSystem
        {
            foreach (var system in systems)
            {
                if (system is T t)
                {
                    return t;
                }
            }

            throw new KeyNotFoundException();
        }

        /// <summary>
        /// Adds an scene element.<br/>
        /// Calls <see cref="GameObject.Awake"/> if <see cref="initialized"/> == <see langword="true" />
        /// </summary>
        /// <param name="node">The scene element.</param>
        public void Add(GameObject node)
        {
            if (semaphore.CurrentCount == 0)
            {
                Dispatcher.Invoke(node, root.AddChild);
                return;
            }
            semaphore.Wait();
            root.AddChild(node);
            semaphore.Release();
        }

        public void AddChildUnsafe(GameObject node)
        {
            semaphore.Wait();
            root.AddChild(node);
            semaphore.Release();
        }

        /// <summary>
        /// Removes an scene element.<br/>
        /// Calls <see cref="GameObject.Destroy"/> if <see cref="initialized"/> == <see langword="true" />
        /// </summary>
        /// <param name="node">The scene element.</param>
        public void Remove(GameObject node)
        {
            if (semaphore.CurrentCount == 0)
            {
                Dispatcher.Invoke(node, node => root.RemoveChild(node));
                return;
            }
            semaphore.Wait();
            root.RemoveChild(node);
            semaphore.Release();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Time.FixedUpdate -= FixedUpdate;
                root.Destroy();
                foreach (var system in systems[SystemFlags.Destroy])
                {
                    system.Destroy();
                }

                dispatcher = null;
                Camera = null;

                Window = null;
                initialized = false;
                disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Register(GameObject gameObject)
        {
            gameObjects.Add(gameObject);
            GameObjectAdded?.Invoke(this, gameObject);
        }

        internal void Unregister(GameObject gameObject)
        {
            gameObjects.Remove(gameObject);
            GameObjectRemoved?.Invoke(this, gameObject);
        }

        public T? Find<T>()
        {
            foreach (var gameObject in gameObjects)
            {
                if (gameObject is T t)
                {
                    return t;
                }
            }
            return default;
        }
    }
}